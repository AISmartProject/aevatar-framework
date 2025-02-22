using Aevatar.Plugins.DbContexts;
using Aevatar.Plugins.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.Plugins;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule)
)]
public class AevatarPluginsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<PluginGAgentLoadOptions>(context.Services.GetConfiguration().GetSection("Plugins"));
        context.Services.AddTransient<ITenantPluginCodeRepository, TenantPluginCodeRepository>();
        context.Services.AddTransient<IPluginCodeStorageRepository, PluginCodeStorageRepository>();
        context.Services.AddTransient<TenantPluginCodeMongoDbContext>();
        context.Services.AddTransient<PluginCodeStorageMongoDbContext>();
    }
}