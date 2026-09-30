using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(IUnitOfWork unitOfWork, ILogger<SupplierService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Supplier>> GetActiveAsync()
        {
            var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
            return suppliers.OrderBy(s => s.Name).ToList();
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Supplier '{Name}' created", supplier.Name);

            return supplier;
        }
    }
}
