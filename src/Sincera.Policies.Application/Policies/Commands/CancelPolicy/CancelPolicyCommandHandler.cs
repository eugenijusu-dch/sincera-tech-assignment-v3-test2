using MediatR;
using Microsoft.Extensions.Options;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Commands.CancelPolicy;

public sealed class CancelPolicyCommandHandler : IRequestHandler<CancelPolicyCommand, CancelPolicyResponse>
{
    private readonly IPolicyRepository _policies;
    private readonly IClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CancellationPolicy _cancellationPolicy;
    private readonly CancellationOptions _cancellationOptions;

    public CancelPolicyCommandHandler(
        IPolicyRepository policies,
        IClaimRepository claims,
        IUnitOfWork unitOfWork,
        IClock clock,
        CancellationPolicy cancellationPolicy,
        IOptions<CancellationOptions> cancellationOptions)
    {
        _policies = policies;
        _claims = claims;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cancellationPolicy = cancellationPolicy;
        _cancellationOptions = cancellationOptions.Value;
    }

    public async Task<CancelPolicyResponse> Handle(CancelPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _policies.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new DomainException("policy.not_found", $"Policy {request.PolicyId} was not found.");

        if (policy.Status != PolicyStatus.Active)
            throw new InvalidPolicyTransitionException(policy.Status, PolicyStatus.Cancelled);

        var claims = await _claims.GetByPolicyIdAsync(request.PolicyId, cancellationToken);
        var eligibility = new RenewalCreditCalculator().ComputeRefundEligibility(policy, claims, _clock);
        var refund = _cancellationPolicy.ComputeProratedRefund(
            policy, request.EffectiveCancellationDate, _cancellationOptions, eligibility);

        policy.Cancel(request.EffectiveCancellationDate, request.Reason, refund, _clock);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelPolicyResponse(policy.Id.Value, request.EffectiveCancellationDate, refund);
    }
}
