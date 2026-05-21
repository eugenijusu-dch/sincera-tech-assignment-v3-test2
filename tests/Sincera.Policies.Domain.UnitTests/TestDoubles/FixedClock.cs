using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Domain.UnitTests.TestDoubles;

public sealed class FixedClock : IClock
{
    public FixedClock(DateOnly today)
    {
        Today = today;
        UtcNow = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; }
    public DateOnly Today { get; }
}
