using Microsoft.EntityFrameworkCore;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Customers;

namespace Sincera.Policies.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly PoliciesDbContext _db;

    public CustomerRepository(PoliciesDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken)
    {
        return _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
