using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.ReferenceNumber).HasMaxLength(60);

            builder.HasOne(p => p.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ReceivedByUser)
                .WithMany()
                .HasForeignKey(p => p.ReceivedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(p => p.PaymentDate);

            // Note: the Payment <-> Invoice relationship is configured once,
            // on the Invoice side (see InvoiceConfiguration).
        }
    }
}
