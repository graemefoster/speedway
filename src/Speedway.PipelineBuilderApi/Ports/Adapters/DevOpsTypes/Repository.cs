using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Speedway.PipelineBuilderApi.Ports.Adapters.DevOpsTypes
{
    public record Repository(Guid Id, string RemoteUrl, string Name);
    public record Policy(int Id, Dictionary<string, JsonElement> Settings, PolicyType Type);
    public record PolicyType (string Id, string Url, string DisplayName);
    public record PolicyScope(string RefName, string MatchKind, Guid? RepositoryId);
}