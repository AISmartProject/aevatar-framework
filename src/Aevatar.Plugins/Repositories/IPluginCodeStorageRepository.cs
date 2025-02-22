using Aevatar.Plugins.DbContexts;
using Aevatar.Plugins.Entities;
using MongoDB.Driver;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace Aevatar.Plugins.Repositories;

public interface IPluginCodeStorageRepository : IRepository<PluginCodeStorageSnapshotDocument, string>
{
    Task<byte[]?> GetPluginCodeByGAgentPrimaryKey(Guid primaryKey);
    Task<List<byte[]>> GetPluginCodesByGAgentPrimaryKeys(List<Guid> primaryKeys);
}

public class PluginCodeStorageRepository :
    MongoDbRepository<PluginCodeStorageMongoDbContext, PluginCodeStorageSnapshotDocument, string>,
    IPluginCodeStorageRepository, ITransientDependency
{
    private const string GAgentTypeName = "Aevatar.Plugins.pluginCodeStorage";

    public PluginCodeStorageRepository(IMongoDbContextProvider<PluginCodeStorageMongoDbContext> dbContextProvider,
        IServiceProvider serviceProvider) : base(dbContextProvider)
    {
        LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
    }

    public async Task<byte[]?> GetPluginCodeByGAgentPrimaryKey(Guid primaryKey)
    {
        var dbContext = await GetDbContextAsync();
        var document = await dbContext.PluginCodeStorage
            .Find(pc => pc.Id == $"{GAgentTypeName}/{primaryKey:N}")
            .ToListAsync();
        return document.FirstOrDefault()?.Doc.Snapshot.Code.Value;
    }

    public async Task<List<byte[]>> GetPluginCodesByGAgentPrimaryKeys(List<Guid> primaryKeys)
    {
        var codeList = new List<byte[]>();
        foreach (var primaryKey in primaryKeys)
        {
            var code = await GetPluginCodeByGAgentPrimaryKey(primaryKey);
            if (code != null)
            {
                codeList.Add(code);
            }
        }

        return codeList;
    }

    protected override CancellationToken GetCancellationToken(
        CancellationToken preferredValue = new())
    {
        return new CancellationToken();
    }
}