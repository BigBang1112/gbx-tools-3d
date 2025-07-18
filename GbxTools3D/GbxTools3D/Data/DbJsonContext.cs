using System.Text.Json.Serialization;
using GbxTools3D.Client.Models;
using System.Collections.Immutable;

namespace GbxTools3D.Data;

[JsonSerializable(typeof(ImmutableArray<SceneObject>))]
[JsonSerializable(typeof(ImmutableDictionary<string, string>))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal partial class DbJsonContext : JsonSerializerContext;