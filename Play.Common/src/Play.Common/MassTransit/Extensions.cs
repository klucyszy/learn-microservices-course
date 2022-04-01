using System.Reflection;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.MassTransit.Settings;

namespace Play.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration, string formatterPrefix)
    {
        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumers(Assembly.GetEntryAssembly());
            
            cfg.UsingRabbitMq((context, configurator) =>
            {
                var rabbitMqSettings = configuration
                    .GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
        
                configurator.Host(rabbitMqSettings.Host);
                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(formatterPrefix, false));
            });
        });
        services.AddMassTransitHostedService();

        return services;
    }
}