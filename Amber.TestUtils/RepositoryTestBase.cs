using Amber.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Amber.TestUtils;

public class RepositoryTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected AmberContext AmberContext { get; private set; }

    public RepositoryTestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.CreateFunction("now", () => DateTime.UtcNow);
        _connection.Open();

        var options = new DbContextOptionsBuilder<AmberContext>().UseSqlite(_connection).Options;
        AmberContext = new AmberContext(options);
        AmberContext.Database.EnsureCreated();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
