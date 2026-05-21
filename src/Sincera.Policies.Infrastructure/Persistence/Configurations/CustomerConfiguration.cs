using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasMaxLength(64);

        builder.Property(c => c.FullName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DateOfBirth);
        builder.Property(c => c.ResidenceZip).IsRequired().HasMaxLength(20);

        builder.OwnsOne(c => c.DrivingHistory, dh =>
        {
            dh.Property(d => d.YearsLicensed).HasColumnName("DrivingHistory_YearsLicensed");
            dh.Property(d => d.AtFaultIncidentsLast5Years).HasColumnName("DrivingHistory_AtFaultIncidentsLast5Years");
        });

        builder.Ignore(c => c.DomainEvents);
    }
}
