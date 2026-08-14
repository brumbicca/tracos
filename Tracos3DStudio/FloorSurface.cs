using System.Linq;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class FloorSurface
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public List<Vector2> Points { get; } = new();

    public List<FloorZone> Zones { get; } = new();

    public string DefaultMaterialId { get; set; } = FloorMaterialCatalog.DefaultMaterialId;

    public bool Visible { get; set; } = true;

    public float Height { get; set; } = 0f;

    public MeshData Mesh { get; } = new();

    public FloorSurface()
    {
    }

    public FloorSurface(IEnumerable<Vector2> points)
    {
        Points.AddRange(points);
    }

    public bool TryGetBounds(out Vector2 min, out Vector2 max)
    {
        min = Vector2.Zero;
        max = Vector2.Zero;

        if (Points.Count == 0)
            return false;

        min = new Vector2(Points.Min(p => p.X), Points.Min(p => p.Y));
        max = new Vector2(Points.Max(p => p.X), Points.Max(p => p.Y));
        return true;
    }
}