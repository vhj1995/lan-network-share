using System.Windows;
using LANShare.CSharp.ViewModels;

namespace LANShare.CSharp.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow(AboutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.CloseRequested += () => Close();
        }
    }
}
