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

        /// <summary>The grid row being edited. Null means the form is in "add new" mode.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(FormTitle))]
        [NotifyPropertyChangedFor(nameof(FormButtonText))]
        private Supplier? selectedSupplier;

        public bool IsEditing => SelectedSupplier is not null;
        public string FormTitle => IsEditing ? "Edit Supplier" : "Add Supplier";
        public string FormButtonText => IsEditing ? "Save Changes" : "Add Supplier";

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
        private bool newIsActive = true;

        partial void OnSelectedSupplierChanged(Supplier? value)
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
            NewIsActive = value.IsActive;
        }

        private void ResetForm()
        {
            NewName = string.Empty;
            NewContactPerson = null;
            NewPhone = null;
            NewEmail = null;
            NewAddress = null;
            NewIsActive = true;
        }

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        /// <summary>Raw helper (not RunBusyAsync-wrapped) so SaveSupplierAsync can reuse it after its own RunBusyAsync has started.</summary>
        private async Task LoadDataAsync()
        {
            var suppliers = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            SelectedSupplier = null; // leaving edit mode also resets the form
        }

        [RelayCommand]
        private void EditSupplier(Supplier supplier)
        {
            SelectedSupplier = supplier;
        }

        [RelayCommand]
        private async Task SaveSupplierAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    ErrorMessage = "Name is required.";
                    return;
                }

                var editingId = SelectedSupplier?.Id;

                var supplier = new Supplier
                {
                    Id = editingId ?? 0,
                    Name = NewName.Trim(),
                    ContactPerson = NewContactPerson,
                    Phone = NewPhone,
                    Email = NewEmail,
                    Address = NewAddress,
                    IsActive = editingId.HasValue ? NewIsActive : true
                };

                int savedId;
                if (editingId.HasValue)
                {
                    await _supplierService.UpdateAsync(supplier);
                    savedId = editingId.Value;
                }
                else
                {
                    var created = await _supplierService.CreateAsync(supplier);
                    savedId = created.Id;
                }

                SelectedSupplier = null;
                await LoadDataAsync();

                // Only a NEW supplier needs announcing: nothing else is open
                // alongside this screen that shows an editable supplier list.
                if (!editingId.HasValue)
                {
                    var fresh = Suppliers.FirstOrDefault(s => s.Id == savedId);
                    if (fresh is not null)
                    {
                        WeakReferenceMessenger.Default.Send(new SupplierCreatedMessage(fresh));
                    }
                }

                StatusMessage = editingId.HasValue ? "Supplier changes saved." : "Supplier added.";
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
