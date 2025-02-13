using GbxTools3D.Data.Entities;
using GbxTools3D.External;
using GbxTools3D.External.MX;
using GbxTools3D.External.TMX;
using System.Text.Json.Serialization;
using GbxTools3D.Client.Models;

namespace GbxTools3D.Data;

[JsonSerializable(typeof(BlockUnit[]))]
[JsonSerializable(typeof(MxResponse<TmxTrackInfo>))]
[JsonSerializable(typeof(MxResponse<TmxReplayInfo>))]
[JsonSerializable(typeof(MxResponse<MxMapInfo>))]
[JsonSerializable(typeof(IxItemInfo[]))]
[JsonSerializable(typeof(SceneObject[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class AppJsonContext : JsonSerializerContext;