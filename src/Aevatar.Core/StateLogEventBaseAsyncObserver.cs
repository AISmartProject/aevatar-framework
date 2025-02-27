using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Core;

public class StateLogEventBaseAsyncObserver : IAsyncObserver<StateLogEventBase>
{
    private readonly IGAgent _gAgent;
    private readonly IEnumerable<IGAgentStateLogEventHandler> _stateLogEventHandlers;

    public StateLogEventBaseAsyncObserver(IGAgent gAgent, IEnumerable<IGAgentStateLogEventHandler> stateLogEventHandlers)
    {
        _gAgent = gAgent;
        _stateLogEventHandlers = stateLogEventHandlers;
    }

    public Task OnNextAsync(StateLogEventBase item, StreamSequenceToken? token = null)
    {
        return Task.WhenAll(_stateLogEventHandlers.Select(handler => handler.HandleEventAsync(_gAgent.GetGrainId(), item)));
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