using System.Windows;
using System.Windows.Controls;
using CNCPlasmaERP.UI.ViewModels;

namespace CNCPlasmaERP.UI.Views
{
    public partial class InvoiceListView : UserControl
    {
        public InvoiceListView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Printing is handled here in code-behind (not as a ViewModel command)
        /// because WPF's PrintDialog/PrintVisual are UI-thread, View-layer
        /// concerns with no meaningful business logic to unit test — the same
        /// kind of pragmatic exception already made for PasswordBox binding.
        /// </summary>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not InvoiceListViewModel vm || vm.PrintPreviewData is null)
            {
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            // A fresh, off-screen instance sized exactly to A5 — kept separate
            // from the on-screen preview (which is scaled to fit its panel via
            // Viewbox) so the print output is always precisely sized regardless
            // of how the preview happens to be displayed.
            var printView = new InvoicePrintView
            {
                DataContext = vm.PrintPreviewData
            };

            printView.Measure(new Size(printView.Width, printView.Height));
            printView.Arrange(new Rect(0, 0, printView.Width, printView.Height));
            printView.UpdateLayout();

            printDialog.PrintVisual(printView, $"Invoice {vm.PrintPreviewData.Invoice.InvoiceNumber}");
        }
    }
}
