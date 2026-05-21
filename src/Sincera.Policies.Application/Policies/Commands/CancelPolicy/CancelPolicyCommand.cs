using MediatR;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Commands.CancelPolicy;

public sealed record CancelPolicyCommand(PolicyId PolicyId, DateOnly EffectiveCancellationDate, string Reason)
    : IRequest<CancelPolicyResponse>;

public sealed record CancelPolicyResponse(string PolicyId, DateOnly EffectiveCancellationDate, decimal Refund);
