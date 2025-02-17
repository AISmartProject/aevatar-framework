using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aevatar.Core;

public class GAgentFactory : IGAgentFactory
{
    private readonly IClusterClient _clusterClient;
    private readonly IStreamProvider _streamProvider;

    public GAgentFactory(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _streamProvider =
            _clusterClient.ServiceProvider.GetRequiredKeyedService<IStreamProvider>(AevatarCoreConstants
                .StreamProvider);
    }

    public async Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializationEventBase? initializationEvent = null)
    {
        var gAgent = _clusterClient.GetGrain<IGAgent>(grainId);
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public Task<IGAgent> GetGAgentAsync(GrainType grainType, string primaryKeySeed, InitializationEventBase? initializationEvent = null)
    {
        throw new NotImplementedException();
    }

    public async Task<IGAgent> GetGAgentAsync(Guid primaryKey, string alias, string ns,
        InitializationEventBase? initializationEvent = null)
    {
        var gAgent =
            _clusterClient.GetGrain<IGAgent>(GrainId.Create($"{ns}/{alias}",
                primaryKey.ToString("N")));
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public Task<IGAgent> GetGAgentAsync(string primaryKeySeed, string alias, string ns,
        InitializationEventBase? initializationEvent = null)
    {
        throw new NotImplementedException();
    }

    public async Task<IGAgent> GetGAgentAsync(string alias, string ns,
        InitializationEventBase? initializationEvent = null)
    {
        return await GetGAgentAsync(Guid.NewGuid(), alias, ns, initializationEvent);
    }

    public async Task<IGAgent> GetGAgentAsync(Guid primaryKey, Type gAgentType,
        InitializationEventBase? initializationEvent = null)
    {
        return await GetGAgentAsync(primaryKey, gAgentType.Name, gAgentType.Namespace!,
            initializationEvent: initializationEvent);
    }

    public Task<IGAgent> GetGAgentAsync(string primaryKeySeed, Type gAgentType, InitializationEventBase? initializationEvent = null)
    {
        throw new NotImplementedException();
    }

    public async Task<IGAgent> GetGAgentAsync(Type gAgentType, InitializationEventBase? initializationEvent = null)
    {
        return await GetGAgentAsync(gAgentType.Name, ns: gAgentType.Namespace!,
            initializationEvent: initializationEvent);
    }

    public async Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent
    {
        var gAgent = _clusterClient.GetGrain<TGrainInterface>(primaryKey);
        await InitializeGAgentAsync(gAgent, initializationEvent);
        return gAgent;
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(string primaryKeySeed, InitializationEventBase? initializationEvent = null) where TGrainInterface : IGAgent
    {
        throw new NotImplementedException();
    }

    public Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent
    {
        return GetGAgentAsync<TGrainInterface>(Guid.NewGuid(), initializationEvent);
    }

    private async Task InitializeGAgentAsync(IGAgent gAgent, InitializationEventBase? initializationEvent)
    {
        // Must activate the GAgent before sending events.
        await gAgent.ActivateAsync();
        if (initializationEvent != null)
        {
            var eventWrapper = new EventWrapper<EventBase>(initializationEvent, Guid.NewGuid(), gAgent.GetGrainId());
            var streamId = StreamId.Create(AevatarCoreConstants.StreamNamespace, gAgent.GetGrainId().ToString());
            var stream = _streamProvider.GetStream<EventWrapperBase>(streamId);
            await stream.OnNextAsync(eventWrapper);
        }
    }
}