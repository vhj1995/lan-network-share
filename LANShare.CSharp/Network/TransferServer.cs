using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.Network
{
    public class TransferServer : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly Action<TransferInfo> _onTransferCreated;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public event Action<FileReceiver>? ReceiverConnected;

        public TransferServer(AppSettings settings, Action<TransferInfo> onTransferCreated)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _onTransferCreated = onTransferCreated ?? throw new ArgumentNullException(nameof(onTransferCreated));
        }

        public void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _settings.TransferPort);
            _listener.Start();

            Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(ct);
                    var receiver = new FileReceiver(client, _settings);

                    receiver.HeaderReceived += info =>
                    {
                        _onTransferCreated(info);
                    };

                    ReceiverConnected?.Invoke(receiver);

                    _ = Task.Run(() => receiver.ReceiveAsync(ct), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TransferServer] Listener accept loop error: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
