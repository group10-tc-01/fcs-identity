using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.CommomTestsUtilities.TestDoubles;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Infrastructure.SqlServer.Persistence;
using Fcg.Identity.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fcg.Identity.FunctionalTests.Configurations;

public sealed class FunctionalWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeIdentityProvider IdentityProvider { get; } = new();
    public InMemoryDonorProfileRepository DonorProfileRepository { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeMessagePublisher MessagePublisher { get; } = new();

    public void Reset()
    {
        IdentityProvider.Reset();
        DonorProfileRepository.Reset();
        UnitOfWork.Reset();
        MessagePublisher.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FcgIdentityDbContext>>();
            services.RemoveAll<FcgIdentityDbContext>();
            services.RemoveAll<IDonorProfileRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<IIdentityProvider>();
            services.RemoveAll<IMessagePublisher>();

            services.AddSingleton<IDonorProfileRepository>(DonorProfileRepository);
            services.AddSingleton<IUnitOfWork>(UnitOfWork);
            services.AddSingleton<IIdentityProvider>(IdentityProvider);
            services.AddSingleton<IMessagePublisher>(MessagePublisher);
        });
    }
}
