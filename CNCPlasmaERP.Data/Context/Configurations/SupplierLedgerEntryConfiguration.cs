using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class SupplierLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<SupplierLedgerEntry> builder)
        {
            builder.ToTable("SupplierLedgerEntries");
            builder.Property(e => e.Debit).HasPrecision(18, 2);
            builder.Property(e => e.Credit).HasPrecision(18, 2);
            builder.Property(e => e.RunningBalance).HasPrecision(18, 2);
            builder.HasOne(e => e.Supplier).WithMany(s => s.LedgerEntries).HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.StockPurchase).WithMany().HasForeignKey(e => e.StockPurchaseId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.SupplierPayment).WithMany().HasForeignKey(e => e.SupplierPaymentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(e => new { e.SupplierId, e.EntryDate });
            builder.HasIndex(e => e.StockPurchaseId).IsUnique().HasFilter("[StockPurchaseId] IS NOT NULL");
            builder.HasIndex(e => e.SupplierPaymentId).IsUnique().HasFilter("[SupplierPaymentId] IS NOT NULL");
        }
    }
}
