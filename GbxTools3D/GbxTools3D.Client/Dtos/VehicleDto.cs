namespace GbxTools3D.Client.Dtos;

public sealed class VehicleDto
{
    public required string Name { get; set; }
    public float CameraFov { get; set; }
    public float CameraFar { get; set; }
    public float CameraUp { get; set; }
    public float CameraLookAtFactor { get; set; }
    public bool HasIcon { get; set; }
}
