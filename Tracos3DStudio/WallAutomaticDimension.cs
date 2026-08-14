using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Cota linear automática na face de referência (vértices internos — Promob M4).</summary>
public readonly struct WallAutomaticDimension
{
    public Vector2 FaceStart { get; init; }
    public Vector2 FaceEnd { get; init; }
    public Vector2 DimStart { get; init; }
    public Vector2 DimEnd { get; init; }
    public float LengthMm { get; init; }
    public Vector3 LabelWorldPosition { get; init; }
}
