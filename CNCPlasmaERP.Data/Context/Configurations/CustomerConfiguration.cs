using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Phone).HasMaxLength(30);
            builder.Property(c => c.Email).HasMaxLength(150);
            builder.Property(c => c.OpeningBalance).HasPrecision(18, 2);
            builder.Property(c => c.CurrentBalance).HasPrecision(18, 2);
            builder.Property(c => c.CreditLimit).HasPrecision(18, 2);

            builder.HasIndex(c => c.Name);
        }
    }
}
