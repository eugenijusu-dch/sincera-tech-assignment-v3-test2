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
            throw new InvalidPolicyTransitionException(policy.Status, PolicyStatus.Active);

        var requiresInspection = request.ClaimedAmount > _claimsOptions.InspectionThreshold
            || (policy.EffectiveDate.HasValue
                && request.IncidentDate >= policy.EffectiveDate.Value
                && request.IncidentDate <= policy.EffectiveDate.Value.AddDays(_claimsOptions.FreshPolicyInspectionDays));

        var claim = new Claim(
            new ClaimId(Guid.NewGuid().ToString()),
            request.PolicyId,
            request.IncidentDate,
            request.ClaimedAmount,
            request.Description,
            requiresInspection,
            _clock);

        await _claims.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FileClaimResponse(
            claim.Id.Value,
            policy.Id.Value,
            claim.IncidentDate,
            claim.ClaimedAmount,
            claim.RequiresInspection);
    }
}
