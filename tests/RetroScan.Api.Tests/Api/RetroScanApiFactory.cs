using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RetroScan.Api.LanguageModel;
using RetroScan.Api.Sources;
using Testcontainers.MySql;

namespace RetroScan.Api.Tests.Api;

public sealed class RetroScanApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public FakeLanguageModel LanguageModel { get; } = new();
    public FakeSourceSearch SourceSearch { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:RetroScan", connectionString);
        builder.UseSetting("RateLimit:PermitLimit", "1000");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILanguageModel>();
            services.AddSingleton<ILanguageModel>(LanguageModel);
            services.RemoveAll<ISourceSearch>();
            services.AddSingleton<ISourceSearch>(SourceSearch);
        });
    }
}

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:8.4").Build();

    public RetroScanApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _mySql.StartAsync();
        Factory = new RetroScanApiFactory(_mySql.GetConnectionString());
        Factory.StartServer();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _mySql.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "API against MySQL";
}
