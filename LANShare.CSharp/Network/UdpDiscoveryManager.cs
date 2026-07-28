using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.Network
{
    public class UdpDiscoveryManager : IDisposable
    {
        private readonly AppSettings _settings;
        private UdpClient? _udpListener;
        private UdpClient? _udpSender;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<string, Device> _activePeers = new();
        private PeriodicTimer? _broadcastTimer;
        private PeriodicTimer? _cleanupTimer;
        private Task? _listenTask;
        private Task? _broadcastTask;
        private Task? _cleanupTask;

        public event Action<Device>? PeerDiscovered;
        public event Action<Device>? PeerUpdated;
        public event Action<Device>? PeerLost;

        public IReadOnlyCollection<Device> ActivePeers => _activePeers.Values.ToList().AsReadOnly();

        public UdpDiscoveryManager(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();

            try
            {
                _udpSender = new UdpClient();
                _udpSender.EnableBroadcast = true;

                _udpListener = new UdpClient();
                _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, _settings.BroadcastPort));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UdpDiscoveryManager] Error initializing UDP sockets: {ex.Message}");
            }

            _listenTask = Task.Run(() => ListenAsync(_cts.Token));
            _broadcastTask = Task.Run(() => BroadcastLoopAsync(_cts.Token));
            _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
        }

        public async Task BroadcastPresenceAsync()
        {
            if (_udpSender == null) return;

            try
            {
                string localIp = GetLocalIPAddress();
                var payload = new BroadcastPayload
                {
                    Name = _settings.DeviceName,
                    OS = GetCurrentOSName(),
                    Ip = localIp,
                    Port = _settings.TransferPort
                };

                string json = JsonSerializer.Serialize(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                var target = new IPEndPoint(IPAddress.Broadcast, _settings.BroadcastPort);

                await _udpSender.SendAsync(bytes, bytes.Length, target);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UdpDiscoveryManager] Failed to send broadcast ping: {ex.Message}");
            }
        }

        private async Task BroadcastLoopAsync(CancellationToken ct)
        {
            _broadcastTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_settings.BroadcastIntervalMs));
            
            // Immediate initial ping
            await BroadcastPresenceAsync();

            try
            {
                while (await _broadcastTimer.WaitForNextTickAsync(ct))
                {
                    await BroadcastPresenceAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _udpListener != null)
            {
                try
                {
                    UdpReceiveResult result = await _udpListener.ReceiveAsync(ct);
                    string json = Encoding.UTF8.GetString(result.Buffer);

                    var payload = JsonSerializer.Deserialize<BroadcastPayload>(json);
                    if (payload != null && !string.IsNullOrWhiteSpace(payload.Ip))
                    {
                        string localIp = GetLocalIPAddress();
                        string senderIp = result.RemoteEndPoint.Address.ToString();

                        // Exclude pings originating from self
                        if (payload.Ip != localIp && senderIp != localIp)
                        {
                            ProcessPeerPayload(payload, senderIp);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UdpDiscoveryManager] Listening error: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }

        private void ProcessPeerPayload(BroadcastPayload payload, string remoteAddress)
        {
            string key = string.IsNullOrWhiteSpace(payload.Ip) ? remoteAddress : payload.Ip;

            var peer = new Device
            {
                Id = key,
                Name = payload.Name,
                OperatingSystem = payload.OS,
                IpAddress = key,
                Port = payload.Port > 0 ? payload.Port : 45455,
                LastSeen = DateTime.Now,
                IsOnline = true
            };

            bool isNew = !_activePeers.ContainsKey(key);
            _activePeers[key] = peer;

            if (isNew)
            {
                PeerDiscovered?.Invoke(peer);
            }
            else
            {
                PeerUpdated?.Invoke(peer);
            }
        }

        private async Task CleanupLoopAsync(CancellationToken ct)
        {
            _cleanupTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            try
            {
                while (await _cleanupTimer.WaitForNextTickAsync(ct))
                {
                    var now = DateTime.Now;
                    var staleKeys = _activePeers
                        .Where(kvp => (now - kvp.Value.LastSeen).TotalSeconds > 10)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in staleKeys)
                    {
                        if (_activePeers.TryRemove(key, out var removedPeer))
                        {
                            removedPeer.IsOnline = false;
                            PeerLost?.Invoke(removedPeer);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // Fallback method
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private static string GetCurrentOSName()
        {
            if (OperatingSystem.IsWindows()) return "Windows";
            if (OperatingSystem.IsLinux()) return "Linux";
            if (OperatingSystem.IsMacOS()) return "macOS";
            return "Unknown";
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpListener?.Close();
            _udpSender?.Close();
            _udpListener = null;
            _udpSender = null;
            _broadcastTimer?.Dispose();
            _cleanupTimer?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
