using System.Windows;
using LANShare.CSharp.ViewModels;

namespace LANShare.CSharp.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel viewModel)
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
