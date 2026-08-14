using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Geometria efetiva de regiões do piso (retângulo, círculo, polígono, offset Promob).</summary>
public static class FloorZoneGeometry
{
    public const int CircleSegmentCount = 32;

    public static void SyncBoundingBox(FloorZone zone)
    {
        if (zone.Shape == WallRegionShape.Circular)
        {
            float r = GetEffectiveRadius(zone);
            zone.MinX = zone.CenterX - r;
            zone.MaxX = zone.CenterX + r;
            zone.MinY = zone.CenterY - r;
            zone.MaxY = zone.CenterY + r;
            return;
        }

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
        {
            zone.MinX = zone.PolygonAlongMm.Min();
            zone.MaxX = zone.PolygonAlongMm.Max();
            zone.MinY = zone.PolygonHeightMm.Min();
            zone.MaxY = zone.PolygonHeightMm.Max();
        }
    }

    public static float GetEffectiveRadius(FloorZone zone)
    {
        float r = zone.Shape == WallRegionShape.Circular
            ? zone.RadiusMm
            : MathF.Min(zone.MaxX - zone.MinX, zone.MaxY - zone.MinY) * 0.5f;

        return MathF.Max(FloorZoneService.MinSpanMm * 0.5f, r + zone.OffsetMm);
    }

    public static (float minX, float maxX, float minY, float maxY) GetEffectiveBounds(
        FloorZone zone,
        float limitMinX,
        float limitMinY,
        float limitMaxX,
        float limitMaxY)
    {
        if (zone.Shape == WallRegionShape.Circular)
        {
            float r = GetEffectiveRadius(zone);
            return (
                Math.Clamp(zone.CenterX - r, limitMinX, limitMaxX),
                Math.Clamp(zone.CenterX + r, limitMinX, limitMaxX),
                Math.Clamp(zone.CenterY - r, limitMinY, limitMaxY),
                Math.Clamp(zone.CenterY + r, limitMinY, limitMaxY));
        }

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
            SyncBoundingBox(zone);

        float oStart = zone.OffsetMm + zone.OffsetEdgeStartAlongMm;
        float oEnd = zone.OffsetMm + zone.OffsetEdgeEndAlongMm;
        float oBottom = zone.OffsetMm + zone.OffsetEdgeBottomMm;
        float oTop = zone.OffsetMm + zone.OffsetEdgeTopMm;
        float minSpan = FloorZoneService.MinSpanMm;

        float minX = Math.Clamp(zone.MinX - oStart, limitMinX, limitMaxX - minSpan);
        float maxX = Math.Clamp(zone.MaxX + oEnd, minX + minSpan, limitMaxX);
        float minY = Math.Clamp(zone.MinY - oBottom, limitMinY, limitMaxY - minSpan);
        float maxY = Math.Clamp(zone.MaxY + oTop, minY + minSpan, limitMaxY);
        return (minX, maxX, minY, maxY);
    }

    public static (float minX, float maxX, float minY, float maxY) GetBaseBounds(FloorZone zone) =>
        (zone.MinX, zone.MaxX, zone.MinY, zone.MaxY);

    public static bool ContainsPoint(FloorZone zone, Vector2 point)
    {
        if (zone.Shape == WallRegionShape.Circular)
        {
            float dx = point.X - zone.CenterX;
            float dy = point.Y - zone.CenterY;
            float r = GetEffectiveRadius(zone);
            return dx * dx + dy * dy <= r * r;
        }

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
            return WallRegionGeometry.ContainsPointInPolygonList(
                zone.PolygonAlongMm,
                zone.PolygonHeightMm,
                point.X,
                point.Y);

        var (minX, maxX, minY, maxY) = GetEffectiveBounds(
            zone, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }

    public static float DistanceToBoundary(
        FloorZone zone,
        float x,
        float y,
        float limitMinX,
        float limitMinY,
        float limitMaxX,
        float limitMaxY)
    {
        if (zone.Shape == WallRegionShape.Circular)
        {
            float dx = x - zone.CenterX;
            float dy = y - zone.CenterY;
            return MathF.Abs(MathF.Sqrt(dx * dx + dy * dy) - GetEffectiveRadius(zone));
        }

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
        {
            float best = float.MaxValue;
            int n = zone.PolygonAlongMm.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float d = DistancePointToSegment(
                    x, y,
                    zone.PolygonAlongMm[i], zone.PolygonHeightMm[i],
                    zone.PolygonAlongMm[j], zone.PolygonHeightMm[j]);
                best = MathF.Min(best, d);
            }

            return best;
        }

        var (minX, maxX, minY, maxY) = GetEffectiveBounds(
            zone, limitMinX, limitMinY, limitMaxX, limitMaxY);

        float dLeft = MathF.Abs(x - minX);
        float dRight = MathF.Abs(x - maxX);
        float dBottom = MathF.Abs(y - minY);
        float dTop = MathF.Abs(y - maxY);
        return MathF.Min(MathF.Min(dLeft, dRight), MathF.Min(dBottom, dTop));
    }

