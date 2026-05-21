using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Abstractions;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(PolicyId id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Policy> Items, int Total)> ListByCustomerAsync(
        CustomerId customerId,
        PolicyStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
