using Sincera.Policies.Domain.Policies;

namespace Sincera.Policies.Domain.Exceptions;

public sealed class InvalidPolicyTransitionException : DomainException
{
    public PolicyStatus FromStatus { get; }
    public PolicyStatus ToStatus { get; }

    public InvalidPolicyTransitionException(PolicyStatus from, PolicyStatus to)
        : base("policy.invalid_transition", $"Cannot transition policy from {from} to {to}.")
    {
        FromStatus = from;
        ToStatus = to;
    }
}
