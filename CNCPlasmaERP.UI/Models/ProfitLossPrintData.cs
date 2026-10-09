using System.Collections.Generic;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Reports;

namespace CNCPlasmaERP.UI.Models
{
    public class ProfitLossPrintData
    {
        public AppSettings Settings { get; init; } = null!;
        public ProfitLossSummary Summary { get; init; } = null!;
        public IReadOnlyList<ProfitByMaterialLine> MaterialLines { get; init; } = new List<ProfitByMaterialLine>();
    }
}
