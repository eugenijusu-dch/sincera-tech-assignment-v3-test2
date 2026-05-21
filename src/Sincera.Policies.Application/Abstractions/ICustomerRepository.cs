using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);
}
