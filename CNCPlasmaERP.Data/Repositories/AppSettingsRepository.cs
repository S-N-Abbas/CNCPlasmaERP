using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Data.Repositories
{
    public class AppSettingsRepository : IAppSettingsRepository
    {
        private readonly ApplicationDbContext _context;

        public AppSettingsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            var settings = await _context.AppSettings.FirstOrDefaultAsync();

            if (settings is null)
            {
                settings = new AppSettings
                {
                    CompanyName = "My Company",
                    TaxEnabled = false,
                    TaxPercentage = 0,
                    CurrencySymbol = "Rs."
                };

                await _context.AppSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task SaveAsync(AppSettings settings)
        {
            _context.AppSettings.Update(settings);
            await _context.SaveChangesAsync();
        }
    }
}
