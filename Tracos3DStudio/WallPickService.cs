using OpenTK.Mathematics;

namespace Tracos3DStudio;

public readonly struct WallPickTarget
{
    public Guid WallId { get; init; }
    public Vector2 A1 { get; init; }
    public Vector2 A2 { get; init; }
    public Vector2 B1 { get; init; }
    public Vector2 B2 { get; init; }
    public Vector2 Start { get; init; }
    public Vector2 End { get; init; }
    public float Height { get; init; }
    public float HeightStart { get; init; }
    public float HeightEnd { get; init; }
    public float FloorOffset { get; init; }
    public float Thickness { get; init; }
}

public static class WallPickService
{
    public const float FloorPickTolerance = 800f;

    public enum WallPickFaceKind
    {
        None,
        LateralA,
        LateralB,
        Top
    }

    public static bool TryPickRayDetailed(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        IReadOnlyList<WallPickTarget> targets,
        out Guid wallId,
        out float distanceAlong,
        out float heightFromFloor,
        out WallPickFaceKind faceKind,
        out Vector3 hitPoint)
    {
        wallId = Guid.Empty;
        distanceAlong = 0f;
        heightFromFloor = 0f;
        faceKind = WallPickFaceKind.None;
        hitPoint = Vector3.Zero;

        float hitDistance = float.MaxValue;
        WallPickTarget bestTarget = default;
        bool hasTarget = false;
        Vector3 bestHit = Vector3.Zero;
        WallPickFaceKind bestFace = WallPickFaceKind.None;
        bool hitTopFace = false;

        foreach (var target in targets)
        {
            if (!TryRayHitTarget(rayOrigin, rayDirection, target, out float t, out Vector3 hit, out bool topFace, out WallPickFaceKind targetFace))
                continue;

            if (t >= hitDistance)
                continue;

            hitDistance = t;
            bestTarget = target;
            hasTarget = true;
            bestHit = hit;
            bestFace = targetFace;
            hitTopFace = topFace;
        }

        if (!hasTarget)
            return false;

        wallId = bestTarget.WallId;
        hitPoint = bestHit;
        faceKind = hitTopFace ? WallPickFaceKind.Top : bestFace;
        heightFromFloor = bestHit.Y - bestTarget.FloorOffset;
        Vector2 hitFloor = Geometry3D.HitPointToFloor(bestHit);
        distanceAlong = Math.Clamp(
            GetDistanceAlongWall(bestTarget.Start, bestTarget.End, hitFloor),
            0f,
            (bestTarget.End - bestTarget.Start).Length);

        return true;
    }

    public static WallPickTarget FromSegment(WallSegment wall, Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        return new WallPickTarget
        {
            WallId = wall.Id,
            A1 = a1,
            A2 = a2,
            B1 = b1,
            B2 = b2,
            Start = wall.Start,
            End = wall.End,
            Height = wall.Height,
            HeightStart = wall.HeightStart,
            HeightEnd = wall.HeightEnd,
            FloorOffset = wall.FloorOffset,
            Thickness = wall.Thickness
        };
    }

    public static bool TryPickRay(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        IReadOnlyList<WallPickTarget> targets,
        out Guid wallId,
        out float distanceAlong,
        out float hitDistance,
        out bool hitTopFace)
    {
        wallId = Guid.Empty;
        distanceAlong = 0f;
        hitDistance = float.MaxValue;
        hitTopFace = false;

        // Primeira passagem: encontra o hit mais próximo (qualquer face)
        foreach (var target in targets)
        {
            if (!TryRayHitTarget(rayOrigin, rayDirection, target, out float t, out Vector3 hitPoint, out bool topFace, out _))
                continue;

            if (t >= hitDistance)
                continue;

            hitDistance = t;
            wallId = target.WallId;
            hitTopFace = topFace;
            Vector2 hitFloor = Geometry3D.HitPointToFloor(hitPoint);
            distanceAlong = Math.Clamp(GetDistanceAlongWall(target.Start, target.End, hitFloor), 0f, (target.End - target.Start).Length);
        }

        return wallId != Guid.Empty;
    }

