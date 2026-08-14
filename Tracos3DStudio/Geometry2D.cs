using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class Geometry2D
{
    public static float PolygonArea(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 3)
            return 0f;

        var area = 0f;

        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];

            area += a.X * b.Y - b.X * a.Y;
        }

        return area * 0.5f;
    }

    public static bool IsClockwise(IReadOnlyList<Vector2> points)
    {
        return PolygonArea(points) < 0;
    }

    public static Vector2 SnapAngle(Vector2 origin, Vector2 current, float incrementDegrees = 45f)
    {
        var delta = current - origin;
        var length = delta.Length;

        if (length < 0.001f)
            return current;

        var angle = MathF.Atan2(delta.Y, delta.X);
        var step = MathHelper.DegreesToRadians(incrementDegrees);
        var snappedAngle = MathF.Round(angle / step) * step;

        return origin + new Vector2(MathF.Cos(snappedAngle), MathF.Sin(snappedAngle)) * length;
    }

    public static bool AlmostEqual(Vector2 a, Vector2 b, float tolerance = 5f)
    {
        return (a - b).Length <= tolerance;
    }

    public static bool TryLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        float denominator = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);

        if (MathF.Abs(denominator) < 0.001f)
            return false;

        float px =
            ((p1.X * p2.Y - p1.Y * p2.X) * (p3.X - p4.X) -
             (p1.X - p2.X) * (p3.X * p4.Y - p3.Y * p4.X)) / denominator;

        float py =
            ((p1.X * p2.Y - p1.Y * p2.X) * (p3.Y - p4.Y) -
             (p1.Y - p2.Y) * (p3.X * p4.Y - p3.Y * p4.X)) / denominator;

        intersection = new Vector2(px, py);
        return true;
    }

    public static List<Vector2> RemoveDuplicates(IEnumerable<Vector2> points, float tolerance = 2f)
    {
        var result = new List<Vector2>();

        foreach (var point in points)
        {
            if (result.Count == 0 || !AlmostEqual(result[^1], point, tolerance))
                result.Add(point);
        }

        if (result.Count > 2 && AlmostEqual(result[0], result[^1], tolerance))
            result.RemoveAt(result.Count - 1);

        return result;
    }

    public static Vector2 DirectionFromAngle(float degrees)
    {
        var radians = MathHelper.DegreesToRadians(degrees);

        return new Vector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    public static bool ContainsPoint(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        if (polygon.Count < 3)
            return false;

        bool inside = false;

        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool intersects = (pi.Y > point.Y) != (pj.Y > point.Y) &&
                point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y + 1e-9f) + pi.X;

            if (intersects)
                inside = !inside;
        }

        return inside;
    }
}