using Microsoft.EntityFrameworkCore;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence;

public sealed class PoliciesDbContext : DbContext
{
    public PoliciesDbContext(DbContextOptions<PoliciesDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PoliciesDbContext).Assembly);
    }
}
