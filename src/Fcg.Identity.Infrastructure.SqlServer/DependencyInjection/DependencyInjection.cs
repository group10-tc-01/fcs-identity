using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Infrastructure.SqlServer.Persistence;
using Fcg.Identity.Infrastructure.SqlServer.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.Infrastructure.SqlServer.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FcgIdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        services.AddScoped<IDonorProfileRepository, DonorProfileRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FcgIdentityDbContext>());
        services.AddHealthChecks().AddDbContextCheck<FcgIdentityDbContext>("sqlserver");

        return services;
    }
}
