using MediatR;
using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Application.Policies.Queries.GetPolicyById;

public sealed record GetPolicyByIdQuery(PolicyId PolicyId) : IRequest<PolicyDetailsDto?>;

public sealed record PolicyDetailsDto(
    string Id,
    string CustomerId,
    PolicyStatus Status,
    DateOnly? EffectiveDate,
    DateOnly? ExpiryDate,
    decimal? AnnualPremium,
    decimal? CancellationRefund,
    string? CancellationReason,
    int RenewalCount,
    IReadOnlyList<CoverageDto> Coverages);

public sealed record CoverageDto(CoverageType Type, decimal LimitAmount, decimal Deductible);
