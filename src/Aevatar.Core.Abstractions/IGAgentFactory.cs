namespace Aevatar.Core.Abstractions;

public interface IGAgentFactory
{
    Task<IGAgent> GetGAgentAsync(GrainId grainId, InitializationEventBase? initializationEvent = null);
    Task<IGAgent> GetGAgentAsync(GrainType grainType, string primaryKeySeed, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(Guid primaryKey, string alias,
        string ns, InitializationEventBase? initializationEvent = null);
    Task<IGAgent> GetGAgentAsync(string primaryKeySeed, string alias,
        string ns, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(string alias, string ns,
        InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(Guid primaryKey, Type gAgentType, InitializationEventBase? initializationEvent = null);
    Task<IGAgent> GetGAgentAsync(string primaryKeySeed, Type gAgentType, InitializationEventBase? initializationEvent = null);

    Task<IGAgent> GetGAgentAsync(Type gAgentType, InitializationEventBase? initializationEvent = null);

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(Guid primaryKey,
        InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent;
    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(string primaryKeySeed,
        InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent;

    Task<TGrainInterface> GetGAgentAsync<TGrainInterface>(InitializationEventBase? initializationEvent = null)
        where TGrainInterface : IGAgent;
}