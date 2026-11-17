using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SupplierPayments");
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
            builder.Property(p => p.Notes).HasMaxLength(500);
            builder.HasOne(p => p.Supplier).WithMany(s => s.Payments).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.PaidByUser).WithMany().HasForeignKey(p => p.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
