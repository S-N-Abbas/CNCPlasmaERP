using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class InvoiceAdjustmentConfiguration : IEntityTypeConfiguration<InvoiceAdjustment>
    {
        public void Configure(EntityTypeBuilder<InvoiceAdjustment> builder)
        {
            builder.ToTable("InvoiceAdjustments");
            builder.Property(a => a.PreviousOtherCharges).HasPrecision(18, 2);
            builder.Property(a => a.NewOtherCharges).HasPrecision(18, 2);
            builder.Property(a => a.PreviousDiscountAmount).HasPrecision(18, 2);
            builder.Property(a => a.NewDiscountAmount).HasPrecision(18, 2);
            builder.Property(a => a.PreviousTaxAmount).HasPrecision(18, 2);
            builder.Property(a => a.NewTaxAmount).HasPrecision(18, 2);
            builder.Property(a => a.PreviousGrandTotal).HasPrecision(18, 2);
            builder.Property(a => a.NewGrandTotal).HasPrecision(18, 2);
            builder.Property(a => a.Reason).IsRequired().HasMaxLength(500);
            builder.HasOne(a => a.Invoice).WithMany(i => i.Adjustments).HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.AdjustedByUser).WithMany().HasForeignKey(a => a.AdjustedByUserId).OnDelete(DeleteBehavior.SetNull);
            builder.HasIndex(a => new { a.InvoiceId, a.AdjustmentDate });
        }
    }
}
