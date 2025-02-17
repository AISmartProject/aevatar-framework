using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestGAgents;
using Shouldly;

namespace Aevatar.GAgents.Tests;

public class GAgentEventHandlerAnalyzerTests : AevatarGAgentsTestBase
{
    [Fact]
    public async Task GAgentEventHandlerAnalyzerTest()
    {
        var types = GAgentEventHandlerAnalyzer.GetPublishedEvents(typeof(InvestorTestGAgent));
        types.Count.ShouldBe(1);
    }
}