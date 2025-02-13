using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Name))]
public sealed class Decoration
{
    public int Id { get; set; }
    
    [StringLength(32)]
    public required string Name { get; set; }

    public int DecorationSizeId { get; set; }
    public required DecorationSize DecorationSize { get; set; }

    public Dictionary<string, string> Musics { get; set; } = [];
    public Dictionary<string, string> Sounds { get; set; } = [];
    
    [StringLength(byte.MaxValue)]
    public string? Remap { get; set; }
}