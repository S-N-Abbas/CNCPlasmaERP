using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>
    /// Container for the Stock screen's two tabs. Holds no logic of its own —
    /// MaterialsViewModel and StockPurchaseViewModel are resolved via DI (each
    /// gets the full ViewModelBase treatment independently) and simply exposed
    /// here for the TabControl in StockView.xaml to bind against.
    /// </summary>
    public partial class StockViewModel : ViewModelBase
    {
        public StockViewModel(MaterialsViewModel materialsViewModel, StockPurchaseViewModel purchaseViewModel, ILogger<StockViewModel> logger)
            : base(logger)
        {
            Materials = materialsViewModel;
            Purchases = purchaseViewModel;
        }

        public MaterialsViewModel Materials { get; }
        public StockPurchaseViewModel Purchases { get; }
    }
}
