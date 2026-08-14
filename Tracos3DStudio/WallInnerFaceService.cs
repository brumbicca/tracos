using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Face interna da parede (linha tracejada) — referência para cotas e medidas.</summary>
public readonly struct WallInnerFaceGeometry
{
    public Vector2 InnerStart { get; init; }

    public Vector2 InnerEnd { get; init; }

    public Vector2 Direction { get; init; }

    public float Length { get; init; }

    public Vector2 InteriorNormal { get; init; }

    public float DistanceFromInnerStart(Vector2 point) =>
        Math.Clamp(Vector2.Dot(point - InnerStart, Direction), 0f, Length);

    public Vector2 PointAtDistance(float distanceAlong) =>
        InnerStart + Direction * Math.Clamp(distanceAlong, 0f, Length);
}

public static class WallInnerFaceService
{
    /// <summary>
    /// A face de referência (Comprimento digitado) coincide com a face interna do ambiente
    /// quando Orientação=Interna e o percurso tem interior à esquerda (fluxo horário Promob).
    /// </summary>
    public static bool ReferenceIsOnInteriorRoomSide(WallMeasureSide measureSide, bool interiorOnLeft) =>
        (measureSide == WallMeasureSide.Interior) == interiorOnLeft;

    public static WallInnerFaceGeometry GetInnerFace(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls)
    {
        var visuals = WallVisualBuilder.BuildWithCorners(walls);
        var visual = visuals.First(v => v.Wall.Id == wall.Id);
        bool interiorOnLeft = ComputeRoomInteriorOnLeft(walls);
        bool useFaceA = UseInnerFaceA(visual, interiorOnLeft);

        Vector2 innerStart = useFaceA ? visual.A1 : visual.B1;
        Vector2 innerEnd = useFaceA ? visual.A2 : visual.B2;
        Vector2 delta = innerEnd - innerStart;
        float length = delta.Length;
        Vector2 direction = length > 0.001f ? delta / length : wall.Direction;

        Vector2 faceMid = (innerStart + innerEnd) * 0.5f;
        Vector2 interiorRef = ComputeInteriorReference(walls);
        Vector2 toInterior = interiorRef - faceMid;
        Vector2 interiorNormal = toInterior.LengthSquared > 1f
            ? Vector2.Normalize(toInterior)
            : wall.RightNormal;

        return new WallInnerFaceGeometry
        {
            InnerStart = innerStart,
            InnerEnd = innerEnd,
            Direction = direction,
            Length = length,
            InteriorNormal = interiorNormal
        };
    }

    public static WallInnerFaceGeometry GetReferenceFace(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls)
    {
        var visuals = WallVisualBuilder.BuildWithCorners(walls);
        var visual = visuals.First(v => v.Wall.Id == wall.Id);
        bool interiorOnLeft = ComputeRoomInteriorOnLeft(walls);
        bool refOnInteriorRoom = ReferenceIsOnInteriorRoomSide(wall.MeasureSide, interiorOnLeft);
        bool innerUsesFaceA = UseInnerFaceA(visual, interiorOnLeft);
        bool refUsesFaceA = refOnInteriorRoom ? innerUsesFaceA : !innerUsesFaceA;

        Vector2 refStart = refUsesFaceA ? visual.A1 : visual.B1;
        Vector2 refEnd = refUsesFaceA ? visual.A2 : visual.B2;
        Vector2 delta = refEnd - refStart;
        float length = delta.Length;
        Vector2 direction = length > 0.001f ? delta / length : wall.Direction;

        return new WallInnerFaceGeometry
        {
            InnerStart = refStart,
            InnerEnd = refEnd,
            Direction = direction,
            Length = length,
            InteriorNormal = wall.RightNormal
        };
    }

    /// <summary>
    /// Converte um segmento da face de referência (Comprimento) no eixo da parede.
    /// </summary>
    public static (Vector2 AxisStart, Vector2 AxisEnd) ReferenceSegmentToAxis(
        Vector2 referenceStart,
        Vector2 referenceEnd,
        float thickness,
        bool interiorOnLeft,
        WallMeasureSide measureSide)
    {
        Vector2 delta = referenceEnd - referenceStart;

        if (delta.LengthSquared < 0.001f)
            return (referenceStart, referenceEnd);

        Vector2 offset = ReferenceToAxisOffset(
            Vector2.Normalize(delta),
            thickness,
            interiorOnLeft,
            ReferenceIsOnInteriorRoomSide(measureSide, interiorOnLeft));

        return (referenceStart + offset, referenceEnd + offset);
    }

