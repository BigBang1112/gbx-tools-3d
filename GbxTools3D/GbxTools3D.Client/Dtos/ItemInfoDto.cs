namespace GbxTools3D.Client.Dtos;

public class ItemInfoDto
{
    public required string UploaderId { get; set; }
    public required string UploaderName { get; set; }
    public required string Name { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required int Score { get; set; }
    public required ItemSetInfoDto? Set { get; set; }
}
