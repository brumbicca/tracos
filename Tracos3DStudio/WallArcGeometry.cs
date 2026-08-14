using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Geometria de parede curva por corda (Start–End) e flecha (sagitta).</summary>
public sealed class WallArcGeometry
{
    public const float StraightToleranceMm = 0.5f;
    public const int DefaultTessellationSegments = 16;

    public Vector2 Start { get; }
    public Vector2 End { get; }
    public float FlechaMm { get; }
    public float ChordLength { get; }
    public Vector2 Midpoint { get; }
    public Vector2 ChordDirection { get; }
    public Vector2 LeftNormal { get; }
    public Vector2 BulgePoint { get; }
    public Vector2 Center { get; }
    public float Radius { get; }
    public float StartAngle { get; }
    public float SweepRadians { get; }
    public float ArcLength { get; }

    public bool IsStraight => MathF.Abs(FlechaMm) < StraightToleranceMm;

    private WallArcGeometry(
        Vector2 start,
        Vector2 end,
        float flechaMm,
        Vector2 chordDirection,
        Vector2 leftNormal,
        Vector2 midpoint,
        Vector2 bulgePoint,
        Vector2 center,
        float radius,
        float startAngle,
        float sweepRadians,
        float arcLength)
    {
        Start = start;
        End = end;
        FlechaMm = flechaMm;
        ChordLength = (end - start).Length;
        ChordDirection = chordDirection;
        LeftNormal = leftNormal;
        Midpoint = midpoint;
        BulgePoint = bulgePoint;
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        SweepRadians = sweepRadians;
        ArcLength = arcLength;
    }

    public static WallArcGeometry FromWall(WallSegment wall) =>
        FromChord(wall.Start, wall.End, wall.FlechaMm);

    public static WallArcGeometry FromChord(Vector2 start, Vector2 end, float flechaMm)
    {
        Vector2 delta = end - start;
        float chord = delta.Length;
        Vector2 chordDir = chord > 0.001f ? Vector2.Normalize(delta) : Vector2.UnitX;
        Vector2 leftNormal = new(-chordDir.Y, chordDir.X);
        Vector2 midpoint = (start + end) * 0.5f;
        Vector2 bulgePoint = midpoint + leftNormal * flechaMm;

        if (MathF.Abs(flechaMm) < StraightToleranceMm || chord < 1f)
        {
            return new WallArcGeometry(
                start, end, flechaMm, chordDir, leftNormal, midpoint, midpoint,
                midpoint, 0f, 0f, 0f, chord);
        }

        float absFlecha = MathF.Abs(flechaMm);
        float radius = (chord * chord + 4f * absFlecha * absFlecha) / (8f * absFlecha);
        float centerOffset = radius - absFlecha;
        float sign = MathF.Sign(flechaMm);
        Vector2 center = midpoint + leftNormal * sign * centerOffset;

        float startAngle = MathF.Atan2(start.Y - center.Y, start.X - center.X);
        float endAngle = MathF.Atan2(end.Y - center.Y, end.X - center.X);
        float bulgeAngle = MathF.Atan2(bulgePoint.Y - center.Y, bulgePoint.X - center.X);

        float sweep = NormalizeSweepTowardAngle(startAngle, endAngle, bulgeAngle);
        float arcLength = MathF.Abs(radius * sweep);

        return new WallArcGeometry(
            start, end, flechaMm, chordDir, leftNormal, midpoint, bulgePoint,
            center, radius, startAngle, sweep, arcLength);
    }

    public float GetArcAngleDegrees() =>
        IsStraight ? 0f : MathHelper.RadiansToDegrees(MathF.Abs(SweepRadians));

