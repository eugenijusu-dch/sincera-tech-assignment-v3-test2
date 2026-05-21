using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Exceptions;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Domain.Claims;

public sealed class Claim : Entity<ClaimId>
{
    private Claim() { }

    public Claim(
        ClaimId id,
        PolicyId policyId,
        DateOnly incidentDate,
        decimal claimedAmount,
        string description,
        bool requiresInspection,
        IClock clock)
    {
        if (claimedAmount <= 0)
            throw new DomainException("claim.invalid_amount", "Claim amount must be positive.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("claim.invalid_description", "Description is required.");
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (incidentDate > clock.Today)
            throw new DomainException("claim.future_incident", "Incident date cannot be in the future.");

        Id = id;
        PolicyId = policyId;
        IncidentDate = incidentDate;
        ClaimedAmount = claimedAmount;
        Description = description;
        RequiresInspection = requiresInspection;
        Status = ClaimStatus.Submitted;
        FiledAtUtc = clock.UtcNow;
    }

    public PolicyId PolicyId { get; private set; }
    public DateOnly IncidentDate { get; private set; }
    public decimal ClaimedAmount { get; private set; }
    public string Description { get; private set; } = default!;
    public bool RequiresInspection { get; private set; }
    public ClaimStatus Status { get; private set; }
    public DateTime FiledAtUtc { get; private set; }
}
