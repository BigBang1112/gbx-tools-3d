using System.Text.Json;

namespace GbxTools3D.Client.Models;

public sealed record IntersectionInfo(string ObjectName, string MaterialName, JsonDocument? MaterialUserData);
