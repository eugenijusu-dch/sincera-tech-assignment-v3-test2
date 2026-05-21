using MediatR;
using Microsoft.Extensions.Options;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Claims.Commands.FileClaim;

public sealed class FileClaimCommandHandler(
    IPolicyRepository policies,
    IClaimRepository claims,
    IUnitOfWork unitOfWork,
    IClock clock,
    IOptions<ClaimsOptions> claimsOptions) : IRequestHandler<FileClaimCommand, FileClaimResponse>
{
    private readonly IPolicyRepository _policies = policies;
    private readonly IClaimRepository _claims = claims;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IClock _clock = clock;
    private readonly ClaimsOptions _claimsOptions = claimsOptions.Value;

    public async Task<FileClaimResponse> Handle(FileClaimCommand request, CancellationToken cancellationToken)
    {
        var policy = await _policies.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new DomainException("policy.not_found", $"Policy {request.PolicyId} was not found.");

        if (policy.Status != PolicyStatus.Active)
            throw new InvalidPolicyTransitionException(policy.Status, policy.Status);

        var requiresInspection = RequiresInspection(policy, request);

        var claim = new Claim(
            id: new ClaimId($"CL-{Guid.NewGuid():N}"[..10].ToUpperInvariant()),
            policyId: request.PolicyId,
            incidentDate: request.IncidentDate,
            claimedAmount: request.ClaimedAmount,
            description: request.Description,
            requiresInspection: requiresInspection,
            clock: _clock);

        await _claims.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FileClaimResponse(
            claim.Id.Value, claim.PolicyId.Value, claim.IncidentDate,
            claim.ClaimedAmount, claim.RequiresInspection);
    }

    private bool RequiresInspection(Policy policy, FileClaimCommand request)
    {
        if (request.ClaimedAmount > _claimsOptions.InspectionThreshold)
            return true;

        if (policy.EffectiveDate is { } effective)
        {
            var daysSinceEffective = request.IncidentDate.DayNumber - effective.DayNumber;
            if (daysSinceEffective >= 0 && daysSinceEffective <= _claimsOptions.FreshPolicyInspectionDays)
                return true;
        }

        return false;
    }
}
