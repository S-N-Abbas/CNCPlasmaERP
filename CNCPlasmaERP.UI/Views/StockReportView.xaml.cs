using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CNCPlasmaERP.UI.Models;
using CNCPlasmaERP.UI.ViewModels;

namespace CNCPlasmaERP.UI.Views
{
    public partial class StockReportView : UserControl
    {
        public StockReportView()
        {
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not StockReportViewModel vm || vm.CompanySettings is null)
            {
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var printData = new StockPrintData
            {
                Settings = vm.CompanySettings,
                Materials = vm.Materials.ToList()
            };

            var printView = new StockReportPrintView
            {
                DataContext = printData
            };

            printView.Measure(new Size(printView.Width, printView.Height));
            printView.Arrange(new Rect(0, 0, printView.Width, printView.Height));
            printView.UpdateLayout();

            printDialog.PrintVisual(printView, "Stock Report");
        }
    }
}
