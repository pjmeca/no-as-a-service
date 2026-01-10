using System.Text.Json.Serialization;
using NoAsAService.Models;

namespace NoAsAService.Serialization;

[JsonSerializable(typeof(EndpointSummary[]))]
[JsonSerializable(typeof(string[]))]
internal partial class AppJsonContext : JsonSerializerContext;
