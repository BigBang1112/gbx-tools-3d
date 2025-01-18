using GbxTools3D.Data.Entities;
using System.Text.Json.Serialization;

namespace GbxTools3D.Data;

[JsonSerializable(typeof(BlockUnit[]))]
internal partial class AppJsonContext : JsonSerializerContext;