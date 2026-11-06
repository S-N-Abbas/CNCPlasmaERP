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
    public class MaterialService : IMaterialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MaterialService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public MaterialService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<MaterialService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserContext = currentUserContext;
        }

        public async Task<IReadOnlyList<Material>> GetAllAsync()
        {
            var materials = await _unitOfWork.Materials.GetAllAsync();
            return materials.OrderBy(m => m.Name).ThenBy(m => m.Gauge).ToList();
        }

        public async Task<IReadOnlyList<Material>> GetActiveAsync()
        {
            var materials = await _unitOfWork.Materials.FindAsync(m => m.IsActive);
            return materials.OrderBy(m => m.Name).ToList();
        }

        public async Task<Material> CreateAsync(Material material)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            if (material.DefaultRatePerKg < 0 || material.ReorderLevelKg < 0)
                throw new BusinessValidationException("Default rate and reorder level cannot be negative.");
            var duplicate = await _unitOfWork.Materials.FirstOrDefaultAsync(m =>
                m.Name == material.Name && m.Gauge == material.Gauge && m.Type == material.Type);

            if (duplicate is not null)
            {
                throw new DuplicateMaterialException(material.Name, material.Gauge);
            }

            await _unitOfWork.Materials.AddAsync(material);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Material '{Name}' ({Gauge}, {Type}) created", material.Name, material.Gauge, material.Type);

            return material;
        }

        /// <summary>
        /// Edits the descriptive fields only. The incoming object usually comes
        /// from a grid row loaded earlier, so its CurrentStockKg/CurrentStockCount
        /// may be out of date — saving it wholesale would silently overwrite the
        /// real stock figures. Stock quantities change only through StockService,
        /// so they are deliberately never copied here.
        /// </summary>
        public async Task UpdateAsync(Material material)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            if (material.DefaultRatePerKg < 0 || material.ReorderLevelKg < 0)
                throw new BusinessValidationException("Default rate and reorder level cannot be negative.");
            var existing = await _unitOfWork.Materials.GetByIdAsync(material.Id)
                ?? throw new KeyNotFoundException($"Material {material.Id} not found.");

            var duplicate = await _unitOfWork.Materials.FirstOrDefaultAsync(m =>
                m.Id != material.Id &&
                m.Name == material.Name && m.Gauge == material.Gauge && m.Type == material.Type);

            if (duplicate is not null)
            {
                throw new DuplicateMaterialException(material.Name, material.Gauge);
            }

            if (existing.IsActive && !material.IsActive && existing.CurrentStockKg > 0)
            {
                throw new CannotDeactivateException(
                    $"Cannot deactivate '{existing.Name}' while {existing.CurrentStockKg:N2} Kg is still in stock. " +
                    "Use the stock first, or correct the quantity.");
            }

            existing.Name = material.Name;
            existing.Type = material.Type;
            existing.Gauge = material.Gauge;
            existing.DefaultRatePerKg = material.DefaultRatePerKg;
            existing.ReorderLevelKg = material.ReorderLevelKg;
            existing.IsActive = material.IsActive;

            // existing is tracked (GetByIdAsync), so change detection picks the
            // edits up on SaveChanges — no explicit Update() call needed.
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Material {MaterialId} updated (active: {IsActive})", existing.Id, existing.IsActive);
        }
    }
}
