using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
        {
            builder.ToTable("CustomerLedgerEntries");

            builder.Property(e => e.Debit).HasPrecision(18, 2);
            builder.Property(e => e.Credit).HasPrecision(18, 2);
            builder.Property(e => e.RunningBalance).HasPrecision(18, 2);

            builder.HasOne(e => e.Customer)
                .WithMany(c => c.LedgerEntries)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Statements are always queried "for this customer, in date order".
            builder.HasIndex(e => new { e.CustomerId, e.EntryDate });
            builder.HasIndex(e => e.InvoiceId).IsUnique().HasFilter("[InvoiceId] IS NOT NULL");
            builder.HasIndex(e => e.PaymentId).IsUnique().HasFilter("[PaymentId] IS NOT NULL");
        }
    }
}
