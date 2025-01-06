using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GenerateSerializer]
public class SubscribeTestGAgentState : StateBase
{
    [Id(0)]  public Dictionary<Type, List<Type>> SubscriptionInfo { get; set; }
}

public class SubscribeTestGEvent : GEventBase;

[GAgent("subscribeTest", "Test")]
public class SubscribeTestGAgent : GAgentBase<SubscribeTestGAgentState, SubscribeTestGEvent>
{
    public SubscribeTestGAgent(ILogger logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent is used to test implementation of GetAllSubscribedEventsAsync.");
    }
    
    public async Task HandleEventAsync(SubscribedEventListEvent eventData)
    {
        if (State.SubscriptionInfo.IsNullOrEmpty())
        {
            State.SubscriptionInfo = eventData.Value;
        }
    }
}