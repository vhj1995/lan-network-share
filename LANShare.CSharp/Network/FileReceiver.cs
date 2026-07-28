using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.Network
{
    public class FileReceiver
    {
        private readonly TcpClient _client;
        private readonly AppSettings _settings;

        public event Action<TransferInfo>? HeaderReceived;
        public event Action<long, long, double>? ProgressChanged; // bytesReceived, totalBytes, speedBytesPerSec
        public event Action<string>? StatusChanged;
        public event Action<TransferStatus>? StateChanged;

        public FileReceiver(TcpClient client, AppSettings settings)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task ReceiveAsync(CancellationToken cancellationToken = default)
        {
            TransferInfo? transfer = null;

            try
            {
                using NetworkStream stream = _client.GetStream();

                // Read 4-Byte Header Length Prefix
                byte[] lengthBuffer = new byte[4];
                int readLen = 0;
                while (readLen < 4)
                {
                    int n = await stream.ReadAsync(lengthBuffer, readLen, 4 - readLen, cancellationToken);
                    if (n == 0) throw new IOException("Connection closed while reading header length.");
                    readLen += n;
                }
                int headerLength = BitConverter.ToInt32(lengthBuffer, 0);

                // Read Header JSON Payload
                byte[] headerBuffer = new byte[headerLength];
                int readHeader = 0;
                while (readHeader < headerLength)
                {
                    int n = await stream.ReadAsync(headerBuffer, readHeader, headerLength - readHeader, cancellationToken);
                    if (n == 0) throw new IOException("Connection closed while reading header payload.");
                    readHeader += n;
                }

                string headerJson = Encoding.UTF8.GetString(headerBuffer);
                var header = JsonSerializer.Deserialize<TransferHeader>(headerJson);
                if (header == null) throw new InvalidDataException("Received invalid transfer header JSON.");

                // Populate TransferInfo Model
                string remoteIp = ((System.Net.IPEndPoint)_client.Client.RemoteEndPoint!).Address.ToString();
                transfer = new TransferInfo
                {
                    Id = string.IsNullOrEmpty(header.TransferId) ? Guid.NewGuid().ToString() : header.TransferId,
                    Direction = TransferDirection.Download,
                    Peer = new Device
                    {
                        Name = header.SenderName,
                        OperatingSystem = header.SenderOs,
                        IpAddress = remoteIp
                    },
                    FolderName = header.FolderName,
                    FileName = string.IsNullOrEmpty(header.FolderName) && header.Files.Count > 0 ? header.Files[0].RelativePath : header.FolderName,
                    TotalSize = header.TotalSize,
                    Status = TransferStatus.Transferring,
                    StatusMessage = "Receiving file stream..."
                };

                HeaderReceived?.Invoke(transfer);
                UpdateState(transfer, TransferStatus.Transferring, "Receiving file stream...");

                // Setup Output Download Target Directory
                string targetDir = _settings.DownloadDirectory;
                if (!string.IsNullOrEmpty(header.FolderName))
                {
                    targetDir = Path.Combine(targetDir, header.FolderName);
                }
                Directory.CreateDirectory(targetDir);

                int bufferSize = Math.Max(4096, _settings.FileBufferSizeKb * 1024);
                byte[] buffer = new byte[bufferSize];

                long totalBytesReceived = 0;
                var stopwatch = Stopwatch.StartNew();
                long lastBytesCount = 0;
                double currentSpeed = 0;

                foreach (var fileHeader in header.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string targetFilePath = Path.Combine(targetDir, fileHeader.RelativePath);
                    string? subDir = Path.GetDirectoryName(targetFilePath);
                    if (!string.IsNullOrEmpty(subDir))
                    {
                        Directory.CreateDirectory(subDir);
                    }

                    UpdateStatus(transfer, $"Receiving {fileHeader.RelativePath}...");

                    long bytesNeededForFile = fileHeader.Size;
                    using var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

                    while (bytesNeededForFile > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int readChunkSize = (int)Math.Min(buffer.Length, bytesNeededForFile);
                        int bytesRead = await stream.ReadAsync(buffer, 0, readChunkSize, cancellationToken);
                        if (bytesRead == 0)
                        {
                            throw new IOException($"Unexpected end of stream while receiving {fileHeader.RelativePath}");
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        bytesNeededForFile -= bytesRead;
                        totalBytesReceived += bytesRead;
                        transfer.BytesTransferred = totalBytesReceived;

                        if (stopwatch.ElapsedMilliseconds >= 300)
                        {
                            double elapsedSec = stopwatch.Elapsed.TotalSeconds;
                            currentSpeed = (totalBytesReceived - lastBytesCount) / elapsedSec;
                            transfer.SpeedBytesPerSec = currentSpeed;
                            lastBytesCount = totalBytesReceived;
                            stopwatch.Restart();

                            ProgressChanged?.Invoke(totalBytesReceived, transfer.TotalSize, currentSpeed);
                        }
                    }
                }

                ProgressChanged?.Invoke(totalBytesReceived, transfer.TotalSize, currentSpeed);
                UpdateState(transfer, TransferStatus.Completed, "Download completed successfully");
            }
            catch (OperationCanceledException)
            {
                if (transfer != null)
                {
                    UpdateState(transfer, TransferStatus.Canceled, "Download canceled by user");
                }
            }
            catch (Exception ex)
            {
                if (transfer != null)
                {
                    UpdateState(transfer, TransferStatus.Failed, $"Download failed: {ex.Message}");
                }
            }
            finally
            {
                _client.Close();
            }
        }

        private void UpdateStatus(TransferInfo transfer, string message)
        {
            transfer.StatusMessage = message;
            StatusChanged?.Invoke(message);
        }

        private void UpdateState(TransferInfo transfer, TransferStatus status, string message)
        {
            transfer.Status = status;
            transfer.StatusMessage = message;
            StateChanged?.Invoke(status);
            StatusChanged?.Invoke(message);
        }
    }
}
