using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Abstractions;

public interface IClaimRepository
{
    Task<IReadOnlyList<Claim>> GetByPolicyIdAsync(PolicyId policyId, CancellationToken cancellationToken);
}
