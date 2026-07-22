using LykyConnector.Core.Queue;
using LykyConnector.Core.Sign;
using Microsoft.Extensions.DependencyInjection;

namespace LykyConnector.Core.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLykyCore(this IServiceCollection services, string? dataPath = null)
    {
        services.AddSingleton<ISignService, SignService>();
        services.AddSingleton<IConfigStore>(sp => new ConfigStore(dataPath ?? string.Empty));

        services.AddSingleton<IMessageQueue>(sp =>
        {
            var basePath = dataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
            Directory.CreateDirectory(basePath);
            return new MessageQueue(Path.Combine(basePath, "lyky.db"));
        });

        services.AddHostedService<QueueConsumer>();

        return services;
    }
}
