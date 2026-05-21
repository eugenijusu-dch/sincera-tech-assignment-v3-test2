using Microsoft.Data.Sqlite;

namespace Sincera.Policies.Infrastructure.Persistence;

// SQLite's in-memory database is destroyed when the LAST connection to it closes. We register
// this connection as a singleton, opened once at startup, so the in-memory database survives for
// the full app lifetime. Do NOT dispose this manually — the DI container handles it on shutdown.
public sealed class SqliteInMemoryConnectionFactory : IDisposable
{
    public SqliteConnection Connection { get; }

    public SqliteInMemoryConnectionFactory()
    {
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();
    }

    public void Dispose() => Connection.Dispose();
}
