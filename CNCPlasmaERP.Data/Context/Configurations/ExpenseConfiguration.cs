using CNCPlasmaERP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CNCPlasmaERP.Data.Context.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");

            builder.Property(e => e.Amount).HasPrecision(18, 2);
            builder.Property(e => e.PaidTo).HasMaxLength(150);
            builder.Property(e => e.ReferenceNumber).HasMaxLength(60);

            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // The Wages screen is just this table filtered by Category == Wages,
            // so this composite index also speeds up that filtered view.
            builder.HasIndex(e => new { e.Category, e.ExpenseDate });
        }
    }
}
