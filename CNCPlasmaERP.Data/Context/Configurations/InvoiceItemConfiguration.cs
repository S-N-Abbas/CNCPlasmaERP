using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.ToTable("InvoiceItems");

            builder.Property(i => i.Description).IsRequired().HasMaxLength(250);
            builder.Property(i => i.QuantityKg).HasPrecision(18, 3);
            builder.Property(i => i.RatePerKg).HasPrecision(18, 2);
            builder.Property(i => i.Amount).HasPrecision(18, 2);
        }
    }
}
