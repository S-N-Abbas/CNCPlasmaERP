using System;
using System.IO;
using CNCPlasmaERP.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CNCPlasmaERP.Data
{
    /// <summary>
    /// Lets "Add-Migration" / "Update-Database" construct the DbContext at design
    /// time without running the full WPF app / DI container. EF Core Tools run
    /// with the Startup Project's build output as the current directory, so this
    /// picks up CNCPlasmaERP.UI/appsettings.json automatically as long as
    /// CNCPlasmaERP.UI is set as the Startup Project in Visual Studio.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var useTestProfile = string.Equals(
                Environment.GetEnvironmentVariable("CNCPLASMAERP_PROFILE"),
                "Test",
                StringComparison.OrdinalIgnoreCase);

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true);
            if (useTestProfile)
            {
                configurationBuilder.AddJsonFile("appsettings.Test.json", optional: false);
            }
            var configuration = configurationBuilder.Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=CNCPlasmaERP;Trusted_Connection=True;MultipleActiveResultSets=true";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
