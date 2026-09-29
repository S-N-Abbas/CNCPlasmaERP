using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            builder.ToTable("Quotations");

            builder.Property(q => q.QuotationNumber).IsRequired().HasMaxLength(30);
            builder.Property(q => q.EstimatedServiceCharges).HasPrecision(18, 2);
            builder.Property(q => q.EstimatedCarriageCharges).HasPrecision(18, 2);

            builder.HasIndex(q => q.QuotationNumber).IsUnique();

            builder.HasOne(q => q.Customer)
                .WithMany(c => c.Quotations)
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Informational link only — converting a quotation must never delete it,
            // and deleting the resulting Job must not delete the quotation either.
            builder.HasOne(q => q.ConvertedToJob)
                .WithOne()
                .HasForeignKey<Quotation>(q => q.ConvertedToJobId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(q => q.Items)
                .WithOne(i => i.Quotation)
                .HasForeignKey(i => i.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
