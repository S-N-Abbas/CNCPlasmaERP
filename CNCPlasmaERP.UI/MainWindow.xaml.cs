using System.Windows;
using CNCPlasmaERP.UI.ViewModels;

namespace CNCPlasmaERP.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
