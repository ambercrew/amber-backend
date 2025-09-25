using Brainy.Application.Services;
using Brainy.Application.Users.Services;
using Brainy.Infrastructure.Database;
using Brainy.WebApi.IntegrationTests.Services;
using LiteBus.Messaging.Registry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brainy.WebApi.IntegrationTests;

internal class BrainyWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        MessageRegistryAccessor.Instance.Clear();

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.CreateFunction("now", () => DateTime.Now);
        _connection.Open();

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<BrainyContext>>();
            services.RemoveAll<IRandomGenerator>();
            services.RemoveAll<IEmailService>();

            services.AddSingleton<IEmailService>(Substitute.For<IEmailService>());
            services.AddSingleton<IRandomGenerator, TestRandomGenerator>();
            services.AddDbContextPool<BrainyContext>(options => options.UseSqlite(_connection));
        });
    }

    public new void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
        MessageRegistryAccessor.Instance.Clear();
    }
}
