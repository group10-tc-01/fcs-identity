using System.Reflection;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IMessagePublisher, NullMessagePublisher>();

        return services;
    }
}
