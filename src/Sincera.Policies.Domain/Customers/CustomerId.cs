namespace Sincera.Policies.Domain.Customers;

public readonly record struct CustomerId(string Value)
{
    public override string ToString() => Value;

    public static CustomerId Parse(string value) => new(value);
}
