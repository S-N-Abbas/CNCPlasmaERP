using System;

namespace CNCPlasmaERP.Core.Reports
{
    /// <summary>One material's profit contribution within a date range, derived from invoiced job material usage.</summary>
    public class ProfitByMaterialLine
    {
        public int MaterialId { get; init; }
        public string MaterialName { get; init; } = string.Empty;
        public string Gauge { get; init; } = string.Empty;

        public decimal TotalWeightKg { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal TotalCost { get; init; }

        public decimal TotalProfit => TotalRevenue - TotalCost;

        public decimal ProfitMarginPercent => TotalRevenue == 0 ? 0 : Math.Round(TotalProfit / TotalRevenue * 100, 1);
    }
}
