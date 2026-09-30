using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.Messages;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class CustomerLedgerViewModel : ViewModelBase, IRecipient<CustomerCreatedMessage>
    {
        private readonly ICustomerService _customerService;
        private readonly ILedgerService _ledgerService;

        public CustomerLedgerViewModel(ICustomerService customerService, ILedgerService ledgerService, ILogger<CustomerLedgerViewModel> logger)
            : base(logger)
        {
            _customerService = customerService;
            _ledgerService = ledgerService;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _ = LoadCustomersAsync();
        }

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<CustomerLedgerEntry> LedgerEntries { get; } = new();

        [ObservableProperty]
        private Customer? selectedCustomer;

        // CommunityToolkit.Mvvm source-generates the call site for this from the
        // [ObservableProperty] above — it fires automatically whenever SelectedCustomer changes.
        partial void OnSelectedCustomerChanged(Customer? value)
        {
            _ = LoadLedgerAsync();
        }

        [RelayCommand]
        private async Task LoadCustomersAsync()
        {
            await RunBusyAsync(async () =>
            {
                var customers = await _customerService.GetActiveAsync();
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }
            });
        }

        private async Task LoadLedgerAsync()
        {
            if (SelectedCustomer is null)
            {
                LedgerEntries.Clear();
                return;
            }

            await RunBusyAsync(async () =>
            {
                var entries = await _ledgerService.GetStatementAsync(SelectedCustomer.Id);
                LedgerEntries.Clear();
                foreach (var entry in entries)
                {
                    LedgerEntries.Add(entry);
                }
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
