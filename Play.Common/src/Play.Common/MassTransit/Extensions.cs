using System.Reflection;
using GreenPipes;
using GreenPipes.Configurators;
using MassTransit;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.MassTransit.Settings;

namespace Play.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services,
        IConfiguration configuration, string formatterPrefix,
        Action<IRetryConfigurator> retryConfigurator = null)
    {
        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumers(Assembly.GetEntryAssembly());

            cfg.UsingPlayEconomyRabbitMq(configuration, formatterPrefix, retryConfigurator);
        });
        services.AddMassTransitHostedService();

        return services;
    }

    public static void UsingPlayEconomyRabbitMq(this IServiceCollectionBusConfigurator cfg,
        IConfiguration configuration, string formatterPrefix,
        Action<IRetryConfigurator> configureRetries = null)
    {
        cfg.UsingRabbitMq((context, configurator) =>
        {
            var rabbitMqSettings = configuration
                .GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();

            configurator.Host(rabbitMqSettings.Host);
            configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(formatterPrefix, false));
            configureRetries ??= retryCfg
                => retryCfg.Interval(3, TimeSpan.FromSeconds(5));
            
            configurator.UseMessageRetry(configureRetries);

        });
    }
}