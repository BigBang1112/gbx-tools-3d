using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Name))]
public sealed class Decoration
{
    public int Id { get; set; }
    
    [StringLength(32)]
    public required string Name { get; set; }

    public int DecorationSizeId { get; set; }
    public required DecorationSize DecorationSize { get; set; }

    public ImmutableDictionary<string, string> Musics { get; set; } = ImmutableDictionary<string, string>.Empty;
    public ImmutableDictionary<string, string> Sounds { get; set; } = ImmutableDictionary<string, string>.Empty;

    [StringLength(byte.MaxValue)]
    public string? Remap { get; set; }
}