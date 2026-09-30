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
    public partial class CustomerListViewModel : ViewModelBase, IRecipient<CustomerCreatedMessage>
    {
        private readonly ICustomerService _customerService;

        public CustomerListViewModel(ICustomerService customerService, ILogger<CustomerListViewModel> logger)
            : base(logger)
        {
            _customerService = customerService;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _ = LoadAsync();
        }

        public ObservableCollection<Customer> Customers { get; } = new();

        [ObservableProperty]
        private string newName = string.Empty;

        [ObservableProperty]
        private string? newContactPerson;

        [ObservableProperty]
        private string? newPhone;

        [ObservableProperty]
        private string? newEmail;

        [ObservableProperty]
        private string? newAddress;

        [ObservableProperty]
        private decimal newOpeningBalance;

        [ObservableProperty]
        private decimal? newCreditLimit;

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                var customers = await _customerService.GetAllAsync();
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }
            });
        }

        [RelayCommand]
        private async Task AddCustomerAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "Name is required.";
                    return;
                }

                var customer = new Customer
                {
                    Name = NewName.Trim(),
                    ContactPerson = NewContactPerson,
                    Phone = NewPhone,
                    Email = NewEmail,
                    Address = NewAddress,
                    OpeningBalance = NewOpeningBalance,
                    CreditLimit = NewCreditLimit,
                    IsActive = true
                };

                await _customerService.CreateAsync(customer);
                Customers.Add(customer);

                WeakReferenceMessenger.Default.Send(new CustomerCreatedMessage(customer));

                NewName = string.Empty;
                NewContactPerson = null;
                NewPhone = null;
                NewEmail = null;
                NewAddress = null;
                NewOpeningBalance = 0;
                NewCreditLimit = null;
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
