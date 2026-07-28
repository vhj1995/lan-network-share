using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LANShare.CSharp.Models;
using LANShare.CSharp.Network;

namespace LANShare.CSharp.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly AppSettings _settings;
        private readonly UdpDiscoveryManager _udpDiscovery;
        private readonly TransferServer _transferServer;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

        [ObservableProperty]
        private Device? _selectedPeer;

        [ObservableProperty]
        private TransferInfo? _selectedTransfer;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private string _localIpAddress = "127.0.0.1";

        public AppSettings Settings => _settings;

        public ObservableCollection<Device> DiscoveredPeers { get; } = new();
        public ObservableCollection<TransferInfo> ActiveTransfers { get; } = new();

        public event Func<IEnumerable<Device>, Device?>? RequestDeviceSelection;
        public event Func<AppSettings, bool?>? RequestSettingsView;
        public event Action? RequestAboutView;

        public MainViewModel()
        {
            _settings = AppSettings.Load();

            // Wire up UdpDiscoveryManager events
            _udpDiscovery = new UdpDiscoveryManager(_settings);
            _udpDiscovery.PeerDiscovered += OnPeerDiscovered;
            _udpDiscovery.PeerUpdated += OnPeerUpdated;
            _udpDiscovery.PeerLost += OnPeerLost;

            // Wire up TransferServer (TcpListener) events
            _transferServer = new TransferServer(_settings, OnIncomingTransferCreated);

            LocalIpAddress = UdpDiscoveryManager.GetLocalIPAddress();

            // Start background network engines
            _udpDiscovery.Start();
            _transferServer.Start();

            StatusMessage = $"Broadcasting on LAN as '{_settings.DeviceName}' ({LocalIpAddress})";
        }

        #region Peer Discovery Handlers

        private void OnPeerDiscovered(Device peer)
        {
            RunOnUI(() =>
            {
                if (!DiscoveredPeers.Any(p => p.IpAddress == peer.IpAddress))
                {
                    DiscoveredPeers.Add(peer);
                }
            });
        }

        private void OnPeerUpdated(Device peer)
        {
            RunOnUI(() =>
            {
                var existing = DiscoveredPeers.FirstOrDefault(p => p.IpAddress == peer.IpAddress);
                if (existing != null)
                {
                    existing.Name = peer.Name;
                    existing.OperatingSystem = peer.OperatingSystem;
                    existing.Port = peer.Port;
                    existing.LastSeen = peer.LastSeen;
                    existing.IsOnline = true;
                }
            });
        }

        private void OnPeerLost(Device peer)
        {
            RunOnUI(() =>
            {
                var existing = DiscoveredPeers.FirstOrDefault(p => p.IpAddress == peer.IpAddress);
                if (existing != null)
                {
                    DiscoveredPeers.Remove(existing);
                }
            });
        }

        #endregion

        #region Transfer Handlers

        private void OnIncomingTransferCreated(TransferInfo transfer)
        {
            var cts = new CancellationTokenSource();
            _cancellationTokens[transfer.Id] = cts;

            RunOnUI(() =>
            {
                ActiveTransfers.Insert(0, transfer);
            });
        }

        #endregion

        #region Relay Commands

        [RelayCommand]
        private void SendFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File to Send",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Device? targetPeer = PromptSelectPeer();
                if (targetPeer != null)
                {
                    StartSendTask(filePath, targetPeer);
                }
            }
        }

        [RelayCommand]
        private void SendFolder()
        {
            using var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Folder to Send",
                UseDescriptionForTitle = true
            };

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = openFolderDialog.SelectedPath;
                Device? targetPeer = PromptSelectPeer();
                if (targetPeer != null)
                {
                    StartSendTask(folderPath, targetPeer);
                }
            }
        }

        [RelayCommand]
        private void SendToPeer(Device? peer)
        {
            if (peer == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Select File to Send to {peer.DisplayName}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                StartSendTask(openFileDialog.FileName, peer);
            }
        }

        [RelayCommand]
        private void CancelTransfer(TransferInfo? transfer)
        {
            transfer ??= SelectedTransfer;
            if (transfer == null) return;

            if (_cancellationTokens.TryGetValue(transfer.Id, out var cts))
            {
                cts.Cancel();
            }

            if (transfer.Status == TransferStatus.Pending || transfer.Status == TransferStatus.Transferring)
            {
                transfer.Status = TransferStatus.Canceled;
                transfer.StatusMessage = "Canceled by user";
            }
        }

        [RelayCommand]
        private void ClearCompleted()
        {
            var toRemove = ActiveTransfers
                .Where(t => t.Status == TransferStatus.Completed || t.Status == TransferStatus.Canceled || t.Status == TransferStatus.Failed)
                .ToList();

            foreach (var t in toRemove)
            {
                ActiveTransfers.Remove(t);
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            if (RequestSettingsView != null)
            {
                bool? result = RequestSettingsView(_settings);
                if (result == true)
                {
                    StatusMessage = $"Settings updated. Broadcasting as '{_settings.DeviceName}'";
                    _ = _udpDiscovery.BroadcastPresenceAsync();
                }
            }
        }

        [RelayCommand]
        private void OpenAbout()
        {
            RequestAboutView?.Invoke();
        }

        [RelayCommand]
        private void OpenDownloadFolder()
        {
            try
            {
                if (!Directory.Exists(_settings.DownloadDirectory))
                {
                    Directory.CreateDirectory(_settings.DownloadDirectory);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = _settings.DownloadDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open download folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshPeersAsync()
        {
            StatusMessage = "Sending UDP discovery broadcast...";
            await _udpDiscovery.BroadcastPresenceAsync();
        }

        #endregion

        #region Helper Methods

        private Device? PromptSelectPeer()
        {
            if (DiscoveredPeers.Count == 0)
            {
                MessageBox.Show("No active LAN Share peers discovered on your network.", "No Peers Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            if (SelectedPeer != null)
            {
                return SelectedPeer;
            }

            if (RequestDeviceSelection != null)
            {
                return RequestDeviceSelection(DiscoveredPeers);
            }

            return DiscoveredPeers.FirstOrDefault();
        }

        private void StartSendTask(string path, Device targetPeer)
        {
            var transfer = new TransferInfo
            {
                Direction = TransferDirection.Upload,
                FilePath = path,
                FileName = Path.GetFileName(path),
                Peer = targetPeer,
                Status = TransferStatus.Pending,
                StatusMessage = "Pending..."
            };

            if (Directory.Exists(path))
            {
                transfer.FolderName = Path.GetFileName(path);
            }

            ActiveTransfers.Insert(0, transfer);

            var cts = new CancellationTokenSource();
            _cancellationTokens[transfer.Id] = cts;

            Task.Run(async () =>
            {
                var sender = new FileSender(_settings);

                sender.ProgressChanged += (sent, total, speed) =>
                {
                    RunOnUI(() =>
                    {
                        transfer.BytesTransferred = sent;
                        transfer.SpeedBytesPerSec = speed;
                    });
                };

                sender.StatusChanged += msg =>
                {
                    RunOnUI(() => transfer.StatusMessage = msg);
                };

                sender.StateChanged += state =>
                {
                    RunOnUI(() => transfer.Status = state);
                };

                await sender.SendAsync(transfer, cts.Token);
                _cancellationTokens.TryRemove(transfer.Id, out _);
            });
        }

        private static void RunOnUI(Action action)
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        #endregion

        public void Dispose()
        {
            foreach (var cts in _cancellationTokens.Values)
            {
                cts.Cancel();
            }
            _udpDiscovery.Dispose();
            _transferServer.Dispose();
        }
    }
}
