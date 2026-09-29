using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
            builder.Property(i => i.SubTotal).HasPrecision(18, 2);
            builder.Property(i => i.TaxPercentage).HasPrecision(5, 2);
            builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
            builder.Property(i => i.GrandTotal).HasPrecision(18, 2);
            builder.Property(i => i.PaidAmount).HasPrecision(18, 2);

            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

            builder.HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Line items belong entirely to the invoice.
            builder.HasMany(i => i.Items)
                .WithOne(it => it.Invoice)
                .HasForeignKey(it => it.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payments are their own financial record — never cascade-delete them.
            builder.HasMany(i => i.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: the Job <-> Invoice one-to-one relationship is configured
            // once, on the Job side (see JobConfiguration), to avoid EF Core
            // seeing two conflicting definitions of the same relationship.
        }
    }
}
