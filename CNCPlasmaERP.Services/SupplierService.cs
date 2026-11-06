using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public SupplierService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<SupplierService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserContext = currentUserContext;
        }

        public async Task<IReadOnlyList<Supplier>> GetAllAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            return suppliers.OrderBy(s => s.Name).ToList();
        }

        public async Task<IReadOnlyList<Supplier>> GetActiveAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
            return suppliers.OrderBy(s => s.Name).ToList();
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new BusinessValidationException("Supplier name is required.");
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Supplier '{Name}' created", supplier.Name);

            return supplier;
        }

        /// <summary>
        /// Copies the editable fields onto the stored record (loaded fresh and
        /// tracked) rather than saving the incoming object wholesale, which may
        /// be a stale or detached grid row. Deactivating needs no balance check:
        /// suppliers carry no running account in this system, and past purchases
        /// keep pointing at them.
        /// </summary>
        public async Task UpdateAsync(Supplier supplier)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new BusinessValidationException("Supplier name is required.");
            var existing = await _unitOfWork.Suppliers.GetByIdAsync(supplier.Id)
                ?? throw new KeyNotFoundException($"Supplier {supplier.Id} not found.");

            existing.Name = supplier.Name;
            existing.ContactPerson = supplier.ContactPerson;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;
            existing.Address = supplier.Address;
            existing.IsActive = supplier.IsActive;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Supplier {SupplierId} updated (active: {IsActive})", existing.Id, existing.IsActive);
        }
    }
}
