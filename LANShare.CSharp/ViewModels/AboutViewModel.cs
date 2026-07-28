using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LANShare.CSharp.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string AppName => "LAN Network Share";
        public string Version => "v1.0.0 (.NET 8 WPF)";
        public string DeveloperCredits => "Developed with Love by Vaibhav Joshi";

        public string Description =>
            "LAN Network Share is a modern, Windows desktop application designed for high-speed file and folder transfers across local area networks. " +
            "It features automated UDP peer discovery, direct TCP socket streaming with real-time speed tracking, and full folder hierarchy preservation without relying on cloud services.";

        public event System.Action? CloseRequested;

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke();
        }
    }
}
