using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class MaterialConfiguration : IEntityTypeConfiguration<Material>
    {
        public void Configure(EntityTypeBuilder<Material> builder)
        {
            builder.ToTable("Materials");

            builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
            builder.Property(m => m.Gauge).IsRequired().HasMaxLength(30);

            builder.Property(m => m.CurrentStockKg).HasPrecision(18, 3);
            builder.Property(m => m.ReorderLevelKg).HasPrecision(18, 3);
            builder.Property(m => m.StandardLengthFt).HasPrecision(10, 2);
            builder.Property(m => m.StandardWidthFt).HasPrecision(10, 2);
            builder.Property(m => m.DefaultRatePerKg).HasPrecision(18, 2);

            // Prevents accidentally creating the same material spec twice.
            builder.HasIndex(m => new { m.Name, m.Gauge, m.Type }).IsUnique();
        }
    }
}
