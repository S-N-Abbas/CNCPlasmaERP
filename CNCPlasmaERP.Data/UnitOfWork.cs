using System.Threading.Tasks;
using System.Data;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Data.Context;
using CNCPlasmaERP.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IGenericRepository<Material>? _materials;
        private IGenericRepository<Supplier>? _suppliers;
        private IGenericRepository<StockPurchase>? _stockPurchases;
        private IGenericRepository<Scrap>? _scraps;
        private IGenericRepository<ScrapSale>? _scrapSales;
        private IGenericRepository<Quotation>? _quotations;
        private IGenericRepository<QuotationItem>? _quotationItems;
        private IGenericRepository<JobMaterialUsage>? _jobMaterialUsages;
        private IGenericRepository<InvoiceItem>? _invoiceItems;
        private IGenericRepository<Payment>? _payments;
        private IGenericRepository<Expense>? _expenses;
        private IGenericRepository<User>? _users;
        private IGenericRepository<CustomerLedgerEntry>? _ledgerEntries;

        private IJobRepository? _jobs;
        private IInvoiceRepository? _invoices;
        private ICustomerRepository? _customers;
        private IAppSettingsRepository? _settings;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<Material> Materials => _materials ??= new GenericRepository<Material>(_context);
        public IGenericRepository<Supplier> Suppliers => _suppliers ??= new GenericRepository<Supplier>(_context);
        public IGenericRepository<StockPurchase> StockPurchases => _stockPurchases ??= new GenericRepository<StockPurchase>(_context);
        public IGenericRepository<Scrap> Scraps => _scraps ??= new GenericRepository<Scrap>(_context);
        public IGenericRepository<ScrapSale> ScrapSales => _scrapSales ??= new GenericRepository<ScrapSale>(_context);
        public IGenericRepository<Quotation> Quotations => _quotations ??= new GenericRepository<Quotation>(_context);
        public IGenericRepository<QuotationItem> QuotationItems => _quotationItems ??= new GenericRepository<QuotationItem>(_context);
        public IGenericRepository<JobMaterialUsage> JobMaterialUsages => _jobMaterialUsages ??= new GenericRepository<JobMaterialUsage>(_context);
        public IGenericRepository<InvoiceItem> InvoiceItems => _invoiceItems ??= new GenericRepository<InvoiceItem>(_context);
        public IGenericRepository<Payment> Payments => _payments ??= new GenericRepository<Payment>(_context);
        public IGenericRepository<Expense> Expenses => _expenses ??= new GenericRepository<Expense>(_context);
        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<CustomerLedgerEntry> LedgerEntries => _ledgerEntries ??= new GenericRepository<CustomerLedgerEntry>(_context);

        public IJobRepository Jobs => _jobs ??= new JobRepository(_context);
        public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
        public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
        public IAppSettingsRepository Settings => _settings ??= new AppSettingsRepository(_context);

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync() => BeginTransactionAsync(IsolationLevel.ReadCommitted);

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            if (_context.Database.CurrentTransaction is not null)
                return new EfUnitOfWorkTransaction(_context.Database.CurrentTransaction, ownsTransaction: false);

            var transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
            return new EfUnitOfWorkTransaction(transaction);
        }

        public void DetachAllTrackedEntities() => _context.ChangeTracker.Clear();

        public void Dispose() => _context.Dispose();
    }
}
