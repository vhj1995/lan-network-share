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
    public class DeviceBroadcaster : IDisposable
    {
        private readonly AppSettings _settings;
        private UdpClient? _udpListener;
        private UdpClient? _udpSender;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<string, Device> _activeDevices = new();
        private readonly System.Threading.Timer _broadcastTimer;
        private readonly System.Threading.Timer _cleanupTimer;

        public event Action<Device>? DeviceDiscovered;
        public event Action<Device>? DeviceUpdated;
        public event Action<Device>? DeviceLost;

        public IReadOnlyCollection<Device> ActiveDevices => _activeDevices.Values.ToList().AsReadOnly();

        public DeviceBroadcaster(AppSettings settings)
        {
            _settings = settings;
            _broadcastTimer = new System.Threading.Timer(OnBroadcastTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer = new System.Threading.Timer(OnCleanupTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
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
                System.Diagnostics.Debug.WriteLine($"Failed to bind UDP socket: {ex.Message}");
            }

            Task.Run(() => ListenLoopAsync(_cts.Token));

            _broadcastTimer.Change(0, _settings.BroadcastIntervalMs);
            _cleanupTimer.Change(5000, 5000);
        }

        public void Stop()
        {
            _broadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _cts?.Cancel();
            _udpListener?.Close();
            _udpSender?.Close();
            _udpListener = null;
            _udpSender = null;
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _udpListener != null)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(ct);
                    string json = Encoding.UTF8.GetString(result.Buffer);

                    var payload = JsonSerializer.Deserialize<BroadcastPayload>(json);
                    if (payload != null && !string.IsNullOrWhiteSpace(payload.Ip))
                    {
                        string localIp = GetLocalIPAddress();
                        if (payload.Ip != localIp && result.RemoteEndPoint.Address.ToString() != localIp)
                        {
                            ProcessReceivedDevice(payload, result.RemoteEndPoint.Address.ToString());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in broadcast listen loop: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }

        private void ProcessReceivedDevice(BroadcastPayload payload, string remoteAddress)
        {
            string key = string.IsNullOrWhiteSpace(payload.Ip) ? remoteAddress : payload.Ip;

            var device = new Device
            {
                Id = key,
                Name = payload.Name,
                OperatingSystem = payload.OS,
                IpAddress = key,
                Port = payload.Port > 0 ? payload.Port : 45455,
                LastSeen = DateTime.Now,
                IsOnline = true
            };

            bool isNew = !_activeDevices.ContainsKey(key);
            _activeDevices[key] = device;

            if (isNew)
            {
                DeviceDiscovered?.Invoke(device);
            }
            else
            {
                DeviceUpdated?.Invoke(device);
            }
        }

        private void OnBroadcastTimerCallback(object? state)
        {
            SendBroadcast();
        }

        public void SendBroadcast()
        {
            if (_udpSender == null) return;

            try
            {
                string localIp = GetLocalIPAddress();
                var payload = new BroadcastPayload
                {
                    Name = _settings.DeviceName,
                    OS = "Windows",
                    Ip = localIp,
                    Port = _settings.TransferPort
                };

                string json = JsonSerializer.Serialize(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                IPEndPoint target = new IPEndPoint(IPAddress.Broadcast, _settings.BroadcastPort);

                _udpSender.Send(bytes, bytes.Length, target);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send UDP broadcast: {ex.Message}");
            }
        }

        private void OnCleanupTimerCallback(object? state)
        {
            var now = DateTime.Now;
            var expiredKeys = _activeDevices
                .Where(kvp => (now - kvp.Value.LastSeen).TotalSeconds > 10)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_activeDevices.TryRemove(key, out var removedDevice))
                {
                    removedDevice.IsOnline = false;
                    DeviceLost?.Invoke(removedDevice);
                }
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
                // Fallback to network interfaces
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

        public void Dispose()
        {
            Stop();
            _broadcastTimer.Dispose();
            _cleanupTimer.Dispose();
        }
    }
}
