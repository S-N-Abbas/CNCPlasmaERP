using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("Jobs");

            builder.Property(j => j.JobNumber).IsRequired().HasMaxLength(30);
            builder.Property(j => j.ServiceCharges).HasPrecision(18, 2);
            builder.Property(j => j.CarriageCharges).HasPrecision(18, 2);

            builder.HasIndex(j => j.JobNumber).IsUnique();

            builder.HasOne(j => j.Customer)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(j => j.Quotation)
                .WithMany()
                .HasForeignKey(j => j.QuotationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(j => j.CreatedByUser)
                .WithMany()
                .HasForeignKey(j => j.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Material usage lines belong entirely to the job — deleting a job
            // (soft-delete, per BaseEntity) is fine to cascade to its own usage lines.
            builder.HasMany(j => j.MaterialUsages)
                .WithOne(u => u.Job)
                .HasForeignKey(u => u.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // One job -> exactly one invoice. Never cascade-delete an invoice
            // (financial record) just because the job record changes.
            builder.HasOne(j => j.Invoice)
                .WithOne(i => i.Job)
                .HasForeignKey<Invoice>(i => i.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
