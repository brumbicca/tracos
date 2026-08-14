using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Overlays de edição de regiões no piso (contornos, setas offset, preview).</summary>
public static class FloorSurfaceViewportRenderer
{
    private static readonly Vector4 ZoneLineColor = new(0.2f, 0.75f, 0.95f, 0.95f);
    private static readonly Vector4 ZoneBaseLineColor = new(0.15f, 0.45f, 0.65f, 0.7f);
    private static readonly Vector4 ZonePickPreviewColor = new(0.2f, 0.75f, 0.95f, 0.95f);
    private static readonly Vector4 ZoneDragPreviewColor = new(0.35f, 0.9f, 1f, 1f);
    private static readonly Vector4 ZoneOffsetArrowColor = new(0.95f, 0.88f, 0.15f, 1f);

    public const float OffsetArrowSpacingMm = 80f;
    public const float OffsetArrowPickToleranceMm = 100f;
    public const float OffsetArrowSizeMm = 45f;

    public static void DrawZoneOutlines(FloorZone zone, float y = 11.5f)
    {
        bool hasOffset =
            MathF.Abs(zone.OffsetMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeStartAlongMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeEndAlongMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeBottomMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeTopMm) > 0.01f;

        if (hasOffset && zone.Shape == WallRegionShape.Rectangular)
            DrawOutlineLoop(FloorZoneGeometry.GetBaseOutlinePoints(zone), y, ZoneBaseLineColor, 1.5f, depthTest: true);

        DrawOutlineLoop(FloorZoneGeometry.GetOutlinePoints(zone), y, ZoneLineColor, 2f, depthTest: true);
    }

    public static void DrawZoneOffsetArrows(FloorZone zone, float y = 12f)
    {
        if (zone.Shape != WallRegionShape.Rectangular)
            return;

        var (minX, maxX, minY, maxY) = FloorZoneGeometry.GetEffectiveBounds(
            zone, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

        float midX = (minX + maxX) * 0.5f;
        float midY = (minY + maxY) * 0.5f;
        float spacing = OffsetArrowSpacingMm;

        RenderEngine.BeginLineBatch();
        DrawOffsetChevron(minX - spacing, midY, -1f, 0f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(minX + spacing, midY, 1f, 0f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(maxX + spacing, midY, 1f, 0f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(maxX - spacing, midY, -1f, 0f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(midX, minY - spacing, 0f, -1f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(midX, minY + spacing, 0f, 1f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(midX, maxY + spacing, 0f, 1f, y, ZoneOffsetArrowColor);
        DrawOffsetChevron(midX, maxY - spacing, 0f, -1f, y, ZoneOffsetArrowColor);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawRectPickPreview(float minX, float maxX, float minY, float maxY, float y = 9f)
    {
        var pts = new List<Vector2>
        {
            new(minX, minY),
            new(maxX, minY),
            new(maxX, maxY),
            new(minX, maxY)
        };

        DrawOutlineLoop(pts, y + 0.5f, ZonePickPreviewColor, 2.5f, depthTest: false);
    }

    public static void DrawCirclePickPreview(float cx, float cy, float radius, float y = 9f)
    {
        DrawOutlineLoop(
            FloorZoneGeometry.GetOutlinePoints(new FloorZone
            {
                Shape = WallRegionShape.Circular,
                CenterX = cx,
                CenterY = cy,
                RadiusMm = radius
            }),
            y + 0.5f,
            ZonePickPreviewColor,
            2.5f,
            depthTest: false);
    }

    public static void DrawPolygonPickPreview(
        IReadOnlyList<float> xs,
        IReadOnlyList<float> ys,
        float previewX,
        float previewY,
        float y = 9f)
    {
        int n = xs.Count;
        RenderEngine.BeginLineBatch();

        for (int i = 0; i < n - 1; i++)
            DrawEdge(xs[i], ys[i], xs[i + 1], ys[i + 1], y, ZonePickPreviewColor);

        if (n > 0)
            DrawEdge(xs[^1], ys[^1], previewX, previewY, y, ZonePickPreviewColor);

        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawZoneDragPreview(FloorZone zone, WallRegionEdgeKind edge, float previewValue, float y = 11.5f)
    {
        var preview = CloneZoneForPreview(zone, edge, previewValue);
        DrawOutlineLoop(FloorZoneGeometry.GetOutlinePoints(preview), y, ZoneDragPreviewColor, 3f, depthTest: false);
    }

    private static FloorZone CloneZoneForPreview(FloorZone zone, WallRegionEdgeKind edge, float previewValue)
    {
        var preview = new FloorZone
        {
            Shape = zone.Shape,
            MinX = zone.MinX,
            MinY = zone.MinY,
            MaxX = zone.MaxX,
            MaxY = zone.MaxY,
            CenterX = zone.CenterX,
            CenterY = zone.CenterY,
            RadiusMm = zone.RadiusMm,
            OffsetMm = zone.OffsetMm,
            OffsetEdgeStartAlongMm = zone.OffsetEdgeStartAlongMm,
            OffsetEdgeEndAlongMm = zone.OffsetEdgeEndAlongMm,
            OffsetEdgeBottomMm = zone.OffsetEdgeBottomMm,
            OffsetEdgeTopMm = zone.OffsetEdgeTopMm
        };

        preview.PolygonAlongMm.AddRange(zone.PolygonAlongMm);
        preview.PolygonHeightMm.AddRange(zone.PolygonHeightMm);

        switch (edge)
        {
            case WallRegionEdgeKind.StartAlong:
                preview.MinX = previewValue;
                break;
            case WallRegionEdgeKind.EndAlong:
                preview.MaxX = previewValue;
                break;
            case WallRegionEdgeKind.Bottom:
                preview.MinY = previewValue;
                break;
            case WallRegionEdgeKind.Top:
                preview.MaxY = previewValue;
                break;
            case WallRegionEdgeKind.Radius:
                preview.RadiusMm = previewValue;
                FloorZoneGeometry.SyncBoundingBox(preview);
                break;
        }

        return preview;
    }

    private static void DrawOutlineLoop(
        IReadOnlyList<Vector2> points,
        float y,
        Vector4 color,
        float width,
        bool depthTest)
    {
        if (points.Count < 2)
            return;

        RenderEngine.BeginLineBatch();
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            DrawEdge(points[i].X, points[i].Y, points[j].X, points[j].Y, y, color);
        }

        RenderEngine.EndLineBatch(width, depthTest: depthTest);
    }

    private static void DrawOffsetChevron(
        float x, float yPlane, float dirX, float dirY, float y, Vector4 color)
    {
        float size = OffsetArrowSizeMm;
        float wing = size * 0.45f;
        float tipX = x + dirX * size * 0.5f;
        float tipY = yPlane + dirY * size * 0.5f;
        float baseX = x - dirX * size * 0.5f;
        float baseY = yPlane - dirY * size * 0.5f;
        float perpX = dirY;
        float perpY = -dirX;

        DrawEdge(tipX, tipY, baseX + perpX * wing, baseY + perpY * wing, y, color);
        DrawEdge(tipX, tipY, baseX - perpX * wing, baseY - perpY * wing, y, color);
    }

    private static void DrawEdge(float x0, float y0, float x1, float y1, float y, Vector4 color)
    {
        RenderEngine.Line(
            new Vector3(x0, y, y0),
            new Vector3(x1, y, y1),
            color);
    }
}
