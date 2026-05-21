using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Application.UnitTests.TestDoubles;

public sealed class FakeClock : IClock
{
    public FakeClock(DateOnly today)
    {
        Today = today;
        UtcNow = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; }
    public DateOnly Today { get; }
}
