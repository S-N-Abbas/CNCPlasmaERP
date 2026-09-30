using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class StockPurchaseViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;
        private readonly IMaterialService _materialService;
        private readonly ISupplierService _supplierService;

        public StockPurchaseViewModel(
            IStockService stockService,
            IMaterialService materialService,
            ISupplierService supplierService,
            ILogger<StockPurchaseViewModel> logger)
            : base(logger)
        {
            _stockService = stockService;
            _materialService = materialService;
            _supplierService = supplierService;

            _ = LoadAsync();
        }

        public ObservableCollection<Material> Materials { get; } = new();
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public ObservableCollection<StockPurchase> RecentPurchases { get; } = new();

        [ObservableProperty]
        private Material? selectedMaterial;

        [ObservableProperty]
        private Supplier? selectedSupplier;

        [ObservableProperty]
        private decimal weightKg;

        [ObservableProperty]
        private int? sheetCount;

        [ObservableProperty]
        private decimal ratePerKg;

        [ObservableProperty]
        private decimal carriageCost;

        [ObservableProperty]
        private string? supplierInvoiceNumber;

        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private string newSupplierName = string.Empty;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        /// <summary>
        /// Raw loading logic, deliberately NOT wrapped in RunBusyAsync itself —
        /// RecordPurchaseAsync below calls this directly (after its own RunBusyAsync
        /// has already set IsBusy = true) to refresh the grids. Calling the
        /// RunBusyAsync-wrapped LoadAsync() from inside another RunBusyAsync block
        /// would silently no-op, since RunBusyAsync ignores re-entrant calls while busy.
        /// </summary>
        private async Task LoadDataAsync()
        {
            var materials = await _materialService.GetActiveAsync();
            Materials.Clear();
            foreach (var material in materials)
            {
                Materials.Add(material);
            }

            var suppliers = await _supplierService.GetActiveAsync();
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }

            var recentPurchases = await _stockService.GetRecentPurchasesAsync();
            RecentPurchases.Clear();
            foreach (var purchase in recentPurchases)
            {
                RecentPurchases.Add(purchase);
            }
        }

        [RelayCommand]
        private async Task AddSupplierAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewSupplierName))
                {
                    ErrorMessage = "Enter a supplier name first.";
                    return;
                }

                var supplier = await _supplierService.CreateAsync(new Supplier
                {
                    Name = NewSupplierName.Trim(),
                    IsActive = true
                });

                Suppliers.Add(supplier);
                SelectedSupplier = supplier;
                NewSupplierName = string.Empty;
            });
        }

        [RelayCommand]
        private async Task RecordPurchaseAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedMaterial is null || SelectedSupplier is null)
                {
                    ErrorMessage = "Select a material and a supplier.";
                    return;
                }

                if (WeightKg <= 0 || RatePerKg <= 0)
                {
                    ErrorMessage = "Weight and rate must be greater than zero.";
                    return;
                }

                var purchase = new StockPurchase
                {
                    MaterialId = SelectedMaterial.Id,
                    SupplierId = SelectedSupplier.Id,
                    WeightKg = WeightKg,
                    SheetCount = SheetCount,
                    RatePerKg = RatePerKg,
                    CarriageCost = CarriageCost,
                    SupplierInvoiceNumber = SupplierInvoiceNumber,
                    Notes = Notes
                };

                await _stockService.RecordPurchaseAsync(purchase);

                WeightKg = 0;
                SheetCount = null;
                RatePerKg = 0;
                CarriageCost = 0;
                SupplierInvoiceNumber = null;
                Notes = null;

                // Safe: LoadDataAsync is the raw (non-RunBusyAsync-wrapped) helper.
                await LoadDataAsync();
            });
        }
    }
}
