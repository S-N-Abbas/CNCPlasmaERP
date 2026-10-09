using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CNCPlasmaERP.UI.Models;
using CNCPlasmaERP.UI.ViewModels;

namespace CNCPlasmaERP.UI.Views
{
    public partial class ProfitLossReportView : UserControl
    {
        public ProfitLossReportView()
        {
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProfitLossReportViewModel vm || vm.Summary is null || vm.CompanySettings is null)
            {
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var printData = new ProfitLossPrintData
            {
                Settings = vm.CompanySettings,
                Summary = vm.Summary,
                MaterialLines = vm.MaterialLines.ToList()
            };

            var printView = new ProfitLossPrintView
            {
                DataContext = printData
            };

            printView.Measure(new Size(printView.Width, printView.Height));
            printView.Arrange(new Rect(0, 0, printView.Width, printView.Height));
            printView.UpdateLayout();

            printDialog.PrintVisual(printView, "Profit and Loss Report");
        }
    }
}
