namespace Sincera.Policies.Domain.Common;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
