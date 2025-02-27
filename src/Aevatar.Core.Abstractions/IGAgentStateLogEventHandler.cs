namespace Aevatar.Core.Abstractions;

public interface IGAgentStateLogEventHandler
{
    Task HandleEventAsync(GrainId grainId, StateLogEventBase stateLogEvent);
}