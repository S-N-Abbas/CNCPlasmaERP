using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Core.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReportService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public ReportService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<ReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserContext = currentUserContext;
        }

        public async Task<ProfitByMaterialReport> GetProfitByMaterialAsync(DateTime fromDate, DateTime toDate)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ViewFinancialReports);
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            // Materialize first (ToListAsync), then group/aggregate in memory —
            // required because SellingAmount/CostAmount/ProfitAmount are
            // [NotMapped] computed properties that EF Core cannot translate into
            // SQL; they only exist once the entities are loaded into .NET objects.
            var usages = await _unitOfWork.JobMaterialUsages.Query()
                .Include(u => u.Material)
                .Include(u => u.Job)
                    .ThenInclude(j => j.Invoice)
                .Where(u => u.Job.Invoice != null
                         && u.Job.Invoice.InvoiceDate >= fromDate.Date
                         && u.Job.Invoice.InvoiceDate <= inclusiveToDate)
                .ToListAsync();

            var lines = usages
                .GroupBy(u => new { u.MaterialId, u.Material.Name, u.Material.Gauge })
                .Select(g => new ProfitByMaterialLine
                {
                    MaterialId = g.Key.MaterialId,
                    MaterialName = g.Key.Name,
                    Gauge = g.Key.Gauge,
                    TotalWeightKg = g.Sum(u => u.WeightUsedKg),
                    TotalRevenue = g.Sum(u => u.SellingAmount),
                    TotalCost = g.Sum(u => u.CostAmount)
                })
                .OrderByDescending(l => l.TotalProfit)
                .ToList();

            _logger.LogInformation(
                "Profit-by-material report generated for {FromDate:d} to {ToDate:d}: {LineCount} materials",
                fromDate, toDate, lines.Count);

            return new ProfitByMaterialReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                Lines = lines
            };
        }

        public async Task<ProfitLossSummary> GetProfitLossSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ViewFinancialReports);
            var inclusiveToDate = toDate.Date.AddDays(1).AddTicks(-1);

            var invoices = await _unitOfWork.Invoices.Query()
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= fromDate.Date && i.InvoiceDate <= inclusiveToDate)
                .ToListAsync();

            var allItems = invoices.SelectMany(i => i.Items).ToList();
            var materialRevenue = allItems.Where(it => it.ItemType == InvoiceItemType.Material).Sum(it => it.Amount);
            var serviceRevenue = allItems.Where(it => it.ItemType == InvoiceItemType.Service).Sum(it => it.Amount);
            var carriageRevenue = allItems.Where(it => it.ItemType == InvoiceItemType.Carriage).Sum(it => it.Amount);

            var jobIds = invoices.Select(i => i.JobId).ToList();
            var materialCost = (await _unitOfWork.JobMaterialUsages.Query()
                    .Where(u => jobIds.Contains(u.JobId))
                    .ToListAsync())
                .Sum(u => u.CostAmount);

            var scrapSales = await _unitOfWork.ScrapSales.FindAsync(
                s => s.SaleDate >= fromDate.Date && s.SaleDate <= inclusiveToDate);
            var scrapIncome = scrapSales.Sum(s => s.TotalAmount);

            var expenses = await _unitOfWork.Expenses.FindAsync(
                e => e.ExpenseDate >= fromDate.Date && e.ExpenseDate <= inclusiveToDate);
            var stockPurchases = await _unitOfWork.StockPurchases.FindAsync(
                p => p.PurchaseDate >= fromDate.Date && p.PurchaseDate <= inclusiveToDate);
            var totalExpenses = expenses.Sum(e => e.Amount) + stockPurchases.Sum(p => p.CarriageCost);

            return new ProfitLossSummary
            {
                FromDate = fromDate,
                ToDate = toDate,
                MaterialRevenue = materialRevenue,
                ServiceRevenue = serviceRevenue,
                CarriageRevenue = carriageRevenue,
                ScrapIncome = scrapIncome,
                MaterialCost = materialCost,
                TotalExpenses = totalExpenses
            };
        }

        public async Task<IReadOnlyList<Material>> GetStockReportAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            var materials = await _unitOfWork.Materials.FindAsync(m => m.IsActive);
            return materials.OrderBy(m => m.Name).ToList();
        }
    }
}
