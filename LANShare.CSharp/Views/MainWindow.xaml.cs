using System.Windows;
using LANShare.CSharp.Models;
using LANShare.CSharp.ViewModels;

namespace LANShare.CSharp.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.RequestDeviceSelection += OnRequestDeviceSelection;
            _viewModel.RequestSettingsView += OnRequestSettingsView;
            _viewModel.RequestAboutView += OnRequestAboutView;

            Unloaded += (s, e) => _viewModel.Dispose();
        }

        private void OnRequestAboutView()
        {
            var vm = new AboutViewModel();
            var dialog = new AboutWindow(vm)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private Device? OnRequestDeviceSelection(System.Collections.Generic.IEnumerable<Device> devices)
        {
            var vm = new DeviceSelectorViewModel(devices);
            var dialog = new DeviceSelectorWindow(vm)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                return vm.SelectedDevice;
            }

            return null;
        }

        private bool? OnRequestSettingsView(AppSettings settings)
        {
            var vm = new SettingsViewModel(settings);
            var dialog = new SettingsWindow(vm)
            {
                Owner = this
            };

            return dialog.ShowDialog();
        }

    }
}
