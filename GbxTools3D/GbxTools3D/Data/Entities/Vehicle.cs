using GBX.NET;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class Vehicle
{
    public int Id { get; set; }

    public required GameVersion GameVersion { get; set; }

    [StringLength(32)]
    public required string Name { get; set; }

    public int? IconId { get; set; }
    public Icon? Icon { get; set; }

    public float CameraFov { get; set; }
    public float CameraFar { get; set; }
    public float CameraUp { get; set; }
    public float CameraLookAtFactor { get; set; }
}
