// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

public class PublishAttribute : Attribute
{
    public Type[] PublishedTypes { get; set; }

    public PublishAttribute(Type[] publishedTypes)
    {
        PublishedTypes = publishedTypes;
    }
}