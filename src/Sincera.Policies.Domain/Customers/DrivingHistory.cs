namespace Sincera.Policies.Domain.Customers;

public sealed record DrivingHistory(int YearsLicensed, int AtFaultIncidentsLast5Years)
{
    public static DrivingHistory None => new(0, 0);
}
