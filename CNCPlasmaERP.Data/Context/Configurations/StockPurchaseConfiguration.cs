using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class StockPurchaseConfiguration : IEntityTypeConfiguration<StockPurchase>
    {
        public void Configure(EntityTypeBuilder<StockPurchase> builder)
        {
            builder.ToTable("StockPurchases");

            builder.Property(p => p.WeightKg).HasPrecision(18, 3);
            builder.Property(p => p.SheetLengthFt).HasPrecision(10, 2);
            builder.Property(p => p.SheetWidthFt).HasPrecision(10, 2);
            builder.Property(p => p.RatePerKg).HasPrecision(18, 2);
            builder.Property(p => p.CarriageCost).HasPrecision(18, 2);
            builder.Property(p => p.MaterialCost).HasPrecision(18, 2);
            builder.Property(p => p.SupplierInvoiceNumber).HasMaxLength(60);

            // Purchases are historical financial records — deleting a Material or
            // Supplier must never cascade-delete purchase history.
            builder.HasOne(p => p.Material)
                .WithMany(m => m.Purchases)
                .HasForeignKey(p => p.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Supplier)
                .WithMany(s => s.Purchases)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(p => p.PurchaseDate);
        }
    }
}
