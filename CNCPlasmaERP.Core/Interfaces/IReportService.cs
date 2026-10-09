using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Reports;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IReportService
    {
        /// <summary>Per-material profit breakdown for invoiced job material usage within the date range.</summary>
        Task<ProfitByMaterialReport> GetProfitByMaterialAsync(DateTime fromDate, DateTime toDate);

        /// <summary>Overall P&amp;L combining job revenue, material cost, scrap income and expenses.</summary>
        Task<ProfitLossSummary> GetProfitLossSummaryAsync(DateTime fromDate, DateTime toDate);

        /// <summary>Current stock levels for every active material.</summary>
        Task<IReadOnlyList<Material>> GetStockReportAsync();
    }
}
