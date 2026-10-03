using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.Messages;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class PaymentsViewModel : ViewModelBase, IRecipient<CustomerCreatedMessage>
    {
        private readonly ICustomerService _customerService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILedgerService _ledgerService;
        private readonly ICurrentUserContext _currentUserContext;

        public PaymentsViewModel(
            ICustomerService customerService,
            IInvoiceService invoiceService,
            ILedgerService ledgerService,
            ICurrentUserContext currentUserContext,
            ILogger<PaymentsViewModel> logger)
            : base(logger)
        {
            _customerService = customerService;
            _invoiceService = invoiceService;
            _ledgerService = ledgerService;
            _currentUserContext = currentUserContext;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _ = InitializeAsync();
        }

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Invoice> OutstandingInvoices { get; } = new();
        public ObservableCollection<Payment> RecentPayments { get; } = new();

        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty]
        private Customer? selectedCustomer;

        [ObservableProperty]
        private Invoice? selectedInvoice;

        [ObservableProperty]
        private bool isAdvance;

        [ObservableProperty]
        private decimal amount;

        [ObservableProperty]
        private PaymentMethod method = PaymentMethod.Cash;

        [ObservableProperty]
        private string? referenceNumber;

        [ObservableProperty]
        private string? notes;

        partial void OnSelectedCustomerChanged(Customer? value) => _ = RunBusyAsync(LoadOutstandingInvoicesAsync);

        private async Task InitializeAsync()
        {
            await RunBusyAsync(async () =>
            {
                var customers = await _customerService.GetActiveAsync();
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }

                await LoadRecentPaymentsAsync();
            });
        }

        private async Task LoadOutstandingInvoicesAsync()
        {
            OutstandingInvoices.Clear();
            SelectedInvoice = null;

            if (SelectedCustomer is null)
            {
                return;
            }

            var invoices = await _invoiceService.GetOutstandingInvoicesForCustomerAsync(SelectedCustomer.Id);
            foreach (var invoice in invoices)
            {
                OutstandingInvoices.Add(invoice);
            }
        }

        private async Task LoadRecentPaymentsAsync()
        {
            var payments = await _ledgerService.GetRecentPaymentsAsync();
            RecentPayments.Clear();
            foreach (var payment in payments)
            {
                RecentPayments.Add(payment);
            }
        }

        [RelayCommand]
        private async Task RecordPaymentAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedCustomer is null)
                {
                    ErrorMessage = "Select a customer.";
                    return;
                }

                if (!IsAdvance && SelectedInvoice is null)
                {
                    ErrorMessage = "Select an invoice, or check 'Advance Payment'.";
                    return;
                }

                if (Amount <= 0)
                {
                    ErrorMessage = "Amount must be greater than zero.";
                    return;
                }

                var payment = new Payment
                {
                    CustomerId = SelectedCustomer.Id,
                    InvoiceId = IsAdvance ? null : SelectedInvoice!.Id,
                    Amount = Amount,
                    Method = Method,
                    ReferenceNumber = ReferenceNumber,
                    IsAdvance = IsAdvance,
                    Notes = Notes,
                    ReceivedByUserId = _currentUserContext.CurrentUser?.Id
                };

                await _ledgerService.PostPaymentAsync(payment);

                Amount = 0;
                ReferenceNumber = null;
                Notes = null;
                IsAdvance = false;

                await LoadOutstandingInvoicesAsync();
                await LoadRecentPaymentsAsync();
            });
        }

        public void Receive(CustomerCreatedMessage message)
        {
            if (Customers.All(c => c.Id != message.Value.Id))
            {
                Customers.Add(message.Value);
            }
        }
    }
}