    /// <summary>Compatibilidade com testes legados (Orientação interna).</summary>
    public static (Vector2 AxisStart, Vector2 AxisEnd) InnerSegmentToAxis(
        Vector2 innerStart,
        Vector2 innerEnd,
        float thickness,
        bool interiorOnLeft) =>
        ReferenceSegmentToAxis(innerStart, innerEnd, thickness, interiorOnLeft, WallMeasureSide.Interior);

    /// <summary>
    /// Área assinada do caminho (aberto ou fechado). Positivo = interior à esquerda de cada aresta.
    /// </summary>
    public static float PathSignedArea2(IReadOnlyList<Vector2> points, bool closed)
    {
        if (points.Count < 2)
            return 0f;

        float area = 0f;
        int lastEdge = closed ? points.Count : points.Count - 1;

        for (int i = 0; i < lastEdge; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Count];
            area += p0.X * p1.Y - p1.X * p0.Y;
        }

        return area;
    }

    public static bool IsInteriorOnLeftOfPath(IReadOnlyList<Vector2> points, bool closed) =>
        TryGetInteriorOnLeft(points, closed, out bool interiorOnLeft) && interiorOnLeft;

    /// <summary>
    /// Determina se o ambiente fica à esquerda do percurso. Fallback horário Promob quando área ≈ 0.
    /// </summary>
    public static bool TryGetInteriorOnLeft(IReadOnlyList<Vector2> points, bool closed, out bool interiorOnLeft)
    {
        float area = PathSignedArea2(points, closed);

        if (MathF.Abs(area) > 1e-3f)
        {
            interiorOnLeft = area > 0f;
            return true;
        }

        interiorOnLeft = true;
        return false;
    }

    public static bool ComputeRoomInteriorOnLeft(IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return true;

        var points = walls.Select(w => w.Start).ToList();
        bool closed = walls.Count >= 3 &&
                      Geometry2D.AlmostEqual(walls[0].Start, walls[^1].End, 20f);

        TryGetInteriorOnLeft(points, closed, out bool interiorOnLeft);
        return interiorOnLeft;
    }

    /// <summary>
    /// Constrói paredes a partir dos cantos da face de referência (Comprimento digitado).
    /// </summary>
    public static List<WallSegment> BuildWallsFromReferenceCorners(
        IReadOnlyList<Vector2> referenceCorners,
        bool isClosed,
        float thickness,
        float height,
        WallOrientation orientation,
        WallMeasureSide measureSide,
        IReadOnlyList<float>? lengthTargets = null)
    {
        var walls = new List<WallSegment>();

        if (referenceCorners.Count < 2)
            return walls;

        int segmentCount = isClosed ? referenceCorners.Count : referenceCorners.Count - 1;
        TryGetInteriorOnLeft(referenceCorners, isClosed, out bool interiorOnLeft);
        bool referenceIsOnInteriorRoomSide = ReferenceIsOnInteriorRoomSide(measureSide, interiorOnLeft);

        var axisCorners = BuildAxisCornersFromReference(
            referenceCorners,
            isClosed,
            thickness,
            interiorOnLeft,
            referenceIsOnInteriorRoomSide);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 axisStart = axisCorners[i];
            Vector2 axisEnd = axisCorners[(i + 1) % axisCorners.Count];

            if ((axisEnd - axisStart).LengthSquared < 1f)
                continue;

            var wall = new WallSegment(axisStart, axisEnd, thickness, height, orientation)
            {
                MeasureSide = measureSide
            };

            if (lengthTargets != null && i < lengthTargets.Count)
                wall.InnerLengthTarget = lengthTargets[i];

            walls.Add(wall);
        }

        return walls;
    }

    /// <summary>Alias legado — usa Orientação interna.</summary>
    public static List<WallSegment> BuildWallsFromInnerCorners(
        IReadOnlyList<Vector2> innerCorners,
        bool isClosed,
        float thickness,
        float height,
        WallOrientation orientation,
        IReadOnlyList<float>? innerLengthTargets = null) =>
        BuildWallsFromReferenceCorners(
            innerCorners,
            isClosed,
            thickness,
            height,
            orientation,
            WallMeasureSide.Interior,
            innerLengthTargets);

    private static List<Vector2> BuildAxisCornersFromReference(
        IReadOnlyList<Vector2> referenceCorners,
        bool isClosed,
        float thickness,
        bool interiorOnLeft,
        bool referenceIsOnInteriorRoomSide)
    {
        int count = referenceCorners.Count;
        var axis = new List<Vector2>(count);

        if (isClosed)
        {
            for (int i = 0; i < count; i++)
            {
                int prev = (i - 1 + count) % count;
                Vector2 corner = referenceCorners[i];
                Vector2 dirIn = SegmentDirection(referenceCorners[prev], corner);
                Vector2 dirOut = SegmentDirection(corner, referenceCorners[(i + 1) % count]);
                axis.Add(IntersectReferenceOffsetLines(
                    corner, dirIn, dirOut, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide));
            }

            return axis;
        }

        Vector2 firstDir = SegmentDirection(referenceCorners[0], referenceCorners[1]);
        axis.Add(referenceCorners[0] + ReferenceToAxisOffset(
            firstDir, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide));

        for (int i = 1; i < count - 1; i++)
        {
            Vector2 dirIn = SegmentDirection(referenceCorners[i - 1], referenceCorners[i]);
            Vector2 dirOut = SegmentDirection(referenceCorners[i], referenceCorners[i + 1]);
            axis.Add(IntersectReferenceOffsetLines(
                referenceCorners[i], dirIn, dirOut, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide));
        }

        Vector2 lastDir = SegmentDirection(referenceCorners[count - 2], referenceCorners[count - 1]);
        axis.Add(referenceCorners[count - 1] + ReferenceToAxisOffset(
            lastDir, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide));

        return axis;
    }

    private static Vector2 ReferenceToAxisOffset(
        Vector2 direction,
        float thickness,
        bool interiorOnLeft,
        bool referenceIsOnInteriorRoomSide)
    {
        Vector2 left = new(-direction.Y, direction.X);

        if (referenceIsOnInteriorRoomSide)
        {
            // Referência na face interna do ambiente: B=ref quando interior à esquerda; A=ref quando à direita.
            return interiorOnLeft ? -left * thickness : Vector2.Zero;
        }

        // Referência na face externa: A=ref quando interior à esquerda; B=ref quando à direita.
        return interiorOnLeft ? Vector2.Zero : -left * thickness;
    }

    private static Vector2 SegmentDirection(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;

        return delta.LengthSquared < 0.001f
            ? Vector2.UnitX
            : Vector2.Normalize(delta);
    }

    private static Vector2 IntersectReferenceOffsetLines(
        Vector2 referenceCorner,
        Vector2 dirIn,
        Vector2 dirOut,
        float thickness,
        bool interiorOnLeft,
        bool referenceIsOnInteriorRoomSide)
    {
        Vector2 offIn = ReferenceToAxisOffset(dirIn, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide);
        Vector2 offOut = ReferenceToAxisOffset(dirOut, thickness, interiorOnLeft, referenceIsOnInteriorRoomSide);
        Vector2 pIn = referenceCorner + offIn;
        Vector2 pOut = referenceCorner + offOut;

        return Geometry2D.TryLineIntersection(pIn, pIn + dirIn, pOut, pOut + dirOut, out Vector2 axisCorner)
            ? axisCorner
            : (pIn + pOut) * 0.5f;
    }

    /// <summary>Reposiciona o fim da parede para que a face de referência tenha o comprimento desejado.</summary>
    public static void ApplyReferenceLengthToWall(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        float desiredReferenceLength)
    {
        if (desiredReferenceLength <= 0f)
            return;

        var reference = GetReferenceFace(wall, walls);
        Vector2 newRefEnd = reference.InnerStart + reference.Direction * desiredReferenceLength;

        var visuals = WallVisualBuilder.BuildWithCorners(walls);
        var visual = visuals.First(v => v.Wall.Id == wall.Id);
        bool interiorOnLeft = ComputeRoomInteriorOnLeft(walls);
        bool refOnInteriorRoom = ReferenceIsOnInteriorRoomSide(wall.MeasureSide, interiorOnLeft);
        bool innerUsesFaceA = UseInnerFaceA(visual, interiorOnLeft);
        bool refUsesFaceA = refOnInteriorRoom ? innerUsesFaceA : !innerUsesFaceA;

        Vector2 axisOffset = refUsesFaceA ? Vector2.Zero : -wall.LeftNormal * wall.Thickness;
        wall.End = newRefEnd + axisOffset;
        wall.InnerLengthTarget = desiredReferenceLength;
    }

    /// <summary>Alias legado.</summary>
    public static void ApplyInnerLengthToWall(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        float desiredInnerLength) =>
        ApplyReferenceLengthToWall(wall, walls, desiredInnerLength);

    public static float GetInnerLength(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls) =>
        GetInnerFace(wall, walls).Length;

    public static float GetReferenceFaceLength(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls) =>
        GetReferenceFace(wall, walls).Length;

    /// <summary>Comprimento real da face interna do ambiente (linha tracejada).</summary>
    public static float GetDisplayInnerLength(WallSegment wall, IReadOnlyList<WallSegment> walls) =>
        GetInnerLength(wall, walls);

    /// <summary>Comprimento real na face de referência (Orientação Promob).</summary>
    public static float GetDisplayReferenceLength(WallSegment wall, IReadOnlyList<WallSegment> walls) =>
        GetReferenceFaceLength(wall, walls);

    public static Vector2 ComputeRoomCentroid(IReadOnlyList<WallSegment> walls) =>
        ComputeInteriorReference(walls);

    public static bool UsesInnerFaceA(VisualWallSegment visual, IReadOnlyList<WallSegment> walls) =>
        UseInnerFaceA(visual, ComputeRoomInteriorOnLeft(walls));

    /// <summary>
    /// Face A (eixo) fica do lado externo; face B = A + esquerda × espessura.
    /// Interior à esquerda do percurso → face interna do ambiente = B.
    /// </summary>
    private static bool UseInnerFaceA(VisualWallSegment visual, bool interiorOnLeft) =>
        !interiorOnLeft;

    private static Vector2 ComputeInteriorReference(IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return Vector2.Zero;

        bool closed = walls.Count >= 3 &&
                      Geometry2D.AlmostEqual(walls[0].Start, walls[^1].End, 20f);

        if (closed || walls.Count >= 3)
            return ComputePolygonCentroid(walls.Select(w => w.Start).ToList());

        if (walls.Count == 2)
        {
            var w0 = walls[0];
            var w1 = walls[1];
            bool interiorOnRight = !ComputeRoomInteriorOnLeft(walls);
            Vector2 inward0 = interiorOnRight ? w0.RightNormal : w0.LeftNormal;
            Vector2 inward1 = interiorOnRight ? w1.RightNormal : w1.LeftNormal;
            return w0.End
                   + inward0 * MathF.Max(w0.Thickness * 2f, 200f)
                   + inward1 * MathF.Max(w1.Thickness * 2f, 200f);
        }

        var only = walls[0];
        bool right = !ComputeRoomInteriorOnLeft(walls);
        Vector2 inward = right ? only.RightNormal : only.LeftNormal;
        Vector2 mid = (only.Start + only.End) * 0.5f;
        return mid + inward * MathF.Max(only.Thickness * 2f, 200f);
    }

    private static Vector2 ComputePolygonCentroid(IReadOnlyList<Vector2> points)
    {
        if (points.Count == 0)
            return Vector2.Zero;

        if (points.Count < 3)
        {
            float sx = 0f;
            float sy = 0f;

            foreach (var p in points)
            {
                sx += p.X;
                sy += p.Y;
            }

            return new Vector2(sx / points.Count, sy / points.Count);
        }

        float area2 = 0f;
        float cx = 0f;
        float cy = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Count];
            float cross = p0.X * p1.Y - p1.X * p0.Y;
            area2 += cross;
            cx += (p0.X + p1.X) * cross;
            cy += (p0.Y + p1.Y) * cross;
        }

        if (MathF.Abs(area2) < 1e-6f)
            return points[0];

        float inv = 1f / (3f * area2);
        return new Vector2(cx * inv, cy * inv);
    }
}
