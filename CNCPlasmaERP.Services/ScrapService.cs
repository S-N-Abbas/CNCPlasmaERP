using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class ScrapService : IScrapService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStockService _stockService;
        private readonly ILogger<ScrapService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public ScrapService(IUnitOfWork unitOfWork, IStockService stockService, ICurrentUserContext currentUserContext, ILogger<ScrapService> logger)
        {
            _unitOfWork = unitOfWork;
            _stockService = stockService;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        // Both read queries use AsNoTracking: they exist only to display data,
        // and this app shares one DbContext for the whole session, so tracked
        // display copies would linger and risk identity conflicts later.
        public async Task<IReadOnlyList<Scrap>> GetBalancesAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            return await _unitOfWork.Scraps.Query()
                .AsNoTracking()
                .Include(s => s.Material)
                .OrderBy(s => s.Material.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ScrapSale>> GetRecentSalesAsync(int count = 50)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            return await _unitOfWork.ScrapSales.Query()
                .AsNoTracking()
                .Include(s => s.Material)
                .OrderByDescending(s => s.SaleDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task RecordSaleAsync(ScrapSale sale)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            _logger.LogInformation(
                "Recording scrap sale: {WeightKg} Kg of Material {MaterialId} to '{Buyer}'",
                sale.WeightKgSold, sale.MaterialId, sale.BuyerName);

            await _stockService.RecordScrapSaleAsync(sale);
        }
    }
}
