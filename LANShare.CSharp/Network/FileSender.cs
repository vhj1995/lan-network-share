using System;
using System.Collections.Generic;
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
    public class FileSender
    {
        private readonly AppSettings _settings;

        public event Action<long, long, double>? ProgressChanged; // bytesSent, totalBytes, speedBytesPerSec
        public event Action<string>? StatusChanged;
        public event Action<TransferStatus>? StateChanged;

        public FileSender(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task SendAsync(TransferInfo transfer, CancellationToken cancellationToken = default)
        {
            if (transfer == null) throw new ArgumentNullException(nameof(transfer));

            UpdateState(transfer, TransferStatus.Transferring, "Connecting to recipient...");

            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(transfer.Peer.IpAddress, transfer.Peer.Port, cancellationToken);
                using NetworkStream stream = client.GetStream();

                List<FileInfoHeader> fileHeaders = new List<FileInfoHeader>();
                List<string> fullFilePaths = new List<string>();
                long calculatedTotalSize = 0;

                if (Directory.Exists(transfer.FilePath))
                {
                    // Recursive Folder Transfer
                    string baseDirPath = transfer.FilePath;
                    transfer.FolderName = Path.GetFileName(baseDirPath);

                    var allFiles = Directory.GetFiles(baseDirPath, "*", SearchOption.AllDirectories);
                    foreach (var filePath in allFiles)
                    {
                        var fi = new FileInfo(filePath);
                        string relPath = Path.GetRelativePath(baseDirPath, filePath);

                        fileHeaders.Add(new FileInfoHeader
                        {
                            RelativePath = relPath,
                            Size = fi.Length
                        });
                        fullFilePaths.Add(filePath);
                        calculatedTotalSize += fi.Length;
                    }

                    transfer.TotalSize = calculatedTotalSize;
                }
                else if (File.Exists(transfer.FilePath))
                {
                    // Single File Transfer
                    var fi = new FileInfo(transfer.FilePath);
                    transfer.FileName = fi.Name;
                    transfer.TotalSize = fi.Length;

                    fileHeaders.Add(new FileInfoHeader
                    {
                        RelativePath = fi.Name,
                        Size = fi.Length
                    });
                    fullFilePaths.Add(transfer.FilePath);
                    calculatedTotalSize = fi.Length;
                }
                else
                {
                    throw new FileNotFoundException($"Source file or folder path not found: {transfer.FilePath}");
                }

                // Construct Protocol Header
                var header = new TransferHeader
                {
                    TransferId = transfer.Id,
                    SenderName = _settings.DeviceName,
                    SenderOs = OperatingSystem.IsWindows() ? "Windows" : (OperatingSystem.IsLinux() ? "Linux" : "macOS"),
                    SenderIp = DeviceBroadcaster.GetLocalIPAddress(),
                    FolderName = transfer.FolderName,
                    TotalSize = transfer.TotalSize,
                    Files = fileHeaders
                };

                // Send Header Payload (4-byte length prefix + UTF-8 JSON)
                string headerJson = JsonSerializer.Serialize(header);
                byte[] headerBytes = Encoding.UTF8.GetBytes(headerJson);
                byte[] headerLengthBytes = BitConverter.GetBytes(headerBytes.Length);

                await stream.WriteAsync(headerLengthBytes, 0, headerLengthBytes.Length, cancellationToken);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                UpdateState(transfer, TransferStatus.Transferring, "Sending data stream...");

                int bufferSize = Math.Max(4096, _settings.FileBufferSizeKb * 1024);
                byte[] buffer = new byte[bufferSize];

                long totalBytesSent = 0;
                var stopwatch = Stopwatch.StartNew();
                long lastBytesCount = 0;
                double currentSpeed = 0;

                for (int i = 0; i < fullFilePaths.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string sourcePath = fullFilePaths[i];
                    string relativeName = fileHeaders[i].RelativePath;
                    UpdateStatus(transfer, $"Sending {relativeName}...");

                    using var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);

                    int bytesRead;
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalBytesSent += bytesRead;
                        transfer.BytesTransferred = totalBytesSent;

                        if (stopwatch.ElapsedMilliseconds >= 300)
                        {
                            double elapsedSec = stopwatch.Elapsed.TotalSeconds;
                            currentSpeed = (totalBytesSent - lastBytesCount) / elapsedSec;
                            transfer.SpeedBytesPerSec = currentSpeed;
                            lastBytesCount = totalBytesSent;
                            stopwatch.Restart();

                            ProgressChanged?.Invoke(totalBytesSent, transfer.TotalSize, currentSpeed);
                        }
                    }
                }

                await stream.FlushAsync(cancellationToken);
                ProgressChanged?.Invoke(totalBytesSent, transfer.TotalSize, currentSpeed);
                UpdateState(transfer, TransferStatus.Completed, "Transfer completed successfully");
            }
            catch (OperationCanceledException)
            {
                UpdateState(transfer, TransferStatus.Canceled, "Transfer canceled by user");
            }
            catch (Exception ex)
            {
                UpdateState(transfer, TransferStatus.Failed, $"Transfer failed: {ex.Message}");
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
