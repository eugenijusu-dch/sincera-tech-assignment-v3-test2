namespace Sincera.Policies.Domain.Policies;

public readonly record struct PolicyId(string Value)
{
    public override string ToString() => Value;

    public static PolicyId Parse(string value) => new(value);
}
