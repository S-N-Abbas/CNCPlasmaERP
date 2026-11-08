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
    public partial class ScrapViewModel : ViewModelBase
    {
        private readonly IScrapService _scrapService;

        public ScrapViewModel(IScrapService scrapService, ILogger<ScrapViewModel> logger)
            : base(logger)
        {
            _scrapService = scrapService;
            _ = LoadAsync();
        }

        /// <summary>Every material's scrap balance (including zero), for the balances grid.</summary>
        public ObservableCollection<Scrap> Balances { get; } = new();

        /// <summary>Only materials with scrap actually on hand — what the "sell" dropdown offers.</summary>
        public ObservableCollection<Scrap> SellableBalances { get; } = new();

        public ObservableCollection<ScrapSale> RecentSales { get; } = new();

        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty]
        private Scrap? selectedScrap;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPreview))]
        private decimal weightKg;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPreview))]
        private decimal ratePerKg;

        [ObservableProperty]
        private string buyerName = string.Empty;

        [ObservableProperty]
        private PaymentMethod method = PaymentMethod.Cash;

        [ObservableProperty]
        private string? notes;

        /// <summary>Live "weight x rate" shown under the form so the amount is visible before saving.</summary>
        public decimal TotalPreview => WeightKg * RatePerKg;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        /// <summary>Raw helper (not RunBusyAsync-wrapped) so RecordSaleAsync can reuse it after its own RunBusyAsync has started.</summary>
        private async Task LoadDataAsync()
        {
            var selectedMaterialId = SelectedScrap?.MaterialId;
            var balances = await _scrapService.GetBalancesAsync();
            Balances.Clear();
            SellableBalances.Clear();
            foreach (var balance in balances)
            {
                Balances.Add(balance);
                if (balance.BalanceKg > 0)
                {
                    SellableBalances.Add(balance);
                }
            }

            var sales = await _scrapService.GetRecentSalesAsync();
            RecentSales.Clear();
            foreach (var sale in sales)
            {
                RecentSales.Add(sale);
            }

            SelectedScrap = selectedMaterialId.HasValue
                ? SellableBalances.FirstOrDefault(s => s.MaterialId == selectedMaterialId.Value)
                : null;
        }

        [RelayCommand]
        private async Task RecordSaleAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = null;

                if (SelectedScrap is null)
                {
                    ErrorMessage = "Select the material whose scrap is being sold.";
                    return;
                }

                if (WeightKg <= 0 || RatePerKg <= 0)
                {
                    ErrorMessage = "Weight and rate must be greater than zero.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(BuyerName))
                {
                    ErrorMessage = "Enter the buyer's name.";
                    return;
                }

                var materialName = SelectedScrap.Material.Name;

                var sale = new ScrapSale
                {
                    MaterialId = SelectedScrap.MaterialId,
                    WeightKgSold = WeightKg,
                    RatePerKg = RatePerKg,
                    BuyerName = BuyerName.Trim(),
                    PaymentMethod = Method,
                    Notes = Notes
                };

                // Throws InsufficientScrapException (a DomainException) if the
                // balance is too low — RunBusyAsync shows its message inline.
                await _scrapService.RecordSaleAsync(sale);

                StatusMessage = $"Sold {WeightKg:N2} Kg of {materialName} scrap for {sale.TotalAmount:N2}.";

                SelectedScrap = null;
                WeightKg = 0;
                RatePerKg = 0;
                BuyerName = string.Empty;
                Notes = null;

                await LoadDataAsync();
            });
        }
    }
}
