using Orleans.Metadata;

// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class GAgentAttribute : Attribute, IGrainTypeProviderAttribute
{
    private readonly string? _ns;
    private readonly string? _alias;

    public GAgentAttribute()
    {

    }

    public GAgentAttribute(string alias)
    {
        _alias = alias;
    }

    public GAgentAttribute(string alias, string ns)
    {
        _alias = alias;
        _ns = ns;
    }

    public GrainType GetGrainType(IServiceProvider services, Type type)
    {
        if (_alias == null) // Use ctor with 0 parameters.
        {
            return GrainType.Create($"{type.Namespace}{AevatarCoreConstants.GAgentNamespaceSeparator}{type.Name}");
        }

        if (_ns == null)
        {
            return GrainType.Create($"{type.Namespace}{AevatarCoreConstants.GAgentNamespaceSeparator}{_alias}");
        }

        return GrainType.Create($"{_ns}{AevatarCoreConstants.GAgentNamespaceSeparator}{_alias}");
    }
}