using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.Models;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class InvoiceListViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISettingsService _settingsService;

        private AppSettings? _companySettings;

        public InvoiceListViewModel(IInvoiceService invoiceService, ISettingsService settingsService, ILogger<InvoiceListViewModel> logger)
            : base(logger)
        {
            _invoiceService = invoiceService;
            _settingsService = settingsService;

            _ = LoadAsync();
        }

        public ObservableCollection<Invoice> Invoices { get; } = new();

        [ObservableProperty]
        private Invoice? selectedInvoice;

        [ObservableProperty]
        private Invoice? selectedInvoiceDetail;

        [ObservableProperty]
        private InvoicePrintData? printPreviewData;

        partial void OnSelectedInvoiceChanged(Invoice? value) => _ = RunBusyAsync(LoadDetailDataAsync);

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadListDataAsync);

        private async Task LoadListDataAsync()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            Invoices.Clear();
            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }

        private async Task LoadDetailDataAsync()
        {
            if (SelectedInvoice is null)
            {
                SelectedInvoiceDetail = null;
                PrintPreviewData = null;
                return;
            }

            SelectedInvoiceDetail = await _invoiceService.GetInvoiceWithDetailsAsync(SelectedInvoice.Id);
            _companySettings ??= await _settingsService.GetSettingsAsync();

            PrintPreviewData = SelectedInvoiceDetail is not null
                ? new InvoicePrintData { Invoice = SelectedInvoiceDetail, Settings = _companySettings! }
                : null;
        }
    }
}
