using Microsoft.EntityFrameworkCore;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository : IPolicyRepository
{
    private readonly PoliciesDbContext _db;

    public PolicyRepository(PoliciesDbContext db)
    {
        _db = db;
    }

    public Task<Policy?> GetByIdAsync(PolicyId id, CancellationToken cancellationToken)
    {
        return _db.Policies
            .Include("_coverages")
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
