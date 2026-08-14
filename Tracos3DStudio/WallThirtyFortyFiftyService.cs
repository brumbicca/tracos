using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class WallCornerJoint
{
    public Vector2 Vertex { get; init; }

    public WallSegment ReferenceWall { get; init; } = null!;

    public WallSegment MovingWall { get; init; } = null!;

    public Vector2 ReferenceDirection { get; init; }

    public Vector2 MovingDirection { get; init; }

    public bool MovingWallStartsAtVertex { get; init; }
}

public static class WallThirtyFortyFiftyService
{
    public const float DefaultAmm = 300f;
    public const float DefaultBmm = 400f;
    public const float DefaultCmm = 500f;

    public static bool TryComputeTargetAngleDegrees(float aMm, float bMm, float cMm, out float angleDegrees)
    {
        angleDegrees = 0f;

        if (aMm <= 0f || bMm <= 0f || cMm <= 0f)
            return false;

        float cos = (aMm * aMm + bMm * bMm - cMm * cMm) / (2f * aMm * bMm);

        if (cos < -1f || cos > 1f)
            return false;

        angleDegrees = MathHelper.RadiansToDegrees(MathF.Acos(cos));
        return true;
    }

    public static bool TryFindCorner(
        WallSegment referenceWall,
        WallSegment movingWall,
        out WallCornerJoint joint)
    {
        joint = null!;

        if (referenceWall.Id == movingWall.Id)
            return false;

        if (TryMatchVertex(referenceWall, movingWall, referenceWall.Start, movingWall.Start, out Vector2 v1))
        {
            joint = BuildJoint(v1, referenceWall, movingWall, refAtStart: true, movAtStart: true);
            return true;
        }

        if (TryMatchVertex(referenceWall, movingWall, referenceWall.Start, movingWall.End, out Vector2 v2))
        {
            joint = BuildJoint(v2, referenceWall, movingWall, refAtStart: true, movAtStart: false);
            return true;
        }

        if (TryMatchVertex(referenceWall, movingWall, referenceWall.End, movingWall.Start, out Vector2 v3))
        {
            joint = BuildJoint(v3, referenceWall, movingWall, refAtStart: false, movAtStart: true);
            return true;
        }

        if (TryMatchVertex(referenceWall, movingWall, referenceWall.End, movingWall.End, out Vector2 v4))
        {
            joint = BuildJoint(v4, referenceWall, movingWall, refAtStart: false, movAtStart: false);
            return true;
        }

        return false;
    }

    public static WallSegment? TryFindAdjacentWall(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        float tolerance = 20f)
    {
        foreach (var other in walls)
        {
            if (other.Id == wall.Id)
                continue;

            if (SharesVertex(wall, other, tolerance))
                return other;
        }

        return null;
    }

    public static bool TryApply(
        WallSegment referenceWall,
        WallSegment movingWall,
        float aMm,
        float bMm,
        float cMm,
        out float appliedAngleDegrees)
    {
        appliedAngleDegrees = 0f;

        if (!TryComputeTargetAngleDegrees(aMm, bMm, cMm, out float targetAngleDeg))
            return false;

        if (!TryFindCorner(referenceWall, movingWall, out WallCornerJoint joint))
            return false;

        float targetRadians = MathHelper.DegreesToRadians(targetAngleDeg);
        Vector2 refDir = Vector2.Normalize(joint.ReferenceDirection);
        Vector2 movDir = Vector2.Normalize(joint.MovingDirection);

        float signedCurrent = SignedAngle(refDir, movDir);
        float signedTarget = MathF.Sign(signedCurrent) * targetRadians;

        if (MathF.Abs(signedCurrent) < 0.0001f)
            signedTarget = targetRadians;

        float delta = signedTarget - signedCurrent;
        RotateMovingWall(joint, delta);

        appliedAngleDegrees = targetAngleDeg;
        return true;
    }

    private static bool TryMatchVertex(
        WallSegment a,
        WallSegment b,
        Vector2 pointA,
        Vector2 pointB,
        out Vector2 vertex)
    {
        vertex = Vector2.Zero;

        if (!Geometry2D.AlmostEqual(pointA, pointB, 20f))
            return false;

        vertex = pointA;
        return true;
    }

    private static bool SharesVertex(WallSegment a, WallSegment b, float tolerance)
    {
        return Geometry2D.AlmostEqual(a.Start, b.Start, tolerance) ||
               Geometry2D.AlmostEqual(a.Start, b.End, tolerance) ||
               Geometry2D.AlmostEqual(a.End, b.Start, tolerance) ||
               Geometry2D.AlmostEqual(a.End, b.End, tolerance);
    }

    private static WallCornerJoint BuildJoint(
        Vector2 vertex,
        WallSegment referenceWall,
        WallSegment movingWall,
        bool refAtStart,
        bool movAtStart)
    {
        Vector2 refDir = refAtStart ? referenceWall.Direction : -referenceWall.Direction;
        Vector2 movDir = movAtStart ? movingWall.Direction : -movingWall.Direction;

        return new WallCornerJoint
        {
            Vertex = vertex,
            ReferenceWall = referenceWall,
            MovingWall = movingWall,
            ReferenceDirection = refDir,
            MovingDirection = movDir,
            MovingWallStartsAtVertex = movAtStart
        };
    }

    private static void RotateMovingWall(WallCornerJoint joint, float deltaRadians)
    {
        var wall = joint.MovingWall;
        Vector2 rotatedDir = Rotate(joint.MovingDirection, deltaRadians);

        if (joint.MovingWallStartsAtVertex)
            wall.End = joint.Vertex + Vector2.Normalize(rotatedDir) * wall.Length;
        else
            wall.Start = joint.Vertex + Vector2.Normalize(rotatedDir) * wall.Length;
    }

    private static float SignedAngle(Vector2 from, Vector2 to) =>
        MathF.Atan2(from.X * to.Y - from.Y * to.X, Vector2.Dot(from, to));

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
