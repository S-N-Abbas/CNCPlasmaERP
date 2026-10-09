using System;
using System.Collections.Generic;
using System.Linq;

namespace CNCPlasmaERP.Core.Reports
{
    public class ProfitByMaterialReport
    {
        public DateTime FromDate { get; init; }
        public DateTime ToDate { get; init; }
        public IReadOnlyList<ProfitByMaterialLine> Lines { get; init; } = Array.Empty<ProfitByMaterialLine>();

        public decimal TotalRevenue => Lines.Sum(l => l.TotalRevenue);
        public decimal TotalCost => Lines.Sum(l => l.TotalCost);
        public decimal TotalProfit => TotalRevenue - TotalCost;
    }
}
