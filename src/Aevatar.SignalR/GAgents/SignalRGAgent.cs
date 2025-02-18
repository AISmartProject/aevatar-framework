using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.SignalR.Core;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using RulesEngine.Models;

namespace Aevatar.SignalR.GAgents;

[GenerateSerializer]

public class SignalRGAgentState : StateBase
{
    [Id(0)] public string Filter { get; set; } = string.Empty;
    [Id(1)] public string ConnectionId { get; set; } = string.Empty;
    [Id(2)] public Guid? CorrelationId { get; set; }
}

[GenerateSerializer]
public class SignalRStateLogEvent : StateLogEventBase<SignalRStateLogEvent>
{

}

[GenerateSerializer]
public class SignalRGAgentConfiguration : ConfigurationBase
{
    /// <summary>
    /// TODO: Not useful for now.
    /// </summary>
    [Id(0)] public string Filter { get; set; } = string.Empty;
    [Id(1)] public string ConnectionId { get; set; } = string.Empty;
}

[GAgent]
public abstract class SignalRGAgentBase<TEvent> :
    GAgentBase<SignalRGAgentState, SignalRStateLogEvent, EventBase, SignalRGAgentConfiguration>,
    ISignalRGAgent<TEvent> where TEvent : EventBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private HubContext<AevatarSignalRHub<TEvent>> _hubContext = default!;

    public SignalRGAgentBase(IGrainFactory grainFactory, IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
        _hubContext = new HubContext<AevatarSignalRHub<TEvent>>(grainFactory);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("SignalR Publisher.");
    }

    public async Task PublishEventAsync(TEvent @event)
    {
        await PublishAsync(@event);
    }

    // [AllEventHandler]
    // public async Task ResponseToSignalRAsync(EventWrapperBase eventWrapperBase)
    // {
    //     var eventWrapper = (EventWrapper<EventBase>)eventWrapperBase;
    //     var filter = State.Filter;
    //     if (!filter.IsNullOrEmpty())
    //     {
    //         var workflow = Newtonsoft.Json.JsonConvert.DeserializeObject<Workflow>(filter);
    //         var rulesEngine = new RulesEngine.RulesEngine([workflow]);
    //         var validateResult = await rulesEngine.ExecuteAllRulesAsync("EventWrapperFilter");
    //         if (validateResult.First().IsSuccess)
    //         {
    //             _hubContext.Group()
    //         }
    //     }
    // }

    protected override async Task PerformConfigAsync(SignalRGAgentConfiguration configuration)
    {
        RaiseEvent(new InitializeSignalRStateLogEvent
        {
            Filter = configuration.Filter,
            ConnectionId = configuration.ConnectionId,
            CorrelationId = configuration.CorrelationId
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task ResponseToSignalRAsync(ResponseToPublisherEventBase @event)
    {
        if (@event.CorrelationId != State.CorrelationId)
        {
            return;
        }
        await _hubContext.Client(State.ConnectionId).Send(SignalROrleansConstants.MethodName, @event);
        var parentGAgentGrainId = await GetParentAsync();
        var parentGAgent = await _gAgentFactory.GetGAgentAsync(parentGAgentGrainId);
        await parentGAgent.UnregisterAsync(this);
    }

    protected override void GAgentTransitionState(SignalRGAgentState state, StateLogEventBase<SignalRStateLogEvent> @event)
    {
        if (@event is InitializeSignalRStateLogEvent initializeSignalRStateLogEvent)
        {
            State.ConnectionId = initializeSignalRStateLogEvent.ConnectionId;
            State.CorrelationId = initializeSignalRStateLogEvent.CorrelationId;
            State.Filter = initializeSignalRStateLogEvent.Filter;
        }
    }

    [GenerateSerializer]
    public class InitializeSignalRStateLogEvent : SignalRStateLogEvent
    {
        [Id(0)] public string Filter { get; set; } = string.Empty;
        [Id(1)] public string ConnectionId { get; set; } = string.Empty;
        [Id(2)] public Guid? CorrelationId { get; set; }
    }
}