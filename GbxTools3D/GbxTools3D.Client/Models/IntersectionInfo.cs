using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace GbxTools3D.Client.Models;

public sealed record IntersectionInfo(JSObject Object, string MaterialName, JsonDocument? MaterialUserData);
