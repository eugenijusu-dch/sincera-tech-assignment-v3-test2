using Microsoft.EntityFrameworkCore;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository(PoliciesDbContext db) : IPolicyRepository
{
    private readonly PoliciesDbContext _db = db;

    public Task<Policy?> GetByIdAsync(PolicyId id, CancellationToken cancellationToken)
    {
        return _db.Policies
            .Include("_coverages")
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Policy> Items, int Total)> ListByCustomerAsync(
        CustomerId customerId,
        PolicyStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Policies.Include("_coverages").Where(p => p.CustomerId == customerId);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Status == PolicyStatus.Active)
            .ThenByDescending(p => p.ExpiryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
