using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class WallManualDimensionService
{
    public const float DefaultOffsetMm = 280f;
    public const float LabelHeightMm = 150f;
    public const float MinArcRadiusMm = 200f;
    public const float MaxArcRadiusMm = 900f;
    public const float PickRadiusMm = 220f;
    public const float SnapRadiusMm = 150f;

    public static Vector2 SnapPoint(Vector2 point, IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return point;

        float bestDist = SnapRadiusMm;
        Vector2 best = point;

        foreach (var wall in walls)
        {
            TrySnapCandidate(wall.Start, point, ref best, ref bestDist);
            TrySnapCandidate(wall.End, point, ref best, ref bestDist);
        }

        var visuals = WallVisualBuilder.BuildWithCorners(walls);

        foreach (var visual in visuals)
        {
            TrySnapCandidate(visual.A1, point, ref best, ref bestDist);
            TrySnapCandidate(visual.A2, point, ref best, ref bestDist);
            TrySnapCandidate(visual.B1, point, ref best, ref bestDist);
            TrySnapCandidate(visual.B2, point, ref best, ref bestDist);
        }

        return best;
    }

    private static void TrySnapCandidate(Vector2 candidate, Vector2 point, ref Vector2 best, ref float bestDist)
    {
        float dist = (candidate - point).Length;

        if (dist < bestDist)
        {
            bestDist = dist;
            best = candidate;
        }
    }

    public static WallManualDimension? TryCreateLinear(Vector2 a, Vector2 b, Vector2 placementHint)
    {
        Vector2 ab = b - a;

        if (ab.LengthSquared < 1f)
            return null;

        Vector2 dir = Vector2.Normalize(ab);
        Vector2 normal = new(-dir.Y, dir.X);
        Vector2 mid = (a + b) * 0.5f;
        float offset = Vector2.Dot(placementHint - mid, normal);

        if (MathF.Abs(offset) < 80f)
            offset = DefaultOffsetMm;

        Vector2 shift = normal * offset;

        return new WallManualDimension
        {
            Kind = WallManualDimensionKind.Linear,
            PointA = a,
            PointB = b,
            DimStart = a + shift,
            DimEnd = b + shift,
            DisplayValue = ab.Length
        };
    }

    public static WallManualDimension? TryCreateAngular(Vector2 a, Vector2 vertex, Vector2 c, Vector2 placementHint)
    {
        Vector2 ba = a - vertex;
        Vector2 bc = c - vertex;

        if (ba.LengthSquared < 1f || bc.LengthSquared < 1f)
            return null;

        float angle = ComputeAngleDegrees(vertex, a, c);

        if (angle < 0.5f)
            return null;

        float radius = Math.Clamp((placementHint - vertex).Length, MinArcRadiusMm, MaxArcRadiusMm);
        Vector2 dirA = Vector2.Normalize(ba);
        Vector2 dirC = Vector2.Normalize(bc);

        return new WallManualDimension
        {
            Kind = WallManualDimensionKind.Angular,
            PointA = a,
            PointB = vertex,
            PointC = c,
            DimStart = vertex + dirA * radius,
            DimEnd = vertex + dirC * radius,
            ArcRadius = radius,
            DisplayValue = angle
        };
    }

    public static float ComputeAngleDegrees(Vector2 vertex, Vector2 a, Vector2 c)
    {
        Vector2 ba = a - vertex;
        Vector2 bc = c - vertex;

        if (ba.LengthSquared < 0.01f || bc.LengthSquared < 0.01f)
            return 0f;

        float dot = Vector2.Dot(Vector2.Normalize(ba), Vector2.Normalize(bc));
        dot = Math.Clamp(dot, -1f, 1f);
        return MathHelper.RadiansToDegrees(MathF.Acos(dot));
    }

    public static Vector3 GetLabelWorldPosition(WallManualDimension dim)
    {
        if (dim.Kind == WallManualDimensionKind.Linear)
        {
            Vector2 labelFloor = (dim.DimStart + dim.DimEnd) * 0.5f;
            return new Vector3(labelFloor.X, LabelHeightMm, labelFloor.Y);
        }

        Vector2 midDir = Vector2.Normalize((dim.DimStart - dim.PointB) + (dim.DimEnd - dim.PointB));

        if (midDir.LengthSquared < 0.01f)
            midDir = new Vector2(1f, 0f);

        Vector2 labelPos = dim.PointB + midDir * (dim.ArcRadius + 120f);
        return new Vector3(labelPos.X, LabelHeightMm, labelPos.Y);
    }

    public static string FormatLabel(WallManualDimension dim) =>
        dim.Kind == WallManualDimensionKind.Angular
            ? $"{dim.DisplayValue:0}°"
            : $"{dim.DisplayValue:0}";

    public static bool TryPick(
        Vector2 floorPoint,
        IReadOnlyList<WallManualDimension> dimensions,
        out Guid pickedId)
    {
        pickedId = Guid.Empty;

        if (dimensions.Count == 0)
            return false;

        float bestDist = PickRadiusMm;
        Guid bestId = Guid.Empty;

        foreach (var dim in dimensions)
        {
            float dist = dim.Kind == WallManualDimensionKind.Linear
                ? DistancePointToSegment(floorPoint, dim.DimStart, dim.DimEnd)
                : DistancePointToAngularDim(floorPoint, dim);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestId = dim.Id;
            }
        }

        if (bestId == Guid.Empty)
            return false;

        pickedId = bestId;
        return true;
    }

    private static float DistancePointToAngularDim(Vector2 point, WallManualDimension dim)
    {
        float toVertex = (point - dim.PointB).Length;
        float toArc = MathF.Abs(toVertex - dim.ArcRadius);
        float toLegA = DistancePointToSegment(point, dim.PointB, dim.PointA);
        float toLegC = DistancePointToSegment(point, dim.PointB, dim.PointC);
        return MathF.Min(toArc, MathF.Min(toLegA, toLegC));
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared;

        if (lenSq < 0.01f)
            return (point - a).Length;

        float t = Math.Clamp(Vector2.Dot(point - a, ab) / lenSq, 0f, 1f);
        Vector2 projection = a + ab * t;
        return (point - projection).Length;
    }
}
