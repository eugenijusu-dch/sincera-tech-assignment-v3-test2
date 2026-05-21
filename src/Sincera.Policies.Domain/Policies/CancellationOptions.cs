namespace Sincera.Policies.Domain.Policies;

public sealed class CancellationOptions
{
    public int MinNoticeDays { get; set; } = 0;
    public decimal AdminFee { get; set; } = 35m;
    public int GracePeriodHours { get; set; } = 24;
    public int MaxFutureCancellationDays { get; set; } = 30;
}
