using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface ISupplierLedgerService
    {
        Task<IReadOnlyList<SupplierLedgerEntry>> GetStatementAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IReadOnlyList<SupplierPayment>> GetRecentPaymentsAsync(int count = 50);
        Task RecordPaymentAsync(SupplierPayment payment);
        Task PostOpeningBalanceAsync(Supplier supplier);
        Task PostPurchaseAsync(StockPurchase purchase);
    }
}
