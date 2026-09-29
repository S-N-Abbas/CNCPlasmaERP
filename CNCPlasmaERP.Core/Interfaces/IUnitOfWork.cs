using System;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Single point of access to all repositories, sharing one DbContext instance
    /// per unit of work so multi-step business operations (e.g. completing a job:
    /// deduct stock, accrue scrap, raise invoice, post a ledger entry) commit
    /// atomically via BeginTransactionAsync + SaveChangesAsync.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Simple aggregates — generic repository is enough, no custom queries needed.
        IGenericRepository<Material> Materials { get; }
        IGenericRepository<Supplier> Suppliers { get; }
        IGenericRepository<StockPurchase> StockPurchases { get; }
        IGenericRepository<Scrap> Scraps { get; }
        IGenericRepository<ScrapSale> ScrapSales { get; }
        IGenericRepository<Quotation> Quotations { get; }
        IGenericRepository<QuotationItem> QuotationItems { get; }
        IGenericRepository<JobMaterialUsage> JobMaterialUsages { get; }
        IGenericRepository<InvoiceItem> InvoiceItems { get; }
        IGenericRepository<Payment> Payments { get; }
        IGenericRepository<Expense> Expenses { get; }
        IGenericRepository<User> Users { get; }
        IGenericRepository<CustomerLedgerEntry> LedgerEntries { get; }

        // Aggregates with custom eager-loading queries.
        IJobRepository Jobs { get; }
        IInvoiceRepository Invoices { get; }
        ICustomerRepository Customers { get; }
        IAppSettingsRepository Settings { get; }

        Task<int> SaveChangesAsync();

        Task<IUnitOfWorkTransaction> BeginTransactionAsync();
    }
}
