using MediatR;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Claims.Commands.FileClaim;

public sealed record FileClaimCommand(PolicyId PolicyId, DateOnly IncidentDate, decimal ClaimedAmount, string Description)
    : IRequest<FileClaimResponse>;

public sealed record FileClaimResponse(string ClaimId, string PolicyId, DateOnly IncidentDate, decimal ClaimedAmount, bool RequiresInspection);
