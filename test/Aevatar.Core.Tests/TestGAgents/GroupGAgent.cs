using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGEvents;
using Aevatar.Core.Tests.TestStates;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

[GAgent("group", "Test")]
public class GroupGAgent : GAgentBase<GroupGAgentState, GroupGEvent>
{
    public GroupGAgent(ILogger<GroupGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("An agent to inform other agents when a social event is published.");
    }

    protected override Task OnRegisterAgentAsync(Guid agentGuid)
    {
        ++State.RegisteredGAgents;
        return Task.CompletedTask;
    }

    protected override Task OnUnregisterAgentAsync(Guid agentGuid)
    {
        --State.RegisteredGAgents;
        return Task.CompletedTask;
    }
    
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        State.RegisteredGAgents = 0;
    }
}