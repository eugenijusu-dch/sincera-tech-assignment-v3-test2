using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new ClaimId(value))
            .HasMaxLength(64);

        builder.Property(c => c.PolicyId)
            .HasConversion(id => id.Value, value => new PolicyId(value))
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(c => c.PolicyId);

        builder.Property(c => c.IncidentDate);
        builder.Property(c => c.ClaimedAmount).HasPrecision(18, 2);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.RequiresInspection);
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.FiledAtUtc);

        builder.Ignore(c => c.DomainEvents);
    }
}
