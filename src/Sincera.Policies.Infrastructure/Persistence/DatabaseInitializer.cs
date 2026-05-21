using Microsoft.Extensions.Options;
using Sincera.Policies.Application.Abstractions;
using Sincera.Policies.Domain.Common;
using Sincera.Policies.Domain.Policies;
using Sincera.Policies.Infrastructure.Persistence.Seeding;

namespace Sincera.Policies.Infrastructure.Persistence;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly PoliciesDbContext _db;
    private readonly IClock _clock;
    private readonly IOptions<PremiumOptions> _premiumOptions;

    public DatabaseInitializer(PoliciesDbContext db, IClock clock, IOptions<PremiumOptions> premiumOptions)
    {
        _db = db;
        _clock = clock;
        _premiumOptions = premiumOptions;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);
        DatabaseSeeder.Seed(_db, _clock, _premiumOptions);
    }
}
