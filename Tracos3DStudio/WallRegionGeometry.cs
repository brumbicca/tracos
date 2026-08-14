namespace Tracos3DStudio;

/// <summary>Geometria efetiva de regiões (retângulo, círculo, polígono, offset Promob).</summary>
public static class WallRegionGeometry
{
    public const int CircleSegmentCount = 32;
    public const float CloseVertexToleranceMm = 80f;

    public static void SyncBoundingBox(WallRegion region)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float r = GetEffectiveRadius(region);
            region.StartAlongMm = region.CenterAlongMm - r;
            region.EndAlongMm = region.CenterAlongMm + r;
            region.BottomMm = region.CenterHeightMm - r;
            region.TopMm = region.CenterHeightMm + r;
            return;
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            float minAlong = region.PolygonAlongMm.Min();
            float maxAlong = region.PolygonAlongMm.Max();
            float minH = region.PolygonHeightMm.Min();
            float maxH = region.PolygonHeightMm.Max();
            region.StartAlongMm = minAlong;
            region.EndAlongMm = maxAlong;
            region.BottomMm = minH;
            region.TopMm = maxH;
        }
    }

    public static float GetEffectiveRadius(WallRegion region)
    {
        float r = region.Shape == WallRegionShape.Circular
            ? region.RadiusMm
            : MathF.Min(region.EndAlongMm - region.StartAlongMm, region.TopMm - region.BottomMm) * 0.5f;

        return MathF.Max(WallRegionService.MinSpanMm * 0.5f, r + region.OffsetMm);
    }

    public static (float startAlong, float endAlong, float bottom, float top) GetEffectiveBounds(
        WallRegion region,
        float wallLength,
        float wallTop)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float r = GetEffectiveRadius(region);
            return (
                Math.Clamp(region.CenterAlongMm - r, 0f, wallLength),
                Math.Clamp(region.CenterAlongMm + r, 0f, wallLength),
                Math.Clamp(region.CenterHeightMm - r, 0f, wallTop),
                Math.Clamp(region.CenterHeightMm + r, 0f, wallTop));
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            SyncBoundingBox(region);
        }

        float oStart = region.OffsetMm + region.OffsetEdgeStartAlongMm;
        float oEnd = region.OffsetMm + region.OffsetEdgeEndAlongMm;
        float oBottom = region.OffsetMm + region.OffsetEdgeBottomMm;
        float oTop = region.OffsetMm + region.OffsetEdgeTopMm;
        float start = Math.Clamp(region.StartAlongMm - oStart, 0f, wallLength - WallRegionService.MinSpanMm);
        float end = Math.Clamp(region.EndAlongMm + oEnd, start + WallRegionService.MinSpanMm, wallLength);
        float bottom = Math.Clamp(region.BottomMm - oBottom, 0f, wallTop - WallRegionService.MinSpanMm);
        float top = Math.Clamp(region.TopMm + oTop, bottom + WallRegionService.MinSpanMm, wallTop);
        return (start, end, bottom, top);
    }

    public static (float startAlong, float endAlong, float bottom, float top) GetBaseBounds(WallRegion region)
    {
        return (region.StartAlongMm, region.EndAlongMm, region.BottomMm, region.TopMm);
    }

    public static bool RegionsOverlap(WallRegion a, WallRegion b, float wallLength, float wallTop)
    {
        if (a.Face != b.Face)
            return false;

        if (a.Shape == WallRegionShape.Polygon || b.Shape == WallRegionShape.Polygon)
        {
            var (pa0, pa1, pa2, pa3) = GetEffectiveBounds(a, wallLength, wallTop);
            var (pb0, pb1, pb2, pb3) = GetEffectiveBounds(b, wallLength, wallTop);
            return pa0 < pb1 && pb0 < pa1 && pa2 < pb3 && pb2 < pb3;
        }

        if (a.Shape == WallRegionShape.Circular || b.Shape == WallRegionShape.Circular)
        {
            float ax = a.Shape == WallRegionShape.Circular ? a.CenterAlongMm : (a.StartAlongMm + a.EndAlongMm) * 0.5f;
            float ay = a.Shape == WallRegionShape.Circular ? a.CenterHeightMm : (a.BottomMm + a.TopMm) * 0.5f;
            float ar = a.Shape == WallRegionShape.Circular
                ? GetEffectiveRadius(a)
                : MathF.Max(a.EndAlongMm - a.StartAlongMm, a.TopMm - a.BottomMm) * 0.5f + a.OffsetMm;

            float bx = b.Shape == WallRegionShape.Circular ? b.CenterAlongMm : (b.StartAlongMm + b.EndAlongMm) * 0.5f;
            float by = b.Shape == WallRegionShape.Circular ? b.CenterHeightMm : (b.BottomMm + b.TopMm) * 0.5f;
            float br = b.Shape == WallRegionShape.Circular
                ? GetEffectiveRadius(b)
                : MathF.Max(b.EndAlongMm - b.StartAlongMm, b.TopMm - b.BottomMm) * 0.5f + b.OffsetMm;

            float dx = ax - bx;
            float dy = ay - by;
            float distSq = dx * dx + dy * dy;
            float sum = ar + br;
            return distSq < sum * sum;
        }

        if (a.Shape == WallRegionShape.Rectangular &&
            b.Shape == WallRegionShape.Rectangular &&
            (MathF.Abs(a.RotationDegrees) > 0.01f || MathF.Abs(b.RotationDegrees) > 0.01f))
        {
            return OrientedRectangularRegionsOverlap(a, b, wallLength, wallTop);
        }

        var (a0, a1, a2, a3) = GetEffectiveBounds(a, wallLength, wallTop);
        var (b0, b1, b2, b3) = GetEffectiveBounds(b, wallLength, wallTop);
        return a0 < b1 && b0 < a1 && a2 < b3 && b2 < a3;
    }

    private static bool OrientedRectangularRegionsOverlap(
        WallRegion a,
        WallRegion b,
        float wallLength,
        float wallTop)
    {
        Span<float> aAlong = stackalloc float[4];
        Span<float> aHeight = stackalloc float[4];
        GetRectCornerPoints(a, wallLength, wallTop, aAlong, aHeight);

        Span<float> bAlong = stackalloc float[4];
        Span<float> bHeight = stackalloc float[4];
        GetRectCornerPoints(b, wallLength, wallTop, bAlong, bHeight);

        for (int i = 0; i < 4; i++)
        {
            if (ContainsPoint(b, aAlong[i], aHeight[i]))
                return true;

            if (ContainsPoint(a, bAlong[i], bHeight[i]))
                return true;
        }

        for (int i = 0; i < 4; i++)
        {
            int iNext = (i + 1) % 4;

            for (int j = 0; j < 4; j++)
            {
                int jNext = (j + 1) % 4;

                if (SegmentsIntersect(
                        aAlong[i], aHeight[i], aAlong[iNext], aHeight[iNext],
                        bAlong[j], bHeight[j], bAlong[jNext], bHeight[jNext]))
                    return true;
            }
        }

        return false;
    }

    private static void GetRectCornerPoints(
        WallRegion region,
        float wallLength,
        float wallTop,
        Span<float> along,
        Span<float> height)
    {
        if (MathF.Abs(region.RotationDegrees) > 0.01f)
        {
            GetRectCorners(region, along, height);
            return;
        }

        var (start, end, bottom, top) = GetEffectiveBounds(region, wallLength, wallTop);
        along[0] = start;
        height[0] = bottom;
        along[1] = end;
        height[1] = bottom;
        along[2] = end;
        height[2] = top;
        along[3] = start;
        height[3] = top;
    }

    private static bool SegmentsIntersect(
        float ax,
        float ay,
        float bx,
        float by,
        float cx,
        float cy,
        float dx,
        float dy)
    {
        static float Cross(float ox, float oy, float px, float py, float qx, float qy) =>
            (px - ox) * (qy - oy) - (py - oy) * (qx - ox);

        float d1 = Cross(cx, cy, dx, dy, ax, ay);
        float d2 = Cross(cx, cy, dx, dy, bx, by);
        float d3 = Cross(ax, ay, bx, by, cx, cy);
        float d4 = Cross(ax, ay, bx, by, dx, dy);

        if (((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) &&
            ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f)))
            return true;

        const float eps = 0.01f;

        if (MathF.Abs(d1) < eps && PointOnSegment(ax, ay, cx, cy, dx, dy))
            return true;

        if (MathF.Abs(d2) < eps && PointOnSegment(bx, by, cx, cy, dx, dy))
            return true;

        if (MathF.Abs(d3) < eps && PointOnSegment(cx, cy, ax, ay, bx, by))
            return true;

        if (MathF.Abs(d4) < eps && PointOnSegment(dx, dy, ax, ay, bx, by))
            return true;

        return false;
    }

    private static bool PointOnSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        return px >= MathF.Min(ax, bx) - 0.01f &&
               px <= MathF.Max(ax, bx) + 0.01f &&
               py >= MathF.Min(ay, by) - 0.01f &&
               py <= MathF.Max(ay, by) + 0.01f;
    }

    public static bool ContainsPoint(WallRegion region, float along, float height)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float dx = along - region.CenterAlongMm;
            float dy = height - region.CenterHeightMm;
            float r = GetEffectiveRadius(region);
            return dx * dx + dy * dy <= r * r;
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
            return ContainsPointInPolygonList(region.PolygonAlongMm, region.PolygonHeightMm, along, height);

        if (region.Shape == WallRegionShape.Rectangular && MathF.Abs(region.RotationDegrees) > 0.01f)
            return ContainsPointInRotatedRect(region, along, height);

        var (start, end, bottom, top) = GetEffectiveBounds(region, float.MaxValue, float.MaxValue);
        return along >= start && along <= end && height >= bottom && height <= top;
    }

    public static float DistanceToBoundary(WallRegion region, float along, float height, float wallLength, float wallTop)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float dx = along - region.CenterAlongMm;
            float dy = height - region.CenterHeightMm;
            return MathF.Abs(MathF.Sqrt(dx * dx + dy * dy) - GetEffectiveRadius(region));
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            float best = float.MaxValue;
            int n = region.PolygonAlongMm.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float d = DistancePointToSegment(
                    along, height,
                    region.PolygonAlongMm[i], region.PolygonHeightMm[i],
                    region.PolygonAlongMm[j], region.PolygonHeightMm[j]);
                best = MathF.Min(best, d);
            }

            return best;
        }

        if (region.Shape == WallRegionShape.Rectangular && MathF.Abs(region.RotationDegrees) > 0.01f)
        {
            Span<float> cornersAlong = stackalloc float[4];
            Span<float> cornersHeight = stackalloc float[4];
            GetRectCorners(region, cornersAlong, cornersHeight);
            float best = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                float d = DistancePointToSegment(
                    along, height,
                    cornersAlong[i], cornersHeight[i],
                    cornersAlong[j], cornersHeight[j]);
                best = MathF.Min(best, d);
            }

            return best;
        }

        var (start, end, bottom, top) = GetEffectiveBounds(region, wallLength, wallTop);
        float dLeft = MathF.Abs(along - start);
        float dRight = MathF.Abs(along - end);
        float dBottom = MathF.Abs(height - bottom);
        float dTop = MathF.Abs(height - top);
        return MathF.Min(MathF.Min(dLeft, dRight), MathF.Min(dBottom, dTop));
    }

    public static string FormatSummary(WallRegion region)
    {
        string face = region.Face == FaceType.Internal ? "interna" : "externa";
        string name = region.Name ?? "Região";
        string offset = FormatOffsetSummary(region);
        string rotation = FormatRotationSummary(region);

        if (region.Shape == WallRegionShape.Circular)
        {
            return $"{name} ({face}): círculo Ø{region.RadiusMm * 2f:0} mm @ {region.CenterAlongMm:0},{region.CenterHeightMm:0}{offset}";
        }

        if (region.Shape == WallRegionShape.Polygon)
        {
            int n = region.PolygonAlongMm.Count;
            return $"{name} ({face}): polígono {n} pts{offset}";
        }

        return $"{name} ({face}): {region.StartAlongMm:0}–{region.EndAlongMm:0} × {region.BottomMm:0}–{region.TopMm:0} mm{rotation}{offset}";
    }

    private static string FormatRotationSummary(WallRegion region)
    {
        if (region.Shape == WallRegionShape.Circular || MathF.Abs(region.RotationDegrees) < 0.01f)
            return string.Empty;

        return $", rotação {region.RotationDegrees:0}°";
    }

    private static string FormatOffsetSummary(WallRegion region)
    {
        if (MathF.Abs(region.OffsetMm) > 0.01f)
            return $", offset forma {region.OffsetMm:0} mm";

        if (region.Shape != WallRegionShape.Rectangular)
            return string.Empty;

        bool anyEdge =
            MathF.Abs(region.OffsetEdgeStartAlongMm) > 0.01f ||
            MathF.Abs(region.OffsetEdgeEndAlongMm) > 0.01f ||
            MathF.Abs(region.OffsetEdgeBottomMm) > 0.01f ||
            MathF.Abs(region.OffsetEdgeTopMm) > 0.01f;

        if (!anyEdge)
            return string.Empty;

        return $", offset arestas {region.OffsetEdgeStartAlongMm:0}/{region.OffsetEdgeEndAlongMm:0}/{region.OffsetEdgeBottomMm:0}/{region.OffsetEdgeTopMm:0} mm";
    }

    public static float ComputeSignedArea(ReadOnlySpan<float> along, ReadOnlySpan<float> height)
    {
        float area = 0f;
        int n = along.Length;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += along[i] * height[j] - along[j] * height[i];
        }

        return area * 0.5f;
    }

    public static bool IsNearFirstPolygonVertex(
        IReadOnlyList<float> along,
        IReadOnlyList<float> height,
        float px,
        float py,
        float toleranceMm)
    {
        if (along.Count == 0)
            return false;

        float dx = px - along[0];
        float dy = py - height[0];
        return dx * dx + dy * dy <= toleranceMm * toleranceMm;
    }

    public static bool IsNearFirstVertex(WallRegion region, float along, float height, float toleranceMm)
    {
        if (region.PolygonAlongMm.Count == 0)
            return false;

        float dx = along - region.PolygonAlongMm[0];
        float dy = height - region.PolygonHeightMm[0];
        return dx * dx + dy * dy <= toleranceMm * toleranceMm;
    }

    public static bool TryFindPolygonEdgeForVertexInsert(
        WallRegion region,
        float along,
        float height,
        float toleranceMm,
        float minVertexSpacingMm,
        out int edgeStartIndex,
        out float insertAlong,
        out float insertHeight)
    {
        edgeStartIndex = -1;
        insertAlong = 0f;
        insertHeight = 0f;

        if (region.Shape != WallRegionShape.Polygon || region.PolygonAlongMm.Count < 3)
            return false;

        int n = region.PolygonAlongMm.Count;
        float bestDist = toleranceMm;
        float minSpacingSq = minVertexSpacingMm * minVertexSpacingMm;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            float ax = region.PolygonAlongMm[i];
            float ay = region.PolygonHeightMm[i];
            float bx = region.PolygonAlongMm[j];
            float by = region.PolygonHeightMm[j];

            if (!TryProjectPointOnSegment(along, height, ax, ay, bx, by, out float t, out float qx, out float qy, out float dist))
                continue;

            if (dist >= bestDist)
                continue;

            float distAi = (qx - ax) * (qx - ax) + (qy - ay) * (qy - ay);
            float distBi = (qx - bx) * (qx - bx) + (qy - by) * (qy - by);
            if (distAi < minSpacingSq || distBi < minSpacingSq)
                continue;

            if (t <= 0.02f || t >= 0.98f)
                continue;

            bestDist = dist;
            edgeStartIndex = i;
            insertAlong = qx;
            insertHeight = qy;
        }

        return edgeStartIndex >= 0;
    }

    public static (float deltaAlong, float deltaHeight) ClampMoveDelta(
        WallRegion region,
        float wallLength,
        float wallTop,
        float deltaAlong,
        float deltaHeight)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float radius = GetEffectiveRadius(region);
            float newAlong = Math.Clamp(
                region.CenterAlongMm + deltaAlong,
                radius,
                MathF.Max(radius, wallLength - radius));
            float newHeight = Math.Clamp(
                region.CenterHeightMm + deltaHeight,
                radius,
                MathF.Max(radius, wallTop - radius));
            return (newAlong - region.CenterAlongMm, newHeight - region.CenterHeightMm);
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            float minAlong = region.PolygonAlongMm.Min();
            float maxAlong = region.PolygonAlongMm.Max();
            float minHeight = region.PolygonHeightMm.Min();
            float maxHeight = region.PolygonHeightMm.Max();
            float clampedAlong = Math.Clamp(deltaAlong, -minAlong, wallLength - maxAlong);
            float clampedHeight = Math.Clamp(deltaHeight, -minHeight, wallTop - maxHeight);
            return (clampedAlong, clampedHeight);
        }

        var (start, end, bottom, top) = GetEffectiveBounds(region, wallLength, wallTop);
        float clampedDeltaAlong = Math.Clamp(deltaAlong, -start, wallLength - end);
        float clampedDeltaHeight = Math.Clamp(deltaHeight, -bottom, wallTop - top);
        return (clampedDeltaAlong, clampedDeltaHeight);
    }

    public static void ApplyMoveDelta(WallRegion region, float deltaAlong, float deltaHeight)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            region.CenterAlongMm += deltaAlong;
            region.CenterHeightMm += deltaHeight;
            SyncBoundingBox(region);
            return;
        }

        if (region.Shape == WallRegionShape.Polygon)
        {
            for (int i = 0; i < region.PolygonAlongMm.Count; i++)
            {
                region.PolygonAlongMm[i] += deltaAlong;
                region.PolygonHeightMm[i] += deltaHeight;
            }

            SyncBoundingBox(region);
            return;
        }

        region.StartAlongMm += deltaAlong;
        region.EndAlongMm += deltaAlong;
        region.BottomMm += deltaHeight;
        region.TopMm += deltaHeight;
    }

    public static WallRegionMoveSnapshot CaptureMoveSnapshot(WallRegion region) =>
        WallRegionMoveSnapshot.From(region);

    public static bool TryProjectPointOnSegment(
        float px,
        float py,
        float ax,
        float ay,
        float bx,
        float by,
        out float t,
        out float qx,
        out float qy,
        out float distance)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float lenSq = dx * dx + dy * dy;

        if (lenSq < 1e-6f)
        {
            t = 0f;
            qx = ax;
            qy = ay;
            distance = MathF.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));
            return false;
        }

        t = Math.Clamp(((px - ax) * dx + (py - ay) * dy) / lenSq, 0f, 1f);
        qx = ax + t * dx;
        qy = ay + t * dy;
        distance = MathF.Sqrt((px - qx) * (px - qx) + (py - qy) * (py - qy));
        return true;
    }

    public static List<(float along, float height)> TriangulatePolygon(ReadOnlySpan<float> along, ReadOnlySpan<float> height)
    {
        int n = along.Length;
        if (n < 3)
            return [];

        var indices = new List<int>();
        for (int i = 0; i < n; i++)
            indices.Add(i);

        var triangles = new List<(float, float)>();
        int guard = 0;

        while (indices.Count > 3 && guard++ < n * 3)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int i0 = indices[(i - 1 + indices.Count) % indices.Count];
                int i1 = indices[i];
                int i2 = indices[(i + 1) % indices.Count];

                if (!IsConvexEar(along, height, i0, i1, i2, indices))
                    continue;

                triangles.Add((along[i0], height[i0]));
                triangles.Add((along[i1], height[i1]));
                triangles.Add((along[i2], height[i2]));
                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
                break;
        }

        if (indices.Count == 3)
        {
            triangles.Add((along[indices[0]], height[indices[0]]));
            triangles.Add((along[indices[1]], height[indices[1]]));
            triangles.Add((along[indices[2]], height[indices[2]]));
        }

        return triangles;
    }

    private static bool IsConvexEar(
        ReadOnlySpan<float> along,
        ReadOnlySpan<float> height,
        int i0,
        int i1,
        int i2,
        List<int> indices)
    {
        float cross = Cross(
            along[i1] - along[i0], height[i1] - height[i0],
            along[i2] - along[i1], height[i2] - height[i1]);

        if (cross <= 0f)
            return false;

        float ax = along[i0];
        float ay = height[i0];
        float bx = along[i1];
        float by = height[i1];
        float cx = along[i2];
        float cy = height[i2];

        foreach (int k in indices)
        {
            if (k == i0 || k == i1 || k == i2)
                continue;

            if (PointInTriangle(along[k], height[k], ax, ay, bx, by, cx, cy))
                return false;
        }

        return true;
    }

    public static bool ContainsPointInPolygonList(
        IReadOnlyList<float> along,
        IReadOnlyList<float> height,
        float px,
        float py)
    {
        bool inside = false;
        int n = along.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = along[i];
            float yi = height[i];
            float xj = along[j];
            float yj = height[j];

            if (((yi > py) != (yj > py)) &&
                (px < (xj - xi) * (py - yi) / (yj - yi + 1e-6f) + xi))
                inside = !inside;
        }

        return inside;
    }

    private static bool ContainsPointInPolygon(List<float> along, List<float> height, float px, float py) =>
        ContainsPointInPolygonList(along, height, px, py);

    private static bool PointInTriangle(
        float px, float py,
        float ax, float ay,
        float bx, float by,
        float cx, float cy)
    {
        float d1 = Sign(px, py, ax, ay, bx, by);
        float d2 = Sign(px, py, bx, by, cx, cy);
        float d3 = Sign(px, py, cx, cy, ax, ay);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !hasNeg || !hasPos;
    }

    private static float Sign(float px, float py, float ax, float ay, float bx, float by) =>
        (px - bx) * (ay - by) - (ax - bx) * (py - by);

    private static float Cross(float ax, float ay, float bx, float by) => ax * by - ay * bx;

    private static float DistancePointToSegment(
        float px, float py,
        float ax, float ay,
        float bx, float by)
    {
        TryProjectPointOnSegment(px, py, ax, ay, bx, by, out _, out _, out _, out float distance);
        return distance;
    }

    public const float RotationHandleOffsetMm = 100f;
    public const float RotationHandlePickToleranceMm = 100f;
    public const float RotationSnapDegrees = 5f;

    public static void GetRectCenter(WallRegion region, out float cx, out float cy)
    {
        cx = (region.StartAlongMm + region.EndAlongMm) * 0.5f;
        cy = (region.BottomMm + region.TopMm) * 0.5f;
    }

    public static void GetPolygonCentroid(WallRegion region, out float cx, out float cy)
    {
        if (region.PolygonAlongMm.Count == 0)
        {
            cx = cy = 0f;
            return;
        }

        cx = region.PolygonAlongMm.Average();
        cy = region.PolygonHeightMm.Average();
    }

    public static void GetRotationCenter(WallRegion region, out float cx, out float cy)
    {
        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
            GetPolygonCentroid(region, out cx, out cy);
        else
            GetRectCenter(region, out cx, out cy);
    }

    public static void GetRectCorners(WallRegion region, Span<float> along, Span<float> height)
    {
        GetRectCenter(region, out float cx, out float cy);
        float hw = (region.EndAlongMm - region.StartAlongMm) * 0.5f;
        float hh = (region.TopMm - region.BottomMm) * 0.5f;
        float rad = region.RotationDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        ReadOnlySpan<(float lx, float ly)> local =
        [
            (-hw, -hh),
            (hw, -hh),
            (hw, hh),
            (-hw, hh)
        ];

        for (int i = 0; i < 4; i++)
        {
            along[i] = cx + local[i].lx * cos - local[i].ly * sin;
            height[i] = cy + local[i].lx * sin + local[i].ly * cos;
        }
    }

    public static bool ContainsPointInRotatedRect(WallRegion region, float along, float height)
    {
        GetRectCenter(region, out float cx, out float cy);
        float rad = -region.RotationDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        float lx = along - cx;
        float ly = height - cy;
        float localAlong = lx * cos - ly * sin;
        float localHeight = lx * sin + ly * cos;
        float hw = (region.EndAlongMm - region.StartAlongMm) * 0.5f;
        float hh = (region.TopMm - region.BottomMm) * 0.5f;
        return MathF.Abs(localAlong) <= hw + 0.01f && MathF.Abs(localHeight) <= hh + 0.01f;
    }

    public static float GetAngleDegreesFromCenter(WallRegion region, float along, float height)
    {
        GetRotationCenter(region, out float cx, out float cy);
        return MathF.Atan2(height - cy, along - cx) * 180f / MathF.PI;
    }

    public static void GetRotationHandlePosition(
        WallRegion region,
        float wallLength,
        float wallTop,
        out float handleAlong,
        out float handleHeight)
    {
        GetRotationCenter(region, out float cx, out float cy);

        if (region.Shape == WallRegionShape.Rectangular && MathF.Abs(region.RotationDegrees) > 0.01f)
        {
            Span<float> cornersAlong = stackalloc float[4];
            Span<float> cornersHeight = stackalloc float[4];
            GetRectCorners(region, cornersAlong, cornersHeight);

            float bestHeight = cornersHeight[0];
            int bestIndex = 0;
            for (int i = 1; i < 4; i++)
            {
                if (cornersHeight[i] > bestHeight)
                {
                    bestHeight = cornersHeight[i];
                    bestIndex = i;
                }
            }

            int prev = (bestIndex + 3) % 4;
            int next = (bestIndex + 1) % 4;
            float edgeAlong = cornersAlong[next] - cornersAlong[prev];
            float edgeHeight = cornersHeight[next] - cornersHeight[prev];
            float len = MathF.Sqrt(edgeAlong * edgeAlong + edgeHeight * edgeHeight);

            if (len > 0.01f)
            {
                float nx = -edgeHeight / len;
                float ny = edgeAlong / len;
                handleAlong = cornersAlong[bestIndex] + nx * RotationHandleOffsetMm;
                handleHeight = cornersHeight[bestIndex] + ny * RotationHandleOffsetMm;
                return;
            }
        }

        var (_, _, _, top) = GetEffectiveBounds(region, wallLength, wallTop);
        handleAlong = cx;
        handleHeight = top + RotationHandleOffsetMm;
    }

    public static bool TryPickRotationHandle(
        WallRegion region,
        float along,
        float height,
        float wallLength,
        float wallTop,
        float toleranceMm)
    {
        if (region.Shape == WallRegionShape.Circular)
            return false;

        GetRotationHandlePosition(region, wallLength, wallTop, out float hx, out float hy);
        float dx = along - hx;
        float dy = height - hy;
        return dx * dx + dy * dy <= toleranceMm * toleranceMm;
    }

    public static float NormalizeDegrees(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
            degrees += 360f;
        return degrees;
    }

    public static float SnapRotationDegrees(float degrees) =>
        MathF.Round(degrees / RotationSnapDegrees) * RotationSnapDegrees;

    public static void ApplyRotationDegrees(WallRegion region, float rotationDegrees)
    {
        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            float target = SnapRotationDegrees(NormalizeDegrees(rotationDegrees));
            float current = region.RotationDegrees;
            float delta = target - current;
            RotatePolygonVertices(region, delta);
            region.RotationDegrees = 0f;
            return;
        }

        region.RotationDegrees = SnapRotationDegrees(NormalizeDegrees(rotationDegrees));
    }

    public static void RotatePolygonVertices(WallRegion region, float deltaDegrees)
    {
        if (region.PolygonAlongMm.Count < 3 || MathF.Abs(deltaDegrees) < 0.01f)
            return;

        GetPolygonCentroid(region, out float cx, out float cy);
        float rad = deltaDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        for (int i = 0; i < region.PolygonAlongMm.Count; i++)
        {
            float lx = region.PolygonAlongMm[i] - cx;
            float ly = region.PolygonHeightMm[i] - cy;
            region.PolygonAlongMm[i] = cx + lx * cos - ly * sin;
            region.PolygonHeightMm[i] = cy + lx * sin + ly * cos;
        }

        SyncBoundingBox(region);
    }

    public static void ApplyRotationDelta(WallRegion region, float deltaDegrees)
    {
        if (region.Shape == WallRegionShape.Circular)
            return;

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            RotatePolygonVertices(region, deltaDegrees);
            return;
        }

        region.RotationDegrees = SnapRotationDegrees(NormalizeDegrees(region.RotationDegrees + deltaDegrees));
    }

    public static bool TryGetVerticalCutAlongRange(
        WallRegion region,
        float wallLength,
        float wallTop,
        out float minAlongMm,
        out float maxAlongMm)
    {
        minAlongMm = maxAlongMm = 0f;

        if (region.Shape == WallRegionShape.Circular)
            return false;

        if (region.Shape == WallRegionShape.Rectangular && MathF.Abs(region.RotationDegrees) < 0.01f)
        {
            var (start, end, _, _) = GetEffectiveBounds(region, wallLength, wallTop);
            minAlongMm = start + WallRegionService.MinSpanMm;
            maxAlongMm = end - WallRegionService.MinSpanMm;
            return maxAlongMm > minAlongMm + 0.01f;
        }

        CopyRegionOutlineVertices(region, wallLength, wallTop, out float[] along, out float[] height);

        if (along.Length < 3)
            return false;

        minAlongMm = along.Min() + WallRegionService.MinSpanMm;
        maxAlongMm = along.Max() - WallRegionService.MinSpanMm;
        return maxAlongMm > minAlongMm + 0.01f;
    }

    public static float ClampVerticalCutAlong(
        WallRegion region,
        float wallLength,
        float wallTop,
        float cutAlongMm)
    {
        if (!TryGetVerticalCutAlongRange(region, wallLength, wallTop, out float minAlong, out float maxAlong))
            return cutAlongMm;

        return Math.Clamp(MathF.Round(cutAlongMm / 10f) * 10f, minAlong, maxAlong);
    }

    public static void CopyRegionOutlineVertices(
        WallRegion region,
        float wallLength,
        float wallTop,
        out float[] along,
        out float[] height)
    {
        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            along = region.PolygonAlongMm.ToArray();
            height = region.PolygonHeightMm.ToArray();
            return;
        }

        Span<float> cornersAlong = stackalloc float[4];
        Span<float> cornersHeight = stackalloc float[4];
        GetRectCorners(region, cornersAlong, cornersHeight);
        along = cornersAlong.ToArray();
        height = cornersHeight.ToArray();
    }

    public static void GetVerticalCutLineSpan(
        WallRegion region,
        float wallLength,
        float wallTop,
        float cutAlongMm,
        out float bottomMm,
        out float topMm)
    {
        CopyRegionOutlineVertices(region, wallLength, wallTop, out float[] along, out float[] height);
        bottomMm = height.Min();
        topMm = height.Max();

        for (int i = 0; i < along.Length; i++)
        {
            int j = (i + 1) % along.Length;
            float ax = along[i];
            float ay = height[i];
            float bx = along[j];
            float by = height[j];

            if (MathF.Abs(ax - cutAlongMm) < 0.01f)
            {
                bottomMm = MathF.Min(bottomMm, ay);
                topMm = MathF.Max(topMm, ay);
            }

            if (MathF.Abs(bx - cutAlongMm) < 0.01f)
            {
                bottomMm = MathF.Min(bottomMm, by);
                topMm = MathF.Max(topMm, by);
            }

            if ((ax - cutAlongMm) * (bx - cutAlongMm) < 0f)
            {
                float t = (cutAlongMm - ax) / (bx - ax);
                float y = ay + t * (by - ay);
                bottomMm = MathF.Min(bottomMm, y);
                topMm = MathF.Max(topMm, y);
            }
        }
    }

    public static bool TryBuildVerticalCutParts(
        WallRegion source,
        float wallLength,
        float wallTop,
        float cutAlongMm,
        out WallRegion leftPart,
        out WallRegion rightPart,
        out string? error)
    {
        leftPart = null!;
        rightPart = null!;
        error = null;

        if (source.Shape == WallRegionShape.Circular)
        {
            error = "Região circular não pode ser cortada.";
            return false;
        }

        cutAlongMm = ClampVerticalCutAlong(source, wallLength, wallTop, cutAlongMm);

        if (source.Shape == WallRegionShape.Rectangular && MathF.Abs(source.RotationDegrees) < 0.01f)
        {
            var (start, end, bottom, top) = GetEffectiveBounds(source, wallLength, wallTop);

            if (cutAlongMm <= start + WallRegionService.MinSpanMm ||
                cutAlongMm >= end - WallRegionService.MinSpanMm)
            {
                error = $"Posição do corte deve deixar pelo menos {WallRegionService.MinSpanMm:0} mm em cada lado.";
                return false;
            }

            leftPart = CreateSplitRegionPart(source, start, cutAlongMm, bottom, top, WallRegionShape.Rectangular);
            rightPart = CreateSplitRegionPart(source, cutAlongMm, end, bottom, top, WallRegionShape.Rectangular);
            return true;
        }

        CopyRegionOutlineVertices(source, wallLength, wallTop, out float[] along, out float[] height);

        var leftAlong = new List<float>();
        var leftHeight = new List<float>();
        var rightAlong = new List<float>();
        var rightHeight = new List<float>();

        if (!TryClipPolygonVertical(along, height, cutAlongMm, keepLeft: true, leftAlong, leftHeight) ||
            !TryClipPolygonVertical(along, height, cutAlongMm, keepLeft: false, rightAlong, rightHeight))
        {
            error = "Não foi possível calcular as partes do corte.";
            return false;
        }

        if (!TryCreatePolygonPart(source, leftAlong, leftHeight, out leftPart, out error))
            return false;

        if (!TryCreatePolygonPart(source, rightAlong, rightHeight, out rightPart, out error))
            return false;

        return true;
    }

    private static WallRegion CreateSplitRegionPart(
        WallRegion source,
        float startAlongMm,
        float endAlongMm,
        float bottomMm,
        float topMm,
        WallRegionShape shape)
    {
        return new WallRegion
        {
            Shape = shape,
            Face = source.Face,
            StartAlongMm = startAlongMm,
            EndAlongMm = endAlongMm,
            BottomMm = bottomMm,
            TopMm = topMm,
            MaterialId = source.MaterialId,
            OffsetMm = source.OffsetMm,
            RotationDegrees = shape == WallRegionShape.Rectangular ? source.RotationDegrees : 0f
        };
    }

    private static bool TryCreatePolygonPart(
        WallRegion source,
        List<float> along,
        List<float> height,
        out WallRegion part,
        out string? error)
    {
        part = null!;
        error = null;

        if (along.Count < WallRegionService.MinPolygonVertices)
        {
            error = "Parte do corte muito pequena.";
            return false;
        }

        float area = MathF.Abs(ComputeSignedArea(along.ToArray(), height.ToArray()));

        if (area < WallRegionService.MinPolygonAreaMm2)
        {
            error = "Parte do corte muito pequena.";
            return false;
        }

        part = new WallRegion
        {
            Shape = WallRegionShape.Polygon,
            Face = source.Face,
            MaterialId = source.MaterialId,
            OffsetMm = source.OffsetMm
        };

        part.PolygonAlongMm.AddRange(along);
        part.PolygonHeightMm.AddRange(height);
        SyncBoundingBox(part);
        return true;
    }

    private static bool TryClipPolygonVertical(
        ReadOnlySpan<float> inAlong,
        ReadOnlySpan<float> inHeight,
        float cutAlongMm,
        bool keepLeft,
        List<float> outAlong,
        List<float> outHeight)
    {
        outAlong.Clear();
        outHeight.Clear();

        int n = inAlong.Length;

        if (n < 3)
            return false;

        for (int i = 0; i < n; i++)
        {
            float ax = inAlong[i];
            float ay = inHeight[i];
            float bx = inAlong[(i + 1) % n];
            float by = inHeight[(i + 1) % n];
            bool aInside = keepLeft ? ax <= cutAlongMm + 0.01f : ax >= cutAlongMm - 0.01f;
            bool bInside = keepLeft ? bx <= cutAlongMm + 0.01f : bx >= cutAlongMm - 0.01f;

            if (aInside)
            {
                outAlong.Add(ax);
                outHeight.Add(ay);
            }

            if (aInside == bInside)
                continue;

            float dx = bx - ax;

            if (MathF.Abs(dx) < 0.001f)
            {
                if (MathF.Abs(ax - cutAlongMm) < 0.01f)
                {
                    outAlong.Add(cutAlongMm);
                    outHeight.Add(ay);
                }

                continue;
            }

            float t = (cutAlongMm - ax) / dx;
            t = Math.Clamp(t, 0f, 1f);
            outAlong.Add(ax + t * dx);
            outHeight.Add(ay + t * (by - ay));
        }

        return outAlong.Count >= WallRegionService.MinPolygonVertices;
    }
}
