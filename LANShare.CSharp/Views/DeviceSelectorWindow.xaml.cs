using System.Windows;
using LANShare.CSharp.ViewModels;

namespace LANShare.CSharp.Views
{
    public partial class DeviceSelectorWindow : Window
    {
        public DeviceSelectorWindow(DeviceSelectorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseRequested += result =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
