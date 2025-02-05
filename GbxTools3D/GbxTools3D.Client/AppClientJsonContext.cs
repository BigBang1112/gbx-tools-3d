using System.Text.Json;
using System.Text.Json.Serialization;
using GbxTools3D.Client.Converters;
using GbxTools3D.Client.Dtos;

namespace GbxTools3D.Client;

[JsonSerializable(typeof(MapContentDto))]
[JsonSerializable(typeof(List<BlockInfoDto>))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true, Converters = [typeof(JsonInt3Converter)])]
internal partial class AppClientJsonContext : JsonSerializerContext;