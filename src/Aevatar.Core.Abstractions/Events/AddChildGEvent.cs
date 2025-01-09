// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class AddChildGEvent : GEventBase
{
    [Id(0)] public GrainId Child { get; set; }
}