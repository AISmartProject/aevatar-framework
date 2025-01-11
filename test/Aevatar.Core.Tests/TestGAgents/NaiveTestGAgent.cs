using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestInitializeDtos;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GenerateSerializer]
public class NaiveTestGAgentState : StateBase
{
    [Id(0)]  public List<string> Content { get; set; }
}

public class NaiveTestStateLogEvent : StateLogEventBase<NaiveTestStateLogEvent>
{
    [Id(0)] public Guid Id { get; set; }
}

[GAgent("naiveTest")]
public class NaiveTestGAgent : GAgentBase<NaiveTestGAgentState, NaiveTestStateLogEvent,EventBase, NaiveGAgentInitialize>
{
    public NaiveTestGAgent(ILogger<NaiveTestGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a naive test GAgent");
    }

    public override async Task InitializeAsync(NaiveGAgentInitialize initialize)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(initialize.InitialGreeting);
    }
}