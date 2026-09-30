using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MaterialService> _logger;

        public MaterialService(IUnitOfWork unitOfWork, ILogger<MaterialService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Material>> GetAllAsync() => await _unitOfWork.Materials.GetAllAsync();

        public async Task<IReadOnlyList<Material>> GetActiveAsync()
        {
            var materials = await _unitOfWork.Materials.FindAsync(m => m.IsActive);
            return materials.OrderBy(m => m.Name).ToList();
        }

        public async Task<Material> CreateAsync(Material material)
        {
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

        public async Task UpdateAsync(Material material)
        {
            _unitOfWork.Materials.Update(material);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Material {MaterialId} updated", material.Id);
        }
    }
}
