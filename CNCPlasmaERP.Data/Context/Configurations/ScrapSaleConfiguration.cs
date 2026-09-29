using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class ScrapSaleConfiguration : IEntityTypeConfiguration<ScrapSale>
    {
        public void Configure(EntityTypeBuilder<ScrapSale> builder)
        {
            builder.ToTable("ScrapSales");

            builder.Property(s => s.WeightKgSold).HasPrecision(18, 3);
            builder.Property(s => s.RatePerKg).HasPrecision(18, 2);
            builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
            builder.Property(s => s.BuyerName).IsRequired().HasMaxLength(150);

            builder.HasOne(s => s.Material)
                .WithMany()
                .HasForeignKey(s => s.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.SaleDate);
        }
    }
}
