using Aevatar.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .ConfigureServices(services =>
            {
                AsyncHelper.RunSync(() => LoadPluginsAsync(services, abpApplication));

                foreach (var service in abpApplication.Services)
                {
                    services.Add(service);
                }
            });
    }

    public static ISiloBuilder UseAevatar<TAbpModule>(this ISiloBuilder builder) where TAbpModule : AbpModule
    {
        var abpApplication = AbpApplicationFactory.Create<TAbpModule>();
        abpApplication.Initialize();

        return builder
            .UseAevatar()
            .ConfigureServices(services =>
            {
                foreach (var service in abpApplication.Services)
                {
                    services.Add(service);
                }
            });
    }

    private static async Task LoadPluginsAsync(IServiceCollection services, IAbpApplicationWithInternalServiceProvider application)
    {
        var assemblies = await application.GetTenantPluginAssemblyListAsync();
        services.AddSerializer(options =>
        {
            foreach (var assembly in assemblies)
            {
                options.AddAssembly(assembly);
            }
        });
    }

    public static IClientBuilder UseAevatar(this IClientBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .ConfigureServices(services =>
        {
            foreach (var service in abpApplication.Services)
            {
                services.Add(service);
            }
        });
    }
}