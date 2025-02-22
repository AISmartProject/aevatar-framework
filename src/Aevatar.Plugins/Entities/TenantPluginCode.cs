using MongoDB.Bson.Serialization.Attributes;
using Volo.Abp.Domain.Entities;

namespace Aevatar.Plugins.Entities;

[BsonIgnoreExtraElements]
public class TenantPluginCodeSnapshotDocument : Entity<string>
{

    [BsonElement("_etag")] public string Etag { get; set; }

    [BsonElement("_doc")] public TenantPluginCodeDocEntity Doc { get; set; }
}

[BsonIgnoreExtraElements]
public class TenantPluginCodeDocEntity
{
    [BsonElement("__id")]
    public string InternalId { get; set; }

    [BsonElement("__type")]
    public string Type { get; set; }

    [BsonElement("Snapshot")]
    public TenantPluginCodeSnapshotEntity Snapshot { get; set; }
}

[BsonIgnoreExtraElements]
public class TenantPluginCodeSnapshotEntity
{
    [BsonElement("__id")]
    public string InternalId { get; set; }

    [BsonElement("__type")] 
    public string Type { get; set; }

    [BsonElement("CodeStorageGuids")]
    public CodeStorageGuidList CodeStorageGuids { get; set; }
}

[BsonIgnoreExtraElements]
public class CodeStorageGuidList
{
    [BsonElement("__type")] public string Type { get; set; }

    [BsonElement("__values")] public List<Guid> Values { get; set; } = new();
}