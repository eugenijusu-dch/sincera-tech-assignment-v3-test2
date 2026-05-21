using MediatR;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Mapping;

namespace Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;

public sealed class ListCustomerPoliciesQueryHandler(IPolicyRepository policies, PolicyMapper mapper)
        : IRequestHandler<ListCustomerPoliciesQuery, ListCustomerPoliciesResponse>
{
    private readonly IPolicyRepository _policies = policies;
    private readonly PolicyMapper _mapper = mapper;

    public async Task<ListCustomerPoliciesResponse> Handle(
        ListCustomerPoliciesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _policies.GetByCustomerIdAsync(
            request.CustomerId, request.Status, request.Page, request.PageSize, cancellationToken);

        return new ListCustomerPoliciesResponse(
            total,
            items.Select(_mapper.ToDetailsDto).ToList());
    }
}
