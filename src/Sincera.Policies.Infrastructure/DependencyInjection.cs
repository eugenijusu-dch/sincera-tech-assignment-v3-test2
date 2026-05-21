using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Claims;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Policies;
using Sincera.Policies.Infrastructure.Clock;
using Sincera.Policies.Infrastructure.Persistence;
using Sincera.Policies.Infrastructure.Persistence.Repositories;

namespace Sincera.Policies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PremiumOptions>(configuration.GetSection("Premium"));
        services.Configure<CancellationOptions>(configuration.GetSection("Cancellation"));
        services.Configure<ClaimsOptions>(configuration.GetSection("Claims"));

        services.AddSingleton<SqliteInMemoryConnectionFactory>();
        services.AddDbContext<PoliciesDbContext>((sp, opts) =>
        {
            var factory = sp.GetRequiredService<SqliteInMemoryConnectionFactory>();
            opts.UseSqlite(factory.Connection);
        });

        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
