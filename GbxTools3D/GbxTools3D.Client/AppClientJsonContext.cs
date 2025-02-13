using System.Text.Json;
using System.Text.Json.Serialization;
using GbxTools3D.Client.Converters;
using GbxTools3D.Client.Dtos;

namespace GbxTools3D.Client;

[JsonSerializable(typeof(MapContentDto))]
[JsonSerializable(typeof(List<BlockInfoDto>))]
[JsonSerializable(typeof(List<DecorationSizeDto>))]
[JsonSerializable(typeof(Dictionary<string, MaterialDto>))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, 
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, 
    UseStringEnumConverter = true, 
    Converters = [typeof(JsonInt3Converter)])]
internal partial class AppClientJsonContext : JsonSerializerContext;