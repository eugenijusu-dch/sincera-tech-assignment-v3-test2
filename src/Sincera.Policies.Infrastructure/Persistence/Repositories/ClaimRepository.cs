using Microsoft.EntityFrameworkCore;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Infrastructure.Persistence.Repositories;

public sealed class ClaimRepository(PoliciesDbContext db) : IClaimRepository
{
    private readonly PoliciesDbContext _db = db;

    public async Task<IReadOnlyList<Claim>> GetByPolicyIdAsync(PolicyId policyId, CancellationToken cancellationToken)
    {
        return await _db.Claims
            .Where(c => c.PolicyId == policyId)
            .ToListAsync(cancellationToken);
    }
}
