using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class SupplierLedgerViewModel : ViewModelBase
    {
        private readonly ISupplierService _supplierService;
        private readonly ISupplierLedgerService _ledgerService;
        private readonly ICurrentUserContext _currentUserContext;

        public SupplierLedgerViewModel(ISupplierService supplierService, ISupplierLedgerService ledgerService,
            ICurrentUserContext currentUserContext, ILogger<SupplierLedgerViewModel> logger) : base(logger)
        {
            _supplierService = supplierService;
            _ledgerService = ledgerService;
            _currentUserContext = currentUserContext;
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            _ = LoadSuppliersAsync();
        }

        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<SupplierLedgerEntry> Entries { get; } = new();
        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty] private Supplier? selectedSupplier;
        [ObservableProperty] private DateTime fromDate;
        [ObservableProperty] private DateTime toDate;
        [ObservableProperty] private decimal paymentAmount;
        [ObservableProperty] private PaymentMethod paymentMethod = PaymentMethod.Cash;
        [ObservableProperty] private string? referenceNumber;
        [ObservableProperty] private string? paymentNotes;

        partial void OnSelectedSupplierChanged(Supplier? value) => _ = LoadEntriesAsync();
        partial void OnFromDateChanged(DateTime value) => _ = LoadEntriesAsync();
        partial void OnToDateChanged(DateTime value) => _ = LoadEntriesAsync();

        [RelayCommand]
        private async Task LoadSuppliersAsync() => await RunBusyAsync(async () =>
        {
            var id = SelectedSupplier?.Id;
            var suppliers = await _supplierService.GetActiveAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers) Suppliers.Add(supplier);
            SelectedSupplier = id.HasValue ? Suppliers.FirstOrDefault(s => s.Id == id.Value) : Suppliers.FirstOrDefault();
            await LoadEntriesDataAsync();
        });

        [RelayCommand]
        private async Task RefreshAsync() => await RunBusyAsync(async () =>
        {
            var id = SelectedSupplier?.Id;
            var suppliers = await _supplierService.GetActiveAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers) Suppliers.Add(supplier);
            SelectedSupplier = id.HasValue ? Suppliers.FirstOrDefault(s => s.Id == id.Value) : null;
            await LoadEntriesDataAsync();
        });

        private async Task LoadEntriesAsync()
        {
            if (IsBusy) return;
            await RunBusyAsync(LoadEntriesDataAsync);
        }

        private async Task LoadEntriesDataAsync()
        {
            Entries.Clear();
            if (SelectedSupplier is null || ToDate.Date < FromDate.Date) return;
            var entries = await _ledgerService.GetStatementAsync(SelectedSupplier.Id, FromDate, ToDate);
            foreach (var entry in entries) Entries.Add(entry);
        }

        [RelayCommand]
        private async Task RecordPaymentAsync() => await RunBusyAsync(async () =>
        {
            if (SelectedSupplier is null) { ErrorMessage = "Select a supplier."; return; }
            if (PaymentAmount <= 0) { ErrorMessage = "Payment amount must be greater than zero."; return; }
            await _ledgerService.RecordPaymentAsync(new SupplierPayment
            {
                SupplierId = SelectedSupplier.Id,
                Amount = PaymentAmount,
                PaymentDate = DateTime.Now,
                Method = PaymentMethod,
                ReferenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
                PaidByUserId = _currentUserContext.CurrentUser?.Id
            });
            PaymentAmount = 0;
            ReferenceNumber = null;
            PaymentNotes = null;
            StatusMessage = "Supplier payment recorded.";
            await LoadSuppliersDataAsync();
        });

        private async Task LoadSuppliersDataAsync()
        {
            var suppliers = await _supplierService.GetActiveAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers) Suppliers.Add(supplier);
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == SelectedSupplier?.Id);
            await LoadEntriesDataAsync();
        }
    }
}
