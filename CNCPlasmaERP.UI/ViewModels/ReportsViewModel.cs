using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>Container for the Reports screen's two tabs: Profit &amp; Loss and Stock Report.</summary>
    public partial class ReportsViewModel : ViewModelBase
    {
        public ReportsViewModel(ProfitLossReportViewModel profitLoss, StockReportViewModel stockReport, ILogger<ReportsViewModel> logger)
            : base(logger)
        {
            ProfitLoss = profitLoss;
            StockReport = stockReport;
        }

        public ProfitLossReportViewModel ProfitLoss { get; }
        public StockReportViewModel StockReport { get; }
    }
}
