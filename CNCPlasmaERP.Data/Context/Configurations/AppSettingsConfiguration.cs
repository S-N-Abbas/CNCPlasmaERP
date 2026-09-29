using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
    {
        public void Configure(EntityTypeBuilder<AppSettings> builder)
        {
            builder.ToTable("AppSettings");

            builder.Property(s => s.CompanyName).IsRequired().HasMaxLength(150);
            builder.Property(s => s.TaxPercentage).HasPrecision(5, 2);
            builder.Property(s => s.CurrencySymbol).HasMaxLength(10);
            builder.Property(s => s.InvoicePrefix).HasMaxLength(10);
            builder.Property(s => s.QuotationPrefix).HasMaxLength(10);
            builder.Property(s => s.JobPrefix).HasMaxLength(10);
        }
    }
}
