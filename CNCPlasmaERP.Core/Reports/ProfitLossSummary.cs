using System;

namespace CNCPlasmaERP.Core.Reports
{
    /// <summary>
    /// Overall Profit &amp; Loss for a date range — combines invoiced job revenue
    /// (material, service, carriage), scrap sale income, material cost, and
    /// operating expenses. Revenue is recognized at invoicing, not at job
    /// completion, matching standard small-business practice.
    /// </summary>
    public class ProfitLossSummary
    {
        public DateTime FromDate { get; init; }
        public DateTime ToDate { get; init; }

        public decimal MaterialRevenue { get; init; }
        public decimal ServiceRevenue { get; init; }
        public decimal CarriageRevenue { get; init; }
        public decimal ScrapIncome { get; init; }
        public decimal TotalRevenue => MaterialRevenue + ServiceRevenue + CarriageRevenue + ScrapIncome;

        public decimal MaterialCost { get; init; }
        public decimal GrossProfit => TotalRevenue - MaterialCost;

        public decimal TotalExpenses { get; init; }
        public decimal NetProfit => GrossProfit - TotalExpenses;
    }
}
