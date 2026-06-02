using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.CommomTestsUtilities.TestDoubles;
using Fcg.Identity.Infrastructure.SqlServer.Persistence;
using Fcg.Identity.IntegratedTests.Support;
using Fcg.Identity.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace Fcg.Identity.IntegratedTests.Configurations;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServerContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeIdentityProvider IdentityProvider { get; } = new();
    public FakeMessagePublisher MessagePublisher { get; } = new();

    public async Task InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FcgIdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlServerContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        IdentityProvider.Reset();
        MessagePublisher.Reset();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FcgIdentityDbContext>();

        dbContext.DonorProfiles.RemoveRange(dbContext.DonorProfiles);
        dbContext.ManagerProfiles.RemoveRange(dbContext.ManagerProfiles);
        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = _sqlServerContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIdentityProvider>();
            services.RemoveAll<IMessagePublisher>();

            services.AddSingleton<IIdentityProvider>(IdentityProvider);
            services.AddSingleton<IMessagePublisher>(MessagePublisher);
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        });
    }
}
