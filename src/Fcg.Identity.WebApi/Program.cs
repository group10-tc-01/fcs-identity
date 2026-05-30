using Fcg.Identity.Application.DependencyInjection;
using Fcg.Identity.Infrastructure.Kafka.DependencyInjection;
using Fcg.Identity.Infrastructure.Keycloak.DependencyInjection;
using Fcg.Identity.Infrastructure.SqlServer.DependencyInjection;
using Fcg.Identity.WebApi.DependencyInjection;

namespace Fcg.Identity.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddWebApi(builder.Configuration);
        builder.Services.AddApplication();
        builder.Services.AddSqlServerInfrastructure(builder.Configuration);
        builder.Services.AddKafkaInfrastructure(builder.Configuration);
        builder.Services.AddKeycloakInfrastructure(builder.Configuration);

        var app = builder.Build();
        app.UseWebApiPipeline();
        app.Run();
    }
}
