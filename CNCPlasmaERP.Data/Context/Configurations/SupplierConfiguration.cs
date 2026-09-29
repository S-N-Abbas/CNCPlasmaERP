using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");

            builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
            builder.Property(s => s.Phone).HasMaxLength(30);
            builder.Property(s => s.Email).HasMaxLength(150);

            builder.HasIndex(s => s.Name);
        }
    }
}
