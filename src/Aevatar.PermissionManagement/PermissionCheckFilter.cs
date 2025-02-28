using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Aevatar.PermissionManagement;

public class PermissionCheckFilter : IIncomingGrainCallFilter
{
    private readonly IServiceProvider _serviceProvider;
    private IPermissionChecker _permissionChecker;

    public PermissionCheckFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var method = context.ImplementationMethod;
        var permissionAttribute = method.GetCustomAttribute<PermissionAttribute>();

        if (permissionAttribute == null)
        {
            await context.Invoke();
            return;
        }

        if (RequestContext.Get("CurrentUser") is not UserContext currentUser)
        {
            throw new AuthenticationException("Request requires authentication");
        }

        using var scope = _serviceProvider.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();
        var permissionName = permissionAttribute.Name;

        var principal = BuildClaimsPrincipal(currentUser);

        if (!await checker.IsGrantedAsync(principal, permissionName))
        {
            throw new AuthenticationException($"Missing permission: {permissionName}");
        }

        await context.Invoke();
    }

    private static ClaimsPrincipal BuildClaimsPrincipal(UserContext user)
    {
        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, user.UserId.ToString()),
            new(AbpClaimTypes.UserName, user.UserName),
            new(AbpClaimTypes.Email, user.Email)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(AbpClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}