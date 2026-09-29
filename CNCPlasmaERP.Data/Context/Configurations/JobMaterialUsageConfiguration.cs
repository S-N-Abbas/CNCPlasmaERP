using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class JobMaterialUsageConfiguration : IEntityTypeConfiguration<JobMaterialUsage>
    {
        public void Configure(EntityTypeBuilder<JobMaterialUsage> builder)
        {
            builder.ToTable("JobMaterialUsages");

            builder.Property(u => u.WeightUsedKg).HasPrecision(18, 3);
            builder.Property(u => u.CostRatePerKg).HasPrecision(18, 2);
            builder.Property(u => u.SellingRatePerKg).HasPrecision(18, 2);
            builder.Property(u => u.ScrapGeneratedKg).HasPrecision(18, 3);

            builder.HasOne(u => u.Material)
                .WithMany(m => m.JobUsages)
                .HasForeignKey(u => u.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
