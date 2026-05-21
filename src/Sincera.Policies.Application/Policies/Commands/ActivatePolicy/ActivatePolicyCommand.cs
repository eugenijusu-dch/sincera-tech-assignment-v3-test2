using MediatR;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Commands.ActivatePolicy;

public sealed record ActivatePolicyCommand(PolicyId PolicyId) : IRequest<ActivatePolicyResponse>;

public sealed record ActivatePolicyResponse(string PolicyId, DateOnly EffectiveDate, DateOnly ExpiryDate, decimal AnnualPremium);
