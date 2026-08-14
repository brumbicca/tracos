using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class VisualWallSegment
{
    public WallSegment Wall { get; init; } = null!;

    public Vector2 A1 { get; set; }

    public Vector2 A2 { get; set; }

    public Vector2 B1 { get; set; }

    public Vector2 B2 { get; set; }

    public List<Vector2>? TessellatedFaceA { get; init; }

    public List<Vector2>? TessellatedFaceB { get; init; }

    public bool IsCurved => TessellatedFaceA is { Count: > 2 };
}

public static class WallVisualBuilder
{
    public static List<VisualWallSegment> BuildWithCorners(IReadOnlyList<WallSegment> walls)
    {
        var result = new List<VisualWallSegment>();

        if (walls.Count == 0)
            return result;

        var raw = new List<VisualWallSegment>();

        foreach (var wall in walls)
            raw.Add(CreateRaw(wall));

        bool closed = walls.Count >= 3 && Geometry2D.AlmostEqual(walls[0].Start, walls[^1].End, 20f);

        for (int i = 0; i < raw.Count; i++)
        {
            var current = raw[i];

            if (current.IsCurved)
            {
                result.Add(current);
                continue;
            }

            int prevIndex = i - 1;
            int nextIndex = i + 1;

            bool hasPrev = prevIndex >= 0 || closed;
            bool hasNext = nextIndex < raw.Count || closed;

            if (closed && prevIndex < 0)
                prevIndex = raw.Count - 1;

            if (closed && nextIndex >= raw.Count)
                nextIndex = 0;

            if (hasPrev)
            {
                var previous = raw[prevIndex];

                if (!previous.IsCurved &&
                    TryLineIntersection(previous.A1, previous.A2, current.A1, current.A2, out var intersectionA))
                    current.A1 = intersectionA;

                if (!previous.IsCurved &&
                    TryLineIntersection(previous.B1, previous.B2, current.B1, current.B2, out var intersectionB))
                    current.B1 = intersectionB;
            }

            if (hasNext)
            {
                var next = raw[nextIndex];

                if (!next.IsCurved &&
                    TryLineIntersection(current.A1, current.A2, next.A1, next.A2, out var intersectionA))
                    current.A2 = intersectionA;

                if (!next.IsCurved &&
                    TryLineIntersection(current.B1, current.B2, next.B1, next.B2, out var intersectionB))
                    current.B2 = intersectionB;
            }

            result.Add(current);
        }

        ApplyInnerFaceCornerMiters(result, walls, closed);

        return result;
    }

    /// <summary>Chanfro nos encontros em que a face interna alterna entre A e B (cadeia aberta/fechada).</summary>
    private static void ApplyInnerFaceCornerMiters(
        List<VisualWallSegment> segments,
        IReadOnlyList<WallSegment> walls,
        bool closed)
    {
        if (segments.Count < 2)
            return;

        for (int i = 0; i < segments.Count; i++)
        {
            int prevIndex = i - 1;

            if (prevIndex < 0)
            {
                if (!closed)
                    continue;

                prevIndex = segments.Count - 1;
            }

            var previous = segments[prevIndex];
            var current = segments[i];

            bool prevInnerA = WallInnerFaceService.UsesInnerFaceA(previous, walls);
            bool currInnerA = WallInnerFaceService.UsesInnerFaceA(current, walls);

            if (prevInnerA == currInnerA || previous.IsCurved || current.IsCurved)
                continue;

            Vector2 prevInnerStart = prevInnerA ? previous.A1 : previous.B1;
            Vector2 prevInnerEnd = prevInnerA ? previous.A2 : previous.B2;
            Vector2 currInnerStart = currInnerA ? current.A1 : current.B1;
            Vector2 currInnerEnd = currInnerA ? current.A2 : current.B2;

            if (!TryLineIntersection(prevInnerStart, prevInnerEnd, currInnerStart, currInnerEnd, out var innerCorner))
                continue;

            if (prevInnerA)
                previous.A2 = innerCorner;
            else
                previous.B2 = innerCorner;

            if (currInnerA)
                current.A1 = innerCorner;
            else
                current.B1 = innerCorner;
        }
    }

