namespace GbxTools3D.Client.Dtos;

public sealed class DecorationDto
{
    public required string Name { get; set; }
    public required Dictionary<string, string> Musics { get; set; }
    public required Dictionary<string, string> Sounds { get; set; }
    public string? Remap { get; set; }
}