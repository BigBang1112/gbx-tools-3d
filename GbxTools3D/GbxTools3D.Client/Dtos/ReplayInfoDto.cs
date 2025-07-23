namespace GbxTools3D.Client.Dtos;

public sealed class ReplayInfoDto
{
    public required string UploaderId { get; set; }
    public required string UploaderName { get; set; }
    public required DateTime UploadedAt { get; set; }
    public int? Position { get; set; }
}