    /// <summary>Raio na face lateral da parede (face de inserção de módulos — estilo Promob).</summary>
    public static bool TryPickModuleInsertionFace(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        IReadOnlyList<WallPickTarget> targets,
        out Guid wallId,
        out float distanceAlong,
        out Vector2 interiorNormal,
        out Vector3 hitPoint)
    {
        wallId = Guid.Empty;
        distanceAlong = 0f;
        interiorNormal = Vector2.UnitY;
        hitPoint = Vector3.Zero;

        float bestT = float.MaxValue;
        Vector2 bestInterior = Vector2.UnitY;
        Guid bestWall = Guid.Empty;
        Vector2 bestStart = Vector2.Zero;
        Vector2 bestEnd = Vector2.Zero;
        Vector3 bestHit = Vector3.Zero;

        foreach (var target in targets)
        {
            float yFloor = target.FloorOffset;
            float yTopStart = yFloor + target.HeightStart;
            float yTopEnd = yFloor + target.HeightEnd;

            Vector3 a0 = new(target.A1.X, yFloor, target.A1.Y);
            Vector3 a1 = new(target.A1.X, yTopStart, target.A1.Y);
            Vector3 a2 = new(target.A2.X, yFloor, target.A2.Y);
            Vector3 a3 = new(target.A2.X, yTopEnd, target.A2.Y);

            Vector3 b0 = new(target.B1.X, yFloor, target.B1.Y);
            Vector3 b1 = new(target.B1.X, yTopStart, target.B1.Y);
            Vector3 b2 = new(target.B2.X, yFloor, target.B2.Y);
            Vector3 b3 = new(target.B2.X, yTopEnd, target.B2.Y);

            Vector2 aMid = (target.A1 + target.A2) * 0.5f;
            Vector2 bMid = (target.B1 + target.B2) * 0.5f;
            Vector2 towardRoomFromA = bMid - aMid;
            if (towardRoomFromA.LengthSquared > 1f)
                towardRoomFromA = Vector2.Normalize(towardRoomFromA);
            Vector2 towardRoomFromB = -towardRoomFromA;

            ConsiderInsertionFace(rayOrigin, rayDirection, a0, a2, a3, a1, towardRoomFromA,
                target, ref bestT, ref bestHit, ref bestWall, ref bestStart, ref bestEnd, ref bestInterior);
            ConsiderInsertionFace(rayOrigin, rayDirection, b2, b0, b1, b3, towardRoomFromB,
                target, ref bestT, ref bestHit, ref bestWall, ref bestStart, ref bestEnd, ref bestInterior);
        }

        if (bestWall == Guid.Empty)
            return false;

        wallId = bestWall;
        hitPoint = bestHit;
        interiorNormal = bestInterior;
        Vector2 hitFloor = Geometry3D.HitPointToFloor(hitPoint);
        distanceAlong = Math.Clamp(
            GetDistanceAlongWall(bestStart, bestEnd, hitFloor),
            0f,
            (bestEnd - bestStart).Length);

        return true;
    }

    private static void ConsiderInsertionFace(
        Vector3 origin,
        Vector3 direction,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        Vector2 interiorNormal,
        WallPickTarget target,
        ref float bestT,
        ref Vector3 bestHit,
        ref Guid bestWall,
        ref Vector2 bestStart,
        ref Vector2 bestEnd,
        ref Vector2 bestInterior)
    {
        if (!Geometry3D.TryRayQuadIntersect(origin, direction, p0, p1, p2, p3, out float t, out Vector3 hit))
            return;

        if (t >= bestT)
            return;

        bestT = t;
        bestHit = hit;
        bestWall = target.WallId;
        bestStart = target.Start;
        bestEnd = target.End;
        bestInterior = interiorNormal;
    }

    public static bool TryPickFloor(
        Vector2 point,
        IReadOnlyList<WallSegment> walls,
        out Guid wallId,
        out float distanceAlong,
        float tolerance = FloorPickTolerance)
    {
        wallId = Guid.Empty;
        distanceAlong = 0f;

        WallSegment? bestWall = null;
        float bestDistance = float.MaxValue;
        float bestAlong = 0f;

        foreach (var wall in walls)
        {
            float perpendicular = DistancePointToSegment(point, wall.Start, wall.End);
            float maxDistance = Math.Max(tolerance, wall.Thickness + 400f);

            if (perpendicular > maxDistance || perpendicular >= bestDistance)
                continue;

            bestDistance = perpendicular;
            bestWall = wall;
            bestAlong = Math.Clamp(GetDistanceAlongWall(wall.Start, wall.End, point), 0f, wall.Length);
        }

        if (bestWall == null)
            return false;

        wallId = bestWall.Id;
        distanceAlong = bestAlong;
        return true;
    }

    public static float GetDistanceAlongWall(Vector2 start, Vector2 end, Vector2 point)
    {
        Vector2 direction = end - start;
        float lengthSquared = direction.LengthSquared;

        if (lengthSquared < 0.001f)
            return 0f;

        return Vector2.Dot(point - start, direction / MathF.Sqrt(lengthSquared));
    }

