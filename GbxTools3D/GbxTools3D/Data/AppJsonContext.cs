using GbxTools3D.Data.Entities;
using GbxTools3D.External;
using GbxTools3D.External.MX;
using GbxTools3D.External.TMX;
using System.Text.Json.Serialization;

namespace GbxTools3D.Data;

[JsonSerializable(typeof(BlockUnit[]))]
[JsonSerializable(typeof(MxResponse<TmxTrackInfo>))]
[JsonSerializable(typeof(MxResponse<TmxReplayInfo>))]
[JsonSerializable(typeof(MxResponse<MxMapInfo>))]
[JsonSerializable(typeof(IxItemInfo[]))]
internal partial class AppJsonContext : JsonSerializerContext;