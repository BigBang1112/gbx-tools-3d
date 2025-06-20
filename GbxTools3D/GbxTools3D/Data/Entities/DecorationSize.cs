using GbxTools3D.Client.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(SizeX), nameof(SizeY), nameof(SizeZ))]
public sealed class DecorationSize
{
    public int Id { get; set; }
    public required int SizeX { get; set; }
    public required int SizeY { get; set; }
    public required int SizeZ { get; set; }
    public int BaseHeight { get; set; }
    public bool OffsetBlockY { get; set; }

    [StringLength(64)]
    public required string SceneName { get; set; }

    public ImmutableArray<SceneObject> Scene { get; set; } = [];
    
    public int CollectionId { get; set; }
    public required Collection Collection { get; set; }
    
    public ICollection<Decoration> Decorations { get; set; } = [];
}