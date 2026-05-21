namespace Sincera.Policies.Api.Contracts.Policies;

public sealed record CancelPolicyRequest(DateOnly EffectiveCancellationDate, string Reason);
