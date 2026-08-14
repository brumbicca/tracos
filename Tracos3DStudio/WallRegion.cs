namespace Tracos3DStudio;

/// <summary>Região na face da parede (retangular ou circular — Editor de Regiões Promob).</summary>
public sealed class WallRegion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Name { get; set; }

    public WallRegionShape Shape { get; set; } = WallRegionShape.Rectangular;

    public FaceType Face { get; set; } = FaceType.Internal;

    public float StartAlongMm { get; set; }

    public float EndAlongMm { get; set; }

    /// <summary>Base da região em mm desde o afastamento piso da parede.</summary>
    public float BottomMm { get; set; }

    /// <summary>Topo da região em mm desde o afastamento piso da parede.</summary>
    public float TopMm { get; set; }

    /// <summary>Centro ao longo da parede (região circular).</summary>
    public float CenterAlongMm { get; set; }

    /// <summary>Centro na altura (região circular).</summary>
    public float CenterHeightMm { get; set; }

    /// <summary>Raio em mm (região circular).</summary>
    public float RadiusMm { get; set; }

    /// <summary>Offset Forma Promob — expansão (+) ou recuo (-) uniforme em todas as bordas (mm).</summary>
    public float OffsetMm { get; set; }

    /// <summary>Offset na aresta inicial (along menor) — positivo expande para fora (mm).</summary>
    public float OffsetEdgeStartAlongMm { get; set; }

    /// <summary>Offset na aresta final (along maior) — positivo expande para fora (mm).</summary>
    public float OffsetEdgeEndAlongMm { get; set; }

    /// <summary>Offset na aresta inferior — positivo expande para fora (mm).</summary>
    public float OffsetEdgeBottomMm { get; set; }

    /// <summary>Offset na aresta superior — positivo expande para fora (mm).</summary>
    public float OffsetEdgeTopMm { get; set; }

    public string? MaterialId { get; set; }

    /// <summary>Rotação em graus (sentido anti-horário na face) — R8.</summary>
    public float RotationDegrees { get; set; }

    /// <summary>Vértices em mm (along × altura) — <see cref="WallRegionShape.Polygon"/>.</summary>
    public List<float> PolygonAlongMm { get; } = new();

    public List<float> PolygonHeightMm { get; } = new();
}
