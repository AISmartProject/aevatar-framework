﻿namespace Aevatar.SignalR.Core;

[GenerateSerializer]
public sealed class ServerDirectoryState
{
    [Id(0)]
    public Dictionary<Guid, DateTime> ServerHeartBeats { get; set; } = new();
}
