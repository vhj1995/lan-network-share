using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.ViewModels
{
    public class DeviceSelectorViewModel : ViewModelBase
    {
        private Device? _selectedDevice;

        public ObservableCollection<Device> Devices { get; }
        public Device? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public bool? DialogResult { get; private set; }

        public DeviceSelectorViewModel(System.Collections.Generic.IEnumerable<Device> devices)
        {
            Devices = new ObservableCollection<Device>(devices);
            SelectedDevice = Devices.FirstOrDefault();

            ConfirmCommand = new RelayCommand(
                param => {
                    DialogResult = true;
                    CloseRequested?.Invoke(true);
                },
                param => SelectedDevice != null
            );

            CancelCommand = new RelayCommand(
                param => {
                    DialogResult = false;
                    CloseRequested?.Invoke(false);
                }
            );
        }

        public event System.Action<bool>? CloseRequested;
    }
}