    public static List<Vector2> GetOutlinePoints(FloorZone zone)
    {
        if (zone.Shape == WallRegionShape.Circular)
            return BuildCirclePoints(zone.CenterX, zone.CenterY, GetEffectiveRadius(zone));

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
        {
            var pts = new List<Vector2>(zone.PolygonAlongMm.Count);
            for (int i = 0; i < zone.PolygonAlongMm.Count; i++)
                pts.Add(new Vector2(zone.PolygonAlongMm[i], zone.PolygonHeightMm[i]));

            return pts;
        }

        var (minX, maxX, minY, maxY) = GetEffectiveBounds(
            zone, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

        return [
            new(minX, minY),
            new(maxX, minY),
            new(maxX, maxY),
            new(minX, maxY)
        ];
    }

    public static List<Vector2> GetBaseOutlinePoints(FloorZone zone)
    {
        if (zone.Shape == WallRegionShape.Circular)
            return BuildCirclePoints(zone.CenterX, zone.CenterY, zone.RadiusMm);

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
        {
            var pts = new List<Vector2>(zone.PolygonAlongMm.Count);
            for (int i = 0; i < zone.PolygonAlongMm.Count; i++)
                pts.Add(new Vector2(zone.PolygonAlongMm[i], zone.PolygonHeightMm[i]));

            return pts;
        }

        return [
            new(zone.MinX, zone.MinY),
            new(zone.MaxX, zone.MinY),
            new(zone.MaxX, zone.MaxY),
            new(zone.MinX, zone.MaxY)
        ];
    }

    public static bool ZonesOverlap(FloorZone a, FloorZone b)
    {
        if (a.Shape == WallRegionShape.Polygon || b.Shape == WallRegionShape.Polygon)
        {
            var (pa0, pa1, pa2, pa3) = GetEffectiveBounds(a, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            var (pb0, pb1, pb2, pb3) = GetEffectiveBounds(b, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
            return pa0 < pb1 && pb0 < pa1 && pa2 < pb3 && pb2 < pa3;
        }

        if (a.Shape == WallRegionShape.Circular || b.Shape == WallRegionShape.Circular)
        {
            float ax = a.Shape == WallRegionShape.Circular ? a.CenterX : (a.MinX + a.MaxX) * 0.5f;
            float ay = a.Shape == WallRegionShape.Circular ? a.CenterY : (a.MinY + a.MaxY) * 0.5f;
            float ar = a.Shape == WallRegionShape.Circular
                ? GetEffectiveRadius(a)
                : MathF.Max(a.MaxX - a.MinX, a.MaxY - a.MinY) * 0.5f + a.OffsetMm;

            float bx = b.Shape == WallRegionShape.Circular ? b.CenterX : (b.MinX + b.MaxX) * 0.5f;
            float by = b.Shape == WallRegionShape.Circular ? b.CenterY : (b.MinY + b.MaxY) * 0.5f;
            float br = b.Shape == WallRegionShape.Circular
                ? GetEffectiveRadius(b)
                : MathF.Max(b.MaxX - b.MinX, b.MaxY - b.MinY) * 0.5f + b.OffsetMm;

            float dx = ax - bx;
            float dy = ay - by;
            float sum = ar + br;
            return dx * dx + dy * dy < sum * sum;
        }

        var (a0, a1, a2, a3) = GetEffectiveBounds(a, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
        var (b0, b1, b2, b3) = GetEffectiveBounds(b, float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
        return a0 < b1 && b0 < a1 && a2 < b3 && b2 < a3;
    }

    public static string FormatSummary(FloorZone zone)
    {
        string offset = FormatOffsetSummary(zone);

        if (zone.Shape == WallRegionShape.Circular)
            return $"{zone.Name}: círculo Ø{zone.RadiusMm * 2f:0} mm @ {zone.CenterX:0},{zone.CenterY:0}{offset}";

        if (zone.Shape == WallRegionShape.Polygon)
            return $"{zone.Name}: polígono {zone.PolygonAlongMm.Count} pts{offset}";

        return $"{zone.Name}: {zone.MinX:0}–{zone.MaxX:0} × {zone.MinY:0}–{zone.MaxY:0} mm{offset}";
    }

    private static string FormatOffsetSummary(FloorZone zone)
    {
        if (MathF.Abs(zone.OffsetMm) > 0.01f)
            return $", offset forma {zone.OffsetMm:0} mm";

        if (zone.Shape != WallRegionShape.Rectangular)
            return string.Empty;

        bool anyEdge =
            MathF.Abs(zone.OffsetEdgeStartAlongMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeEndAlongMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeBottomMm) > 0.01f ||
            MathF.Abs(zone.OffsetEdgeTopMm) > 0.01f;

        if (!anyEdge)
            return string.Empty;

        return $", offset arestas {zone.OffsetEdgeStartAlongMm:0}/{zone.OffsetEdgeEndAlongMm:0}/{zone.OffsetEdgeBottomMm:0}/{zone.OffsetEdgeTopMm:0} mm";
    }

    public static bool IsNearFirstVertex(FloorZone zone, float x, float y, float toleranceMm)
    {
        if (zone.PolygonAlongMm.Count == 0)
            return false;

        float dx = x - zone.PolygonAlongMm[0];
        float dy = y - zone.PolygonHeightMm[0];
        return dx * dx + dy * dy <= toleranceMm * toleranceMm;
    }

    private static List<Vector2> BuildCirclePoints(float cx, float cy, float radius)
    {
        var pts = new List<Vector2>(CircleSegmentCount);
        for (int i = 0; i < CircleSegmentCount; i++)
        {
            float a = i * MathF.Tau / CircleSegmentCount;
            pts.Add(new Vector2(cx + MathF.Cos(a) * radius, cy + MathF.Sin(a) * radius));
        }

        return pts;
    }

    private static float DistancePointToSegment(
        float px, float py,
        float ax, float ay,
        float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-6f)
            return MathF.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));

        float t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / lenSq, 0f, 1f);
        float qx = ax + t * dx;
        float qy = ay + t * dy;
        return MathF.Sqrt((px - qx) * (px - qx) + (py - qy) * (py - qy));
    }
}
