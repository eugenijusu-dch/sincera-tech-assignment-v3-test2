using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Exceptions;

namespace Sincera.Policies.Domain.Customers;

public sealed class Customer : Entity<CustomerId>
{
    private Customer() { }

    public Customer(CustomerId id, string fullName, DateOnly dateOfBirth, DrivingHistory drivingHistory, string residenceZip)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("customer.invalid_name", "Full name is required.");
        if (string.IsNullOrWhiteSpace(residenceZip))
            throw new DomainException("customer.invalid_zip", "Residence ZIP is required.");

        Id = id;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        DrivingHistory = drivingHistory;
        ResidenceZip = residenceZip;
    }

    public string FullName { get; private set; } = default!;
    public DateOnly DateOfBirth { get; private set; }
    public DrivingHistory DrivingHistory { get; private set; } = default!;
    public string ResidenceZip { get; private set; } = default!;

    public int AgeOn(DateOnly date)
    {
        var age = date.Year - DateOfBirth.Year;
        if (DateOfBirth > date.AddYears(-age))
            age--;
        return age;
    }
}
