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
    public partial class QuotationsViewModel : ViewModelBase, IRecipient<CustomerCreatedMessage>, IRecipient<MaterialCreatedMessage>
    {
        private readonly IQuotationService _quotationService;
        private readonly ICustomerService _customerService;
        private readonly IMaterialService _materialService;
        private readonly IJobService _jobService;

        public QuotationsViewModel(
            IQuotationService quotationService,
            ICustomerService customerService,
            IMaterialService materialService,
            IJobService jobService,
            ILogger<QuotationsViewModel> logger)
            : base(logger)
        {
            _quotationService = quotationService;
            _customerService = customerService;
            _materialService = materialService;
            _jobService = jobService;

            DraftItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DraftTotal));

            WeakReferenceMessenger.Default.RegisterAll(this);
            _ = InitializeAsync();
        }

        public ObservableCollection<Quotation> Quotations { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Material> Materials { get; } = new();

        /// <summary>Items added to the quotation being drafted — only saved when "Create Quotation" is pressed.</summary>
        public ObservableCollection<QuotationItem> DraftItems { get; } = new();

        // ----- Selection / detail -----

        [ObservableProperty]
        private Quotation? selectedQuotation;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MarkAsSentCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConvertToJobCommand))]
        private Quotation? selectedQuotationDetail;

        /// <summary>0 = Details tab, 1 = New Quotation tab. Bound two-way to the TabControl.</summary>
        [ObservableProperty]
        private int selectedTabIndex;

        partial void OnSelectedQuotationChanged(Quotation? value)
        {
            if (value is not null)
            {
                SelectedTabIndex = 0;
            }

            _ = RunBusyAsync(LoadDetailDataAsync);
        }

        // ----- New quotation form -----

        [ObservableProperty]
        private Customer? newCustomer;

        [ObservableProperty]
        private DateTime? newExpiryDate;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DraftTotal))]
        private decimal newServiceCharges;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DraftTotal))]
        private decimal newCarriageCharges;

        [ObservableProperty]
        private string? newNotes;

        // ----- Item entry -----

        [ObservableProperty]
        private string itemDescription = string.Empty;

        [ObservableProperty]
        private Material? itemMaterial;

        [ObservableProperty]
        private decimal itemWeightKg;

        [ObservableProperty]
        private decimal itemRatePerKg;

        [ObservableProperty]
        private decimal itemAmount;

        partial void OnItemWeightKgChanged(decimal value) => RecalculateItemAmount();

        partial void OnItemRatePerKgChanged(decimal value) => RecalculateItemAmount();

        /// <summary>When both weight and rate are entered the amount is worked out for you; otherwise type the amount directly.</summary>
        private void RecalculateItemAmount()
        {
            if (ItemWeightKg > 0 && ItemRatePerKg > 0)
            {
                ItemAmount = Math.Round(ItemWeightKg * ItemRatePerKg, 2, MidpointRounding.AwayFromZero);
            }
        }

        public decimal DraftTotal => DraftItems.Sum(i => i.EstimatedAmount) + NewServiceCharges + NewCarriageCharges;

        // ----- Loading -----

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

                var materials = await _materialService.GetActiveAsync();
                Materials.Clear();
                foreach (var material in materials)
                {
                    Materials.Add(material);
                }

                await LoadListDataAsync(null);
            });
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                var keepSelectedId = SelectedQuotation?.Id;
                await LoadListDataAsync(keepSelectedId);
                await LoadDetailDataAsync();
            });
        }

        /// <summary>
        /// Raw helper (not RunBusyAsync-wrapped). Reloading the list clears the
        /// grid, which drops the selection, so the caller passes the Id to
        /// re-select afterwards.
        /// </summary>
        private async Task LoadListDataAsync(int? reselectId)
        {
            var quotations = await _quotationService.GetAllAsync();

            Quotations.Clear();
            foreach (var quotation in quotations)
            {
                Quotations.Add(quotation);
            }

            if (reselectId.HasValue)
            {
                SelectedQuotation = Quotations.FirstOrDefault(q => q.Id == reselectId.Value);
            }
        }

        private async Task LoadDetailDataAsync()
        {
            if (SelectedQuotation is null)
            {
                SelectedQuotationDetail = null;
                return;
            }

            SelectedQuotationDetail = await _quotationService.GetWithDetailsAsync(SelectedQuotation.Id);
        }

        // ----- Draft items (in memory only, no database) -----

        [RelayCommand]
        private void AddDraftItem()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(ItemDescription))
            {
                ErrorMessage = "Enter a description for the item.";
                return;
            }

            if (ItemAmount <= 0)
            {
                ErrorMessage = "The item amount must be greater than zero.";
                return;
            }

            DraftItems.Add(new QuotationItem
            {
                Description = ItemDescription.Trim(),
                MaterialId = ItemMaterial?.Id,
                Material = ItemMaterial, // display only; QuotationService strips it before saving
                EstimatedWeightKg = ItemWeightKg > 0 ? ItemWeightKg : null,
                EstimatedRatePerKg = ItemRatePerKg > 0 ? ItemRatePerKg : null,
                EstimatedAmount = ItemAmount
            });

            ItemDescription = string.Empty;
            ItemMaterial = null;
            ItemWeightKg = 0;
            ItemRatePerKg = 0;
            ItemAmount = 0;
        }

        [RelayCommand]
        private void RemoveDraftItem(QuotationItem item)
        {
            DraftItems.Remove(item);
        }

        // ----- Saving / actions -----

        [RelayCommand]
        private async Task CreateQuotationAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = null;

                if (NewCustomer is null)
                {
                    ErrorMessage = "Select a customer.";
                    return;
                }

                if (DraftItems.Count == 0 && NewServiceCharges <= 0 && NewCarriageCharges <= 0)
                {
                    ErrorMessage = "Add at least one item, or enter a service/carriage charge.";
                    return;
                }

                var quotation = new Quotation
                {
                    CustomerId = NewCustomer.Id,
                    ExpiryDate = NewExpiryDate,
                    EstimatedServiceCharges = NewServiceCharges,
                    EstimatedCarriageCharges = NewCarriageCharges,
                    Notes = NewNotes
                };

                var created = await _quotationService.CreateAsync(quotation, DraftItems.ToList());

                NewCustomer = null;
                NewExpiryDate = null;
                NewServiceCharges = 0;
                NewCarriageCharges = 0;
                NewNotes = null;
                DraftItems.Clear();

                StatusMessage = $"Quotation {created.QuotationNumber} created.";

                // Select the new one directly and load its detail explicitly —
                // OnSelectedQuotationChanged's own RunBusyAsync call would no-op
                // here because this method's RunBusyAsync is still running.
                await LoadListDataAsync(created.Id);
                await LoadDetailDataAsync();
                SelectedTabIndex = 0;
            });
        }

        private bool CanMarkAsSent() => SelectedQuotationDetail?.Status == QuotationStatus.Draft;

        [RelayCommand(CanExecute = nameof(CanMarkAsSent))]
        private async Task MarkAsSentAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedQuotation is null)
                {
                    return;
                }

                StatusMessage = null;
                var id = SelectedQuotation.Id;

                await _quotationService.MarkAsSentAsync(id);

                await LoadListDataAsync(id);
                await LoadDetailDataAsync();
            });
        }

        private bool CanConvertToJob() =>
            SelectedQuotationDetail is not null && SelectedQuotationDetail.Status != QuotationStatus.Converted;

        [RelayCommand(CanExecute = nameof(CanConvertToJob))]
        private async Task ConvertToJobAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedQuotation is null)
                {
                    return;
                }

                StatusMessage = null;
                var id = SelectedQuotation.Id;

                var job = await _jobService.ConvertQuotationToJobAsync(id);

                StatusMessage = $"Converted to job {job.JobNumber}. Open Jobs to add the material used.";

                await LoadListDataAsync(id);
                await LoadDetailDataAsync();
            });
        }

        // ----- Keep dropdowns in sync with other screens -----

        public void Receive(CustomerCreatedMessage message)
        {
            if (Customers.All(c => c.Id != message.Value.Id))
            {
                Customers.Add(message.Value);
            }
        }

        public void Receive(MaterialCreatedMessage message)
        {
            if (Materials.All(m => m.Id != message.Value.Id))
            {
                Materials.Add(message.Value);
            }
        }
    }
}
