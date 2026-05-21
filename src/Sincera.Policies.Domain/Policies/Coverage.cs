using Sincera.Policies.Domain.Exceptions;

namespace Sincera.Policies.Domain.Policies;

public sealed class Coverage
{
    private Coverage() { }

    public Coverage(CoverageType type, decimal limitAmount, decimal deductible)
    {
        if (type == CoverageType.Unspecified)
            throw new DomainException("coverage.invalid_type", "Coverage type must be specified.");
        if (limitAmount <= 0)
            throw new DomainException("coverage.invalid_limit", "Coverage limit must be positive.");
        if (deductible < 0)
            throw new DomainException("coverage.invalid_deductible", "Deductible cannot be negative.");

        Type = type;
        LimitAmount = limitAmount;
        Deductible = deductible;
    }

    public int Id { get; private set; }
    public CoverageType Type { get; private set; }
    public decimal LimitAmount { get; private set; }
    public decimal Deductible { get; private set; }
}
