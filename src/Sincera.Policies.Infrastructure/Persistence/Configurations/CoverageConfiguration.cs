using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Configurations;

public sealed class CoverageConfiguration : IEntityTypeConfiguration<Coverage>
{
    public void Configure(EntityTypeBuilder<Coverage> builder)
    {
        builder.ToTable("Coverages");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.LimitAmount).HasPrecision(18, 2);
        builder.Property(c => c.Deductible).HasPrecision(18, 2);

        builder.Property<PolicyId>("PolicyId")
            .HasConversion(id => id.Value, value => new PolicyId(value))
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex("PolicyId");
    }
}
