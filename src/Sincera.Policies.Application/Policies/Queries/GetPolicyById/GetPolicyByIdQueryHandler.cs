using MediatR;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Application.Policies.Mapping;

namespace Sincera.Policies.Application.Policies.Queries.GetPolicyById;

public sealed class GetPolicyByIdQueryHandler : IRequestHandler<GetPolicyByIdQuery, PolicyDetailsDto?>
{
    private readonly IPolicyRepository _policies;
    private readonly PolicyMapper _mapper;

    public GetPolicyByIdQueryHandler(IPolicyRepository policies, PolicyMapper mapper)
    {
        _policies = policies;
        _mapper = mapper;
    }

    public async Task<PolicyDetailsDto?> Handle(GetPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var policy = await _policies.GetByIdAsync(request.PolicyId, cancellationToken);
        return policy is null ? null : _mapper.ToDetailsDto(policy);
    }
}
