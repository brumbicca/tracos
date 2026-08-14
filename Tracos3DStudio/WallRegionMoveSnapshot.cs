namespace Tracos3DStudio;

public sealed class WallRegionMoveSnapshot
{
    public WallRegionShape Shape { get; private init; }

    public float StartAlongMm { get; private init; }

    public float EndAlongMm { get; private init; }

    public float BottomMm { get; private init; }

    public float TopMm { get; private init; }

    public float CenterAlongMm { get; private init; }

    public float CenterHeightMm { get; private init; }

    public float RadiusMm { get; private init; }

    public float RotationDegrees { get; private init; }

    public float[] PolygonAlongMm { get; private init; } = [];

    public float[] PolygonHeightMm { get; private init; } = [];

    public static WallRegionMoveSnapshot From(WallRegion region) =>
        new()
        {
            Shape = region.Shape,
            StartAlongMm = region.StartAlongMm,
            EndAlongMm = region.EndAlongMm,
            BottomMm = region.BottomMm,
            TopMm = region.TopMm,
            CenterAlongMm = region.CenterAlongMm,
            CenterHeightMm = region.CenterHeightMm,
            RadiusMm = region.RadiusMm,
            RotationDegrees = region.RotationDegrees,
            PolygonAlongMm = region.PolygonAlongMm.ToArray(),
            PolygonHeightMm = region.PolygonHeightMm.ToArray()
        };

    public void RestoreTo(WallRegion region)
    {
        region.Shape = Shape;
        region.StartAlongMm = StartAlongMm;
        region.EndAlongMm = EndAlongMm;
        region.BottomMm = BottomMm;
        region.TopMm = TopMm;
        region.CenterAlongMm = CenterAlongMm;
        region.CenterHeightMm = CenterHeightMm;
        region.RadiusMm = RadiusMm;
        region.RotationDegrees = RotationDegrees;
        region.PolygonAlongMm.Clear();
        region.PolygonAlongMm.AddRange(PolygonAlongMm);
        region.PolygonHeightMm.Clear();
        region.PolygonHeightMm.AddRange(PolygonHeightMm);
        WallRegionGeometry.SyncBoundingBox(region);
    }
}
