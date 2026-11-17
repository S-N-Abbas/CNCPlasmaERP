using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Services
{
    public class SupplierLedgerService : ISupplierLedgerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserContext _currentUserContext;

        public SupplierLedgerService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
        }

        public async Task<IReadOnlyList<SupplierLedgerEntry>> GetStatementAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            var query = _unitOfWork.SupplierLedgerEntries.Query().Where(e => e.SupplierId == supplierId);
            if (fromDate.HasValue) query = query.Where(e => e.EntryDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(e => e.EntryDate < toDate.Value.Date.AddDays(1));
            return await query.OrderBy(e => e.EntryDate).ThenBy(e => e.Id).ToListAsync();
        }

        public async Task<IReadOnlyList<SupplierPayment>> GetRecentPaymentsAsync(int count = 50)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            return await _unitOfWork.SupplierPayments.Query().Include(p => p.Supplier)
                .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id).Take(count).ToListAsync();
        }

        public async Task RecordPaymentAsync(SupplierPayment payment)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSuppliers);
            if (payment.Amount <= 0) throw new BusinessValidationException("Payment amount must be greater than zero.");
            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(payment.SupplierId)
                ?? throw new KeyNotFoundException($"Supplier {payment.SupplierId} not found.");
            if (!supplier.IsActive) throw new BusinessValidationException("Payments cannot be recorded for an inactive supplier.");

            await _unitOfWork.SupplierPayments.AddAsync(payment);
            var balance = supplier.CurrentBalance - payment.Amount;
            supplier.CurrentBalance = balance;
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SupplierLedgerEntries.AddAsync(new SupplierLedgerEntry
            {
                SupplierId = supplier.Id,
                EntryDate = payment.PaymentDate,
                Type = SupplierTransactionType.Payment,
                Debit = payment.Amount,
                RunningBalance = balance,
                SupplierPayment = payment,
                Notes = BuildPaymentNotes(payment)
            });
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task PostOpeningBalanceAsync(Supplier supplier)
        {
            if (supplier.OpeningBalance == 0) return;
            supplier.CurrentBalance = supplier.OpeningBalance;
            await _unitOfWork.SupplierLedgerEntries.AddAsync(new SupplierLedgerEntry
            {
                SupplierId = supplier.Id,
                EntryDate = DateTime.Now,
                Type = SupplierTransactionType.OpeningBalance,
                Credit = supplier.OpeningBalance > 0 ? supplier.OpeningBalance : 0,
                Debit = supplier.OpeningBalance < 0 ? -supplier.OpeningBalance : 0,
                RunningBalance = supplier.OpeningBalance,
                Notes = "Opening balance"
            });
        }

        public async Task PostPurchaseAsync(StockPurchase purchase)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(purchase.SupplierId)
                ?? throw new KeyNotFoundException($"Supplier {purchase.SupplierId} not found.");
            var amount = purchase.MaterialCost + purchase.CarriageCost;
            var balance = supplier.CurrentBalance + amount;
            supplier.CurrentBalance = balance;
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SupplierLedgerEntries.AddAsync(new SupplierLedgerEntry
            {
                SupplierId = supplier.Id,
                EntryDate = purchase.PurchaseDate,
                Type = SupplierTransactionType.Purchase,
                Credit = amount,
                RunningBalance = balance,
                StockPurchase = purchase,
                Notes = string.IsNullOrWhiteSpace(purchase.SupplierInvoiceNumber)
                    ? $"Stock purchase: {purchase.WeightKg:N3} kg"
                    : $"Stock purchase invoice {purchase.SupplierInvoiceNumber}"
            });
        }

        private static string BuildPaymentNotes(SupplierPayment payment)
        {
            var parts = new List<string> { "Supplier payment" };
            if (!string.IsNullOrWhiteSpace(payment.ReferenceNumber)) parts.Add($"Ref: {payment.ReferenceNumber.Trim()}");
            if (!string.IsNullOrWhiteSpace(payment.Notes)) parts.Add(payment.Notes.Trim());
            return string.Join(" — ", parts);
        }
    }
}
