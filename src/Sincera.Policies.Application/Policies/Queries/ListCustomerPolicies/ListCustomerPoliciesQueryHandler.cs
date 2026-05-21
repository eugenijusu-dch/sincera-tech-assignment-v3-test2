using MediatR;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Mapping;
using Sincera.Policies.Application.Policies.Queries.GetPolicyById;

namespace Sincera.Policies.Application.Policies.Queries.ListCustomerPolicies;

public sealed class ListCustomerPoliciesQueryHandler(IPolicyRepository policies, PolicyMapper mapper)
        : IRequestHandler<ListCustomerPoliciesQuery, ListCustomerPoliciesResponse>
{
    private readonly IPolicyRepository _policies = policies;
    private readonly PolicyMapper _mapper = mapper;

    public async Task<ListCustomerPoliciesResponse> Handle(
        ListCustomerPoliciesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);

        var (items, total) = await _policies.ListByCustomerAsync(
            request.CustomerId, request.Status, page, pageSize, cancellationToken);

        var dtos = items.Select(_mapper.ToDetailsDto).ToList();
        return new ListCustomerPoliciesResponse(total, page, pageSize, dtos);
    }
}
