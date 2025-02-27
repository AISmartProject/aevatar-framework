using Aevatar.Core.Abstractions;

namespace Aevatar.Core.StateLogEventHandlers;

public class EventDispatchStateLogEventHandler : IGAgentStateLogEventHandler
{
    private readonly IEventDispatcher _eventDispatcher;

    public EventDispatchStateLogEventHandler(IEventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
    }

    public async Task HandleEventAsync(GrainId grainId, StateLogEventBase stateLogEvent)
    {
        await _eventDispatcher.PublishAsync(stateLogEvent.Id, grainId, stateLogEvent);
    }
}