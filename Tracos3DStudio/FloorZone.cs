using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Região do piso com material próprio (estilo Promob — vários acabamentos no mesmo ambiente).</summary>
public sealed class FloorZone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string MaterialId { get; set; } = FloorMaterialCatalog.DefaultMaterialId;

    public string Name { get; set; } = "Região";

    public WallRegionShape Shape { get; set; } = WallRegionShape.Rectangular;

  /// <summary>Retângulo axis-aligned em mm (planta X/Y).</summary>
    public float MinX { get; set; }

    public float MinY { get; set; }

    public float MaxX { get; set; }

    public float MaxY { get; set; }

    public float CenterX { get; set; }

    public float CenterY { get; set; }

    public float RadiusMm { get; set; }

    /// <summary>Offset Forma Promob — uniforme em todas as bordas (mm).</summary>
    public float OffsetMm { get; set; }

    public float OffsetEdgeStartAlongMm { get; set; }

    public float OffsetEdgeEndAlongMm { get; set; }

    public float OffsetEdgeBottomMm { get; set; }

    public float OffsetEdgeTopMm { get; set; }

    public List<float> PolygonAlongMm { get; } = new();

    public List<float> PolygonHeightMm { get; } = new();

    public float Width => MathF.Max(0f, MaxX - MinX);

    public float Depth => MathF.Max(0f, MaxY - MinY);

    public bool IsValid
    {
        get
        {
            if (Shape == WallRegionShape.Circular)
                return RadiusMm >= FloorZoneService.MinCircleRadiusMm;

            if (Shape == WallRegionShape.Polygon)
                return PolygonAlongMm.Count >= FloorZoneService.MinPolygonVertices;

            return Width >= FloorZoneService.MinSpanMm && Depth >= FloorZoneService.MinSpanMm;
        }
    }

    public Vector2 Center => Shape == WallRegionShape.Circular
        ? new Vector2(CenterX, CenterY)
        : new Vector2((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f);

    public List<Vector2> ToPoints() => FloorZoneGeometry.GetOutlinePoints(this);

    public static FloorZone FromCorners(Vector2 a, Vector2 b)
    {
        float minX = MathF.Min(a.X, b.X);
        float minY = MathF.Min(a.Y, b.Y);
        float maxX = MathF.Max(a.X, b.X);
        float maxY = MathF.Max(a.Y, b.Y);

        return new FloorZone
        {
            Shape = WallRegionShape.Rectangular,
            MinX = minX,
            MinY = minY,
            MaxX = maxX,
            MaxY = maxY
        };
    }

    public bool ContainsPoint(Vector2 point) => FloorZoneGeometry.ContainsPoint(this, point);
}
