using Riok.Mapperly.Abstractions;
using Sincera.Policies.Application.Policies.Queries.GetPolicyById;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Mapping;

[Mapper]
public partial class PolicyMapper
{
    [MapperIgnoreSource(nameof(Policy.CancelledAtUtc))]
    [MapperIgnoreSource(nameof(Policy.DomainEvents))]
    public partial PolicyDetailsDto ToDetailsDto(Policy policy);

    [MapperIgnoreSource(nameof(Coverage.Id))]
    public partial CoverageDto ToCoverageDto(Coverage coverage);

    private static string MapPolicyId(PolicyId id) => id.Value;

    private static string MapCustomerId(CustomerId id) => id.Value;
}
