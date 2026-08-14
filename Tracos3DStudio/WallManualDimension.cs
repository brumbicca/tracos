using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Cota manual no Editor de Paredes (Promob M5).</summary>
public sealed class WallManualDimension
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public WallManualDimensionKind Kind { get; set; }

    public Vector2 PointA { get; set; }

    public Vector2 PointB { get; set; }

    public Vector2 PointC { get; set; }

    public Vector2 DimStart { get; set; }

    public Vector2 DimEnd { get; set; }

    public float ArcRadius { get; set; }

    public float DisplayValue { get; set; }
}
