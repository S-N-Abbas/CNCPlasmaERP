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

        /// <summary>The grid row being edited. Null means the form is in "add new" mode.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(FormTitle))]
        [NotifyPropertyChangedFor(nameof(FormButtonText))]
        private Customer? selectedCustomer;

        public bool IsEditing => SelectedCustomer is not null;
        public string FormTitle => IsEditing ? "Edit Customer" : "Add Customer";
        public string FormButtonText => IsEditing ? "Save Changes" : "Add Customer";

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

        /// <summary>Editable only when adding. While editing it is shown read-only: the balance is owned by the ledger.</summary>
        [ObservableProperty]
        private decimal newOpeningBalance;

        [ObservableProperty]
        private decimal? newCreditLimit;

        [ObservableProperty]
        private bool newIsActive = true;

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            ErrorMessage = null;

            if (value is null)
            {
                ResetForm();
                return;
            }

            NewName = value.Name;
            NewContactPerson = value.ContactPerson;
            NewPhone = value.Phone;
            NewEmail = value.Email;
            NewAddress = value.Address;
            NewOpeningBalance = value.OpeningBalance;
            NewCreditLimit = value.CreditLimit;
            NewIsActive = value.IsActive;
        }

        private void ResetForm()
        {
            NewName = string.Empty;
            NewContactPerson = null;
            NewPhone = null;
            NewEmail = null;
            NewAddress = null;
            NewOpeningBalance = 0;
            NewCreditLimit = null;
            NewIsActive = true;
        }

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        /// <summary>Raw helper (not RunBusyAsync-wrapped) so SaveCustomerAsync can reuse it after its own RunBusyAsync has started.</summary>
        private async Task LoadDataAsync()
        {
            var selectedCustomerId = SelectedCustomer?.Id;
            var customers = await _customerService.GetAllAsync();
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            SelectedCustomer = selectedCustomerId.HasValue
                ? Customers.FirstOrDefault(c => c.Id == selectedCustomerId.Value)
                : null;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            SelectedCustomer = null; // leaving edit mode also resets the form
        }

        [RelayCommand]
        private void EditCustomer(Customer customer)
        {
            SelectedCustomer = customer;
        }

        [RelayCommand]
        private async Task SaveCustomerAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "Name is required.";
                    return;
                }

                var editingId = SelectedCustomer?.Id;

                // A detached object carrying only the form's values. When
                // editing, the service copies just the editable fields —
                // balances are never touched.
                var customer = new Customer
                {
                    Id = editingId ?? 0,
                    Name = NewName.Trim(),
                    ContactPerson = NewContactPerson,
                    Phone = NewPhone,
                    Email = NewEmail,
                    Address = NewAddress,
                    OpeningBalance = editingId.HasValue ? 0 : NewOpeningBalance,
                    CreditLimit = NewCreditLimit,
                    IsActive = editingId.HasValue ? NewIsActive : true
                };

                int savedId;
                if (editingId.HasValue)
                {
                    await _customerService.UpdateAsync(customer);
                    savedId = editingId.Value;
                }
                else
                {
                    var created = await _customerService.CreateAsync(customer);
                    savedId = created.Id;
                }

                SelectedCustomer = null;
                await LoadDataAsync();

                var fresh = Customers.FirstOrDefault(c => c.Id == savedId);
                if (fresh is not null)
                {
                    if (editingId.HasValue)
                    {
                        WeakReferenceMessenger.Default.Send(new CustomerUpdatedMessage(fresh));
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new CustomerCreatedMessage(fresh));
                    }
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
