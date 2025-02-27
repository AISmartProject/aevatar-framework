using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateBseAsyncObserver: IAsyncObserver<StateBase>
{
    private readonly IGAgent _gAgent;
    private readonly IEnumerable<IGAgentStateHandler> _stateHandlers;

    public StateBseAsyncObserver(IGAgent gAgent, IEnumerable<IGAgentStateHandler> stateHandlers)
    {
        _gAgent = gAgent;
        _stateHandlers = stateHandlers;
    }

    public Task OnNextAsync(StateBase item, StreamSequenceToken? token = null)
    {
        return Task.WhenAll(_stateHandlers.Select(handler => handler.HandleAsync(_gAgent.GetGrainId(), item)));
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}