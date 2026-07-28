using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LANShare.CSharp.Models
{
    public enum TransferStatus
    {
        Pending,
        Transferring,
        Completed,
        Failed,
        Canceled,
        Paused
    }

    public enum TransferDirection
    {
        Upload,
        Download
    }

    public class TransferInfo : INotifyPropertyChanged
    {
        private long _bytesTransferred;
        private double _progress;
        private double _speedBytesPerSec;
        private TransferStatus _status = TransferStatus.Pending;
        private string _statusMessage = "Pending";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public bool IsFolder => !string.IsNullOrEmpty(FolderName);
        public long TotalSize { get; set; }

        public long BytesTransferred
        {
            get => _bytesTransferred;
            set
            {
                if (SetProperty(ref _bytesTransferred, value))
                {
                    UpdateProgress();
                }
            }
        }

        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        public double SpeedBytesPerSec
        {
            get => _speedBytesPerSec;
            set
            {
                if (SetProperty(ref _speedBytesPerSec, value))
                {
                    OnPropertyChanged(nameof(SpeedText));
                }
            }
        }

        public string SpeedText => Status == TransferStatus.Transferring ? FormatBytesPerSec(SpeedBytesPerSec) : Status.ToString();

        public TransferStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(SpeedText));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public TransferDirection Direction { get; set; } = TransferDirection.Upload;
        public Device Peer { get; set; } = new Device();
        public DateTime StartTime { get; set; } = DateTime.Now;

        private void UpdateProgress()
        {
            if (TotalSize > 0)
            {
                Progress = Math.Min(100.0, ((double)BytesTransferred / TotalSize) * 100.0);
            }
            else
            {
                Progress = 0;
            }
        }

        public static string FormatBytesPerSec(double bytesPerSec)
        {
            if (bytesPerSec < 1024)
                return $"{bytesPerSec:0} B/s";
            if (bytesPerSec < 1024 * 1024)
                return $"{bytesPerSec / 1024:0.0} KB/s";
            if (bytesPerSec < 1024 * 1024 * 1024)
                return $"{bytesPerSec / (1024 * 1024):0.0} MB/s";
            return $"{bytesPerSec / (1024 * 1024 * 1024):0.0} GB/s";
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:0.0} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):0.0} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.0} GB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
