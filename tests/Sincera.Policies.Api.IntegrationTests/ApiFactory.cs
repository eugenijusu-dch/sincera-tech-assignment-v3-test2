using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Common;

namespace Sincera.Policies.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public Mock<IPolicyRepository> Policies { get; } = new(MockBehavior.Loose);
    public Mock<ICustomerRepository> Customers { get; } = new(MockBehavior.Loose);
    public Mock<IClaimRepository> Claims { get; } = new(MockBehavior.Loose);
    public Mock<IUnitOfWork> UnitOfWork { get; } = new(MockBehavior.Loose);
    public IClock Clock { get; } = new TestClock(new DateOnly(2026, 5, 20));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPolicyRepository>();
            services.AddScoped(_ => Policies.Object);

            services.RemoveAll<ICustomerRepository>();
            services.AddScoped(_ => Customers.Object);

            services.RemoveAll<IClaimRepository>();
            services.AddScoped(_ => Claims.Object);

            services.RemoveAll<IUnitOfWork>();
            services.AddScoped(_ => UnitOfWork.Object);

            services.RemoveAll<IClock>();
            services.AddSingleton(Clock);
        });
    }

    public void ResetMocks()
    {
        Policies.Reset();
        Customers.Reset();
        Claims.Reset();
        UnitOfWork.Reset();
    }

    private sealed class TestClock(DateOnly today) : IClock
    {
        public DateTime UtcNow { get; } = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        public DateOnly Today { get; } = today;
    }
}
