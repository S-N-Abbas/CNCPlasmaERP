using CommunityToolkit.Mvvm.ComponentModel;
using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>
    /// Container for the Stock screen's three tabs. Holds no logic of its own —
    /// MaterialsViewModel, StockPurchaseViewModel and ScrapViewModel are
    /// resolved via DI (each gets the full ViewModelBase treatment
    /// independently) and simply exposed here for the TabControl in
    /// StockView.xaml to bind against.
    /// </summary>
    public partial class StockViewModel : ViewModelBase
    {
        public StockViewModel(
            MaterialsViewModel materialsViewModel,
            StockPurchaseViewModel purchaseViewModel,
            ScrapViewModel scrapViewModel,
            ILogger<StockViewModel> logger)
            : base(logger)
        {
            Materials = materialsViewModel;
            Purchases = purchaseViewModel;
            Scrap = scrapViewModel;
        }

        public MaterialsViewModel Materials { get; }
        public StockPurchaseViewModel Purchases { get; }
        public ScrapViewModel Scrap { get; }

        [ObservableProperty]
        private int selectedTabIndex;

        partial void OnSelectedTabIndexChanged(int value)
        {
            switch (value)
            {
                case 0:
                    Materials.LoadCommand.Execute(null);
                    break;
                case 1:
                    Purchases.LoadCommand.Execute(null);
                    break;
                case 2:
                    Scrap.LoadCommand.Execute(null);
                    break;
            }
        }
    }
}
