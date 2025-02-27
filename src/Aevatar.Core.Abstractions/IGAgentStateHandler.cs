namespace Aevatar.Core.Abstractions;

public interface IGAgentStateHandler
{
    Task HandleAsync(GrainId grainId, StateBase state);
}