namespace Sincera.Policies.Domain.Claims;

public readonly record struct ClaimId(string Value)
{
    public override string ToString() => Value;

    public static ClaimId Parse(string value) => new(value);
}
