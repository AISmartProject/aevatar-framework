﻿using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Plugins;
using Aevatar.Plugins.DbContexts;
using Aevatar.Plugins.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;
using Volo.Abp.Uow;

namespace Aevatar;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpMongoDbModule),
    typeof(AbpDddDomainModule),
    typeof(AbpUnitOfWorkModule),
    typeof(AevatarPluginsModule)
)]
public class AevatarModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new AevatarDefaultConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarModule>(); });
        context.Services.AddSingleton<IGAgentManager, GAgentManager>();
        context.Services.AddSingleton<IGAgentFactory, GAgentFactory>();
        context.Services.AddSingleton<IConfigureGrainTypeComponents, ConfigureAevatarGrainActivator>();
    }
}