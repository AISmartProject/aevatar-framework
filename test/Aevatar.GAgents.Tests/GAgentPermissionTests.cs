using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Core.Tests.TestGAgents;
using Aevatar.PermissionManagement;
using Aevatar.Plugins.Extensions;
using Shouldly;
using Volo.Abp.PermissionManagement;

namespace Aevatar.GAgents.Tests;

public sealed class GAgentPermissionTests : AevatarGAgentsTestBase, IAsyncLifetime
{
    private readonly IPermissionManager _permissionManager;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IPermissionInfoProvider _permissionInfoProvider;

    public GAgentPermissionTests()
    {
        _permissionManager = GetRequiredService<IPermissionManager>();
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _permissionInfoProvider = GetRequiredService<IPermissionInfoProvider>();
    }

    [Fact]
    public async Task PermissionInfosProviderTest()
    {
        var allPermissionInfos = _permissionInfoProvider.GetAllPermissionInfos();
        allPermissionInfos.ShouldContain(i =>
            i.Name == "DoSomething1"
            && i.DisplayName == "Only for testing."
            && i.GrainType == "Aevatar.Core.Tests.TestGAgents.PermissionGAgent"
            && i.Type == "Aevatar.Core.Tests.TestGAgents.PermissionGAgent"
        );
        allPermissionInfos.ShouldContain(i =>
            i.Name == "DoSomething2"
            && i.GroupName == "DefaultGroup"
        );
        allPermissionInfos.ShouldContain(i =>
            i.Name == "DoSomething3"
            && i.GroupName == "AnotherGroup"
        );
    }

    public async Task InitializeAsync()
    {
        var userId = "TestUser".ToGuid().ToString();
        await _permissionManager.SetAsync("DoSomething", "User", userId, true);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}