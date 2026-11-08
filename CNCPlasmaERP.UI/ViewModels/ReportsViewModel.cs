using CommunityToolkit.Mvvm.ComponentModel;
using CNCPlasmaERP.UI.ViewModels.Base;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>Container for the Reports screen's two tabs: Profit &amp; Loss and Stock Report.</summary>
    public partial class ReportsViewModel : ViewModelBase
    {
        public ReportsViewModel(ProfitLossReportViewModel profitLoss, StockReportViewModel stockReport, ICurrentUserContext currentUserContext, ILogger<ReportsViewModel> logger)
            : base(logger)
        {
            ProfitLoss = profitLoss;
            StockReport = stockReport;
            CanViewStockReport = currentUserContext.HasPermission(Permission.ManageStock);
        }

        public ProfitLossReportViewModel ProfitLoss { get; }
        public StockReportViewModel StockReport { get; }
        public bool CanViewStockReport { get; }

        [ObservableProperty]
        private int selectedTabIndex;

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (value == 0)
            {
                ProfitLoss.LoadCommand.Execute(null);
            }
            else if (value == 1 && CanViewStockReport)
            {
                StockReport.LoadCommand.Execute(null);
            }
        }
    }
}
