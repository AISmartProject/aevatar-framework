namespace Aevatar.Core.Abstractions;

public interface IGAgentManager
{
    List<Type> GetAvailableGAgentTypes();
    List<Type> GetAvailableEventTypes();
    List<GrainType> GetAvailableGAgentGrainTypes();
}