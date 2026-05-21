using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Configurations;

public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PolicyId(value))
            .HasMaxLength(64);

        builder.Property(p => p.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(p => p.CustomerId);

        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.EffectiveDate);
        builder.Property(p => p.ExpiryDate);
        builder.Property(p => p.AnnualPremium).HasPrecision(18, 2);
        builder.Property(p => p.CancellationRefund).HasPrecision(18, 2);
        builder.Property(p => p.CancelledAtUtc);
        builder.Property(p => p.CancellationReason).HasMaxLength(500);
        builder.Property(p => p.RenewalCount);

        builder.HasMany<Coverage>("_coverages")
            .WithOne()
            .HasForeignKey("PolicyId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation("_coverages")!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.Coverages);
        builder.Ignore(p => p.DomainEvents);
    }
}
