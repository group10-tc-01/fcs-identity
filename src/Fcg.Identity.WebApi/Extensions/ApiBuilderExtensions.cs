using System.Diagnostics.CodeAnalysis;
using Fcg.Identity.Infrastructure.SqlServer.Persistence;
using Fcg.Identity.WebApi.Middlewares;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Identity.WebApi.Extensions;

[ExcludeFromCodeCoverage]
public static class ApiBuilderExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        using var dbContext = scope.ServiceProvider.GetRequiredService<FcgIdentityDbContext>();

        dbContext.Database.Migrate();

    }

    public static void UseCustomerExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    public static void UseGlobalCorrelationId(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalCorrelationIdMiddleware>();
    }
}
