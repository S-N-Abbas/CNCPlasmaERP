using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Data.Context
{
    /// <summary>
    /// Primary EF Core context for the whole system. Two cross-cutting behaviours
    /// are enforced centrally here rather than per-entity:
    ///   1. Soft delete — every BaseEntity-derived table is automatically filtered
    ///      to IsDeleted == false, and calling .Remove() on any tracked entity is
    ///      converted into a soft delete in SaveChanges. Financial/stock history
    ///      must never be physically lost.
    ///   2. Audit stamping — CreatedAt / UpdatedAt are set automatically; callers
    ///      never need to set them by hand.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<StockPurchase> StockPurchases => Set<StockPurchase>();
        public DbSet<Scrap> Scraps => Set<Scrap>();
        public DbSet<ScrapSale> ScrapSales => Set<ScrapSale>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
        public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobMaterialUsage> JobMaterialUsages => Set<JobMaterialUsage>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Picks up every IEntityTypeConfiguration<T> class in this assembly (see /Context/Configurations).
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            ApplyDefaultDecimalPrecision(modelBuilder);
            ApplySoftDeleteQueryFilters(modelBuilder);
        }

        /// <summary>
        /// Fallback precision(18,2) for any decimal column a configuration class
        /// didn't explicitly set (e.g. weight columns use (18,3) and are set
        /// explicitly in their own IEntityTypeConfiguration).
        /// </summary>
        private static void ApplyDefaultDecimalPrecision(ModelBuilder modelBuilder)
        {
            var decimalProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimalProperties)
            {
                if (property.GetPrecision() is null)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }

        private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    continue;
                }

                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override int SaveChanges()
        {
            ApplyAuditRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Stamps CreatedAt/UpdatedAt automatically and converts any hard delete
        /// (.Remove()) into a soft delete, so no financial or stock record is ever
        /// physically removed from the database.
        /// </summary>
        private void ApplyAuditRules()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.Now;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;
                }
            }
        }
    }
}
