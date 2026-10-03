using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>Container for the Invoicing screen's three tabs: generate, list/print, and payments.</summary>
    public partial class InvoicingViewModel : ViewModelBase
    {
        public InvoicingViewModel(
            GenerateInvoiceViewModel generateInvoice,
            InvoiceListViewModel invoiceList,
            PaymentsViewModel payments,
            ILogger<InvoicingViewModel> logger)
            : base(logger)
        {
            GenerateInvoice = generateInvoice;
            InvoiceList = invoiceList;
            Payments = payments;
        }

        public GenerateInvoiceViewModel GenerateInvoice { get; }
        public InvoiceListViewModel InvoiceList { get; }
        public PaymentsViewModel Payments { get; }
    }
}
