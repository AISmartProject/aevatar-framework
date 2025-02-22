using Volo.Abp.DependencyInjection;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins.DbContexts;

public class AevatarMongoDbContextBase : AbpMongoDbContext
{
    public AevatarMongoDbContextBase(IServiceProvider serviceProvider)
    {
        LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
    }

    protected void InitializeModelSource()
    {
        ModelSource = new MongoModelSource();
        ModelSource.GetModel(this);
    }
}