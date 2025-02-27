using Aevatar.Core.Abstractions;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Core.StateHandlers;

public class EventDispatchStateHandler : IGAgentStateHandler
{
    private readonly IEventDispatcher _eventDispatcher;

    public EventDispatchStateHandler(IEventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
    }
    
    public async Task HandleAsync(GrainId grainId, StateBase state)
    {
        await _eventDispatcher.PublishAsync(state, grainId);
    }
}