    private static VisualWallSegment CreateRaw(WallSegment wall)
    {
        if (MathF.Abs(wall.FlechaMm) > WallArcGeometry.StraightToleranceMm)
        {
            var arc = WallArcGeometry.FromWall(wall);
            var (faceA, faceB) = arc.BuildFacePolylines(wall.Thickness, wall.Orientation);

            ApplyEndpointChamfersOnPolyline(wall, faceA, faceB);

            return new VisualWallSegment
            {
                Wall = wall,
                A1 = faceA[0],
                A2 = faceA[^1],
                B1 = faceB[0],
                B2 = faceB[^1],
                TessellatedFaceA = faceA,
                TessellatedFaceB = faceB
            };
        }

        Vector2 a1;
        Vector2 a2;
        Vector2 b1;
        Vector2 b2;

        switch (wall.Orientation)
        {
            case WallOrientation.Left:
                a1 = wall.Start;
                a2 = wall.End;
                b1 = wall.Start + wall.RightNormal * wall.Thickness;
                b2 = wall.End + wall.RightNormal * wall.Thickness;
                break;

            case WallOrientation.Center:
                a1 = wall.Start + wall.RightNormal * (wall.Thickness / 2f);
                a2 = wall.End + wall.RightNormal * (wall.Thickness / 2f);
                b1 = wall.Start + wall.LeftNormal * (wall.Thickness / 2f);
                b2 = wall.End + wall.LeftNormal * (wall.Thickness / 2f);
                break;

            default:
                a1 = wall.Start;
                a2 = wall.End;
                b1 = wall.Start + wall.LeftNormal * wall.Thickness;
                b2 = wall.End + wall.LeftNormal * wall.Thickness;
                break;
        }

        ApplyEndpointChamfers(wall, ref a1, ref a2, ref b1, ref b2);

        return new VisualWallSegment
        {
            Wall = wall,
            A1 = a1,
            A2 = a2,
            B1 = b1,
            B2 = b2
        };
    }

    private static void ApplyEndpointChamfersOnPolyline(
        WallSegment wall,
        List<Vector2> faceA,
        List<Vector2> faceB)
    {
        if (faceA.Count < 2 || wall.Length < 1f)
            return;

        if (wall.ChamferStartMm > 0f)
        {
            float cut = MathF.Min(wall.ChamferStartMm, wall.Length - 1f);
            var arc = WallArcGeometry.FromWall(wall);
            Vector2 startTangent = arc.GetTangentAtArcLength(0f);
            faceA[0] += startTangent * cut;
            faceB[0] += startTangent * cut;
        }

        if (wall.ChamferEndMm > 0f)
        {
            float cut = MathF.Min(wall.ChamferEndMm, wall.Length - 1f);
            var arc = WallArcGeometry.FromWall(wall);
            Vector2 endTangent = arc.GetTangentAtArcLength(arc.ArcLength);
            faceA[^1] -= endTangent * cut;
            faceB[^1] -= endTangent * cut;
        }
    }

    private static void ApplyEndpointChamfers(
        WallSegment wall,
        ref Vector2 a1,
        ref Vector2 a2,
        ref Vector2 b1,
        ref Vector2 b2)
    {
        if (wall.Length < 1f)
            return;

        Vector2 dir = wall.Direction;

        if (wall.ChamferStartMm > 0f)
        {
            float cut = MathF.Min(wall.ChamferStartMm, wall.Length - 1f);
            a1 += dir * cut;
            b1 += dir * cut;
        }

        if (wall.ChamferEndMm > 0f)
        {
            float cut = MathF.Min(wall.ChamferEndMm, wall.Length - 1f);
            a2 -= dir * cut;
            b2 -= dir * cut;
        }
    }

    private static bool TryLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        float x1 = p1.X;
        float y1 = p1.Y;
        float x2 = p2.X;
        float y2 = p2.Y;

        float x3 = p3.X;
        float y3 = p3.Y;
        float x4 = p4.X;
        float y4 = p4.Y;

        float denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        if (MathF.Abs(denominator) < 0.001f)
            return false;

        float px =
            ((x1 * y2 - y1 * x2) * (x3 - x4) -
             (x1 - x2) * (x3 * y4 - y3 * x4)) / denominator;

        float py =
            ((x1 * y2 - y1 * x2) * (y3 - y4) -
             (y1 - y2) * (x3 * y4 - y3 * x4)) / denominator;

        intersection = new Vector2(px, py);
        return true;
    }
}
