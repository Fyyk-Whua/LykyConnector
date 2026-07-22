using LykyConnector.Core.Sign;
using Microsoft.Extensions.DependencyInjection;

namespace LykyConnector.Core.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLykyCore(this IServiceCollection services)
    {
        services.AddSingleton<ISignService, SignService>();
        services.AddSingleton<IConfigStore>(sp => new ConfigStore());
        return services;
    }
}
