using CNCPlasmaERP.UI.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace CNCPlasmaERP.UI
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
