using MediatR;
using Sincera.Policies.Application.Policies.Queries.GetPolicyById;
using Sincera.Policies.Domain.Customers;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;

public sealed record ListCustomerPoliciesQuery(
    CustomerId CustomerId,
    PolicyStatus? Status,
    int Page,
    int PageSize) : IRequest<ListCustomerPoliciesResponse>;

public sealed record ListCustomerPoliciesResponse(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<PolicyDetailsDto> Items);