    public static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared;

        if (lengthSquared < 0.001f)
            return (point - start).Length;

        float t = Vector2.Dot(point - start, segment) / lengthSquared;
        t = Math.Clamp(t, 0f, 1f);

        Vector2 projected = start + segment * t;
        return (point - projected).Length;
    }

    private static bool TryRayHitTarget(
        Vector3 origin,
        Vector3 direction,
        WallPickTarget target,
        out float t,
        out Vector3 hitPoint,
        out bool hitTopFace,
        out WallPickFaceKind faceKind)
    {
        t = float.MaxValue;
        hitPoint = Vector3.Zero;
        hitTopFace = false;
        faceKind = WallPickFaceKind.None;
        bool anyHit = false;
        WallPickFaceKind bestFace = WallPickFaceKind.None;

        float yFloor    = target.FloorOffset;
        float yTopStart = yFloor + target.HeightStart;
        float yTopEnd   = yFloor + target.HeightEnd;

        // A1/B1 = lado Start, A2/B2 = lado End
        Vector3 a0 = new(target.A1.X, yFloor,    target.A1.Y);
        Vector3 a1 = new(target.A1.X, yTopStart,  target.A1.Y);
        Vector3 a2 = new(target.A2.X, yFloor,    target.A2.Y);
        Vector3 a3 = new(target.A2.X, yTopEnd,    target.A2.Y);

        Vector3 b0 = new(target.B1.X, yFloor,    target.B1.Y);
        Vector3 b1 = new(target.B1.X, yTopStart,  target.B1.Y);
        Vector3 b2 = new(target.B2.X, yFloor,    target.B2.Y);
        Vector3 b3 = new(target.B2.X, yTopEnd,    target.B2.Y);

        // Faces laterais
        ConsiderQuadDoubleSided(origin, direction, a0, a2, a3, a1, WallPickFaceKind.LateralA, ref t, ref hitPoint, ref anyHit, ref hitTopFace, ref bestFace, false);
        ConsiderQuadDoubleSided(origin, direction, b2, b0, b1, b3, WallPickFaceKind.LateralB, ref t, ref hitPoint, ref anyHit, ref hitTopFace, ref bestFace, false);
        ConsiderQuadDoubleSided(origin, direction, a0, b0, b1, a1, WallPickFaceKind.None, ref t, ref hitPoint, ref anyHit, ref hitTopFace, ref bestFace, false);
        ConsiderQuadDoubleSided(origin, direction, a2, b2, b3, a3, WallPickFaceKind.None, ref t, ref hitPoint, ref anyHit, ref hitTopFace, ref bestFace, false);

        // Topo: isTop=true
        ConsiderQuadDoubleSided(origin, direction, a1, b1, b3, a3, WallPickFaceKind.Top, ref t, ref hitPoint, ref anyHit, ref hitTopFace, ref bestFace, true);

        faceKind = bestFace;
        return anyHit;
    }

    private static void ConsiderQuad(
        Vector3 origin,
        Vector3 direction,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        WallPickFaceKind lateralFace,
        ref float bestT,
        ref Vector3 bestHit,
        ref bool anyHit,
        ref bool hitTopFace,
        ref WallPickFaceKind bestFace,
        bool isTop)
    {
        if (!Geometry3D.TryRayQuadIntersect(origin, direction, p0, p1, p2, p3, out float candidateT, out Vector3 candidateHit))
            return;

        if (candidateT >= bestT)
            return;

        bestT = candidateT;
        bestHit = candidateHit;
        anyHit = true;
        hitTopFace = isTop;
        if (!isTop && lateralFace != WallPickFaceKind.None)
            bestFace = lateralFace;
    }

    // Testa o quad dos dois lados — necessário para topo e faces internas
    private static void ConsiderQuadDoubleSided(
        Vector3 origin,
        Vector3 direction,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        WallPickFaceKind lateralFace,
        ref float bestT,
        ref Vector3 bestHit,
        ref bool anyHit,
        ref bool hitTopFace,
        ref WallPickFaceKind bestFace,
        bool isTop)
    {
        ConsiderQuad(origin, direction, p0, p1, p2, p3, lateralFace, ref bestT, ref bestHit, ref anyHit, ref hitTopFace, ref bestFace, isTop);
        ConsiderQuad(origin, direction, p3, p2, p1, p0, lateralFace, ref bestT, ref bestHit, ref anyHit, ref hitTopFace, ref bestFace, isTop);
    }
}
