using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
    {
        public void Configure(EntityTypeBuilder<QuotationItem> builder)
        {
            builder.ToTable("QuotationItems");

            builder.Property(i => i.Description).IsRequired().HasMaxLength(250);
            builder.Property(i => i.EstimatedWeightKg).HasPrecision(18, 3);
            builder.Property(i => i.EstimatedRatePerKg).HasPrecision(18, 2);
            builder.Property(i => i.EstimatedAmount).HasPrecision(18, 2);

            builder.HasOne(i => i.Material)
                .WithMany()
                .HasForeignKey(i => i.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
