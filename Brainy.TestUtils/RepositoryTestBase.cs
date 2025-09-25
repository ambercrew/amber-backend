using Brainy.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Brainy.TestUtils;

public class RepositoryTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected BrainyContext BrainyContext { get; private set; }

    public RepositoryTestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.CreateFunction("now", () => DateTime.UtcNow);
        _connection.Open();

        var options = new DbContextOptionsBuilder<BrainyContext>().UseSqlite(_connection).Options;
        BrainyContext = new BrainyContext(options);
        BrainyContext.Database.EnsureCreated();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
