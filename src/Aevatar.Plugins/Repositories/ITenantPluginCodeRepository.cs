using Aevatar.Plugins.DbContexts;
using Aevatar.Plugins.Entities;
using MongoDB.Driver;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins.Repositories;

public interface ITenantPluginCodeRepository : IRepository<TenantPluginCodeSnapshotDocument, string>
{
    Task<List<Guid>?> GetGAgentPrimaryKeysByTenantIdAsync(Guid tenantId);
}

public class TenantPluginCodeRepository :
    MongoDbRepository<TenantPluginCodeMongoDbContext, TenantPluginCodeSnapshotDocument, string>,
    ITenantPluginCodeRepository, ITransientDependency
{
    private const string GAgentTypeName = "Aevatar.Plugins.pluginTenant";

    public TenantPluginCodeRepository(IMongoDbContextProvider<TenantPluginCodeMongoDbContext> dbContextProvider,
        IServiceProvider serviceProvider) : base(dbContextProvider)
    {
        LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
    }

    public async Task<List<Guid>?> GetGAgentPrimaryKeysByTenantIdAsync(Guid tenantId)
    {
        var dbContext = await GetDbContextAsync();
        var grainIdString = $"{GAgentTypeName}/{tenantId:N}";
        var document = await dbContext.TenantPluginCode
            .Find(pc => pc.Id == grainIdString)
            .ToListAsync();
        return document.FirstOrDefault()?.Doc.Snapshot.CodeStorageGuids.Values;
    }

    protected override CancellationToken GetCancellationToken(
        CancellationToken preferredValue = new())
    {
        return new CancellationToken();
    }
}