    public Vector2 GetPointAtArcLength(float distanceAlong)
    {
        if (IsStraight)
        {
            float linearT = ChordLength > 0.001f ? Math.Clamp(distanceAlong / ChordLength, 0f, 1f) : 0f;
            return Start + ChordDirection * (ChordLength * linearT);
        }

        float t = ArcLength > 0.001f ? Math.Clamp(distanceAlong / ArcLength, 0f, 1f) : 0f;
        float angle = StartAngle + SweepRadians * t;
        return Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Radius;
    }

    public Vector2 GetTangentAtArcLength(float distanceAlong)
    {
        if (IsStraight)
            return ChordDirection;

        float t = ArcLength > 0.001f ? Math.Clamp(distanceAlong / ArcLength, 0f, 1f) : 0f;
        float angle = StartAngle + SweepRadians * t;
        Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 tangent = new(-radial.Y, radial.X);

        if (SweepRadians < 0f)
            tangent = -tangent;

        return Vector2.Normalize(tangent);
    }

    public float ProjectToArcLength(Vector2 point)
    {
        if (IsStraight)
            return Math.Clamp(Vector2.Dot(point - Start, ChordDirection), 0f, ChordLength);

        float angle = MathF.Atan2(point.Y - Center.Y, point.X - Center.X);
        float rel = NormalizeAngle(angle - StartAngle);

        if (SweepRadians >= 0f)
            rel = Math.Clamp(rel, 0f, SweepRadians);
        else
            rel = -Math.Clamp(rel, 0f, -SweepRadians);

        return MathF.Abs(rel * Radius);
    }

    public float SignedFlechaFromPoint(Vector2 point)
    {
        Vector2 delta = point - Midpoint;
        return Vector2.Dot(delta, LeftNormal);
    }

    public List<Vector2> SampleCenterline(int segments)
    {
        segments = Math.Max(2, segments);

        if (IsStraight)
        {
            var line = new List<Vector2>(segments + 1);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                line.Add(Start + ChordDirection * (ChordLength * t));
            }

            return line;
        }

        var arc = new List<Vector2>(segments + 1);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            arc.Add(GetPointAtArcLength(ArcLength * t));
        }

        return arc;
    }

    public (List<Vector2> faceA, List<Vector2> faceB) BuildFacePolylines(
        float thickness,
        WallOrientation orientation,
        int segments = DefaultTessellationSegments)
    {
        var centerline = SampleCenterline(segments);
        var faceA = new List<Vector2>(centerline.Count);
        var faceB = new List<Vector2>(centerline.Count);

        float half = thickness * 0.5f;

        for (int i = 0; i < centerline.Count; i++)
        {
            float along = IsStraight
                ? ChordLength * i / (float)(centerline.Count - 1)
                : ArcLength * i / (float)(centerline.Count - 1);

            Vector2 tangent = GetTangentAtArcLength(along);
            Vector2 rightNormal = new(tangent.Y, -tangent.X);

            switch (orientation)
            {
                case WallOrientation.Left:
                    faceA.Add(centerline[i]);
                    faceB.Add(centerline[i] + rightNormal * thickness);
                    break;
                case WallOrientation.Center:
                    faceA.Add(centerline[i] + rightNormal * half);
                    faceB.Add(centerline[i] - rightNormal * half);
                    break;
                default:
                    faceA.Add(centerline[i]);
                    faceB.Add(centerline[i] - rightNormal * thickness);
                    break;
            }
        }

        return (faceA, faceB);
    }

    private static float NormalizeSweepTowardAngle(float startAngle, float endAngle, float throughAngle)
    {
        float sweepCc = NormalizeAngle(endAngle - startAngle);
        float throughRel = NormalizeAngle(throughAngle - startAngle);

        if (sweepCc <= MathF.PI)
        {
            if (throughRel <= sweepCc)
                return sweepCc;

            return sweepCc - MathF.Tau;
        }

        if (throughRel >= sweepCc)
            return sweepCc;

        return sweepCc - MathF.Tau;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle < 0f)
            angle += MathF.Tau;

        while (angle >= MathF.Tau)
            angle -= MathF.Tau;

        return angle;
    }
}
