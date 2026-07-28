using System.Windows.Input;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;

        private string _deviceName;
        private string _downloadDirectory;
        private int _broadcastPort;
        private int _transferPort;

        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        public string DownloadDirectory
        {
            get => _downloadDirectory;
            set => SetProperty(ref _downloadDirectory, value);
        }

        public int BroadcastPort
        {
            get => _broadcastPort;
            set => SetProperty(ref _broadcastPort, value);
        }

        public int TransferPort
        {
            get => _transferPort;
            set => SetProperty(ref _transferPort, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseDownloadDirCommand { get; }

        public event System.Action<bool>? CloseRequested;

        public SettingsViewModel(AppSettings settings)
        {
            _settings = settings;
            _deviceName = settings.DeviceName;
            _downloadDirectory = settings.DownloadDirectory;
            _broadcastPort = settings.BroadcastPort;
            _transferPort = settings.TransferPort;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(false));
            BrowseDownloadDirCommand = new RelayCommand(BrowseDownloadDir);
        }

        private void Save()
        {
            _settings.DeviceName = DeviceName;
            _settings.DownloadDirectory = DownloadDirectory;
            _settings.BroadcastPort = BroadcastPort;
            _settings.TransferPort = TransferPort;
            _settings.Save();

            CloseRequested?.Invoke(true);
        }

        private void BrowseDownloadDir()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Download Directory",
                SelectedPath = DownloadDirectory,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DownloadDirectory = dialog.SelectedPath;
            }
        }
    }
}
