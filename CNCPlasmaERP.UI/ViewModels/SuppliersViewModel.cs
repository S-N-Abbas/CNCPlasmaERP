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
    public partial class SuppliersViewModel : ViewModelBase, IRecipient<SupplierCreatedMessage>
    {
        private readonly ISupplierService _supplierService;

        public SuppliersViewModel(ISupplierService supplierService, ILogger<SuppliersViewModel> logger)
            : base(logger)
        {
            _supplierService = supplierService;

            WeakReferenceMessenger.Default.RegisterAll(this);
            _ = LoadAsync();
        }

        public ObservableCollection<Supplier> Suppliers { get; } = new();

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

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                var suppliers = await _supplierService.GetAllAsync();
                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
            });
        }

        [RelayCommand]
        private async Task AddSupplierAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "Name is required.";
                    return;
                }

                var supplier = new Supplier
                {
                    Name = NewName.Trim(),
                    ContactPerson = NewContactPerson,
                    Phone = NewPhone,
                    Email = NewEmail,
                    Address = NewAddress,
                    IsActive = true
                };

                await _supplierService.CreateAsync(supplier);
                Suppliers.Add(supplier);

                WeakReferenceMessenger.Default.Send(new SupplierCreatedMessage(supplier));

                NewName = string.Empty;
                NewContactPerson = null;
                NewPhone = null;
                NewEmail = null;
                NewAddress = null;
            });
        }

        public void Receive(SupplierCreatedMessage message)
        {
            if (Suppliers.All(s => s.Id != message.Value.Id))
            {
                Suppliers.Add(message.Value);
            }
        }
    }
}
