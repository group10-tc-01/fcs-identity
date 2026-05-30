using Fcg.Identity.CommomTestsUtilities.TestDoubles;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fcg.Identity.IntegratedTests.Configurations;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeUnitOfWork UnitOfWork { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUnitOfWork>();

            services.AddSingleton<IUnitOfWork>(UnitOfWork);
        });
    }
}
