namespace Sincera.Policies.Api.Contracts.Policies;

public sealed record ActivatePolicyResponseDto(
    string PolicyId,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    decimal AnnualPremium);
