using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Abstractions;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(PolicyId id, CancellationToken cancellationToken);
}
