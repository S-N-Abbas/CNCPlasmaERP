using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class ScrapConfiguration : IEntityTypeConfiguration<Scrap>
    {
        public void Configure(EntityTypeBuilder<Scrap> builder)
        {
            builder.ToTable("Scraps");

            builder.Property(s => s.BalanceKg).HasPrecision(18, 3);

            // Exactly one running scrap balance per material.
            builder.HasOne(s => s.Material)
                .WithOne(m => m.ScrapBalance)
                .HasForeignKey<Scrap>(s => s.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.MaterialId).IsUnique();
        }
    }
}
