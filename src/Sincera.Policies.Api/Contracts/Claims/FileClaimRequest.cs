namespace Sincera.Policies.Api.Contracts.Claims;

public sealed record FileClaimRequest(
    string PolicyId,
    DateOnly IncidentDate,
    decimal ClaimedAmount,
    string Description);
