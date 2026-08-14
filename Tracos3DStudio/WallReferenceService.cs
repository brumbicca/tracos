using OpenTK.Mathematics;

namespace Tracos3DStudio;

public readonly struct WallReferencePick
{
    public Guid WallId { get; init; }

    /// <summary>Ponto na face interna da parede de referência.</summary>
    public Vector2 AnchorOnInnerFace { get; init; }

    public Vector2 InteriorNormal { get; init; }

    public Vector2 WallDirection { get; init; }
}

/// <summary>Construção de parede com referência (Promob M6): distância perpendicular a parede existente.</summary>
public static class WallReferenceService
{
    public const float PickTolerance = 700f;

    public static bool TryPickInnerFace(
        Vector2 floorPoint,
        IReadOnlyList<WallSegment> walls,
        out WallReferencePick pick,
        float tolerance = PickTolerance)
    {
        pick = default;

        if (walls.Count == 0)
            return false;

        WallSegment? bestWall = null;
        WallInnerFaceGeometry bestInner = default;
        float bestDistance = float.MaxValue;
        float bestAlong = 0f;

        foreach (var wall in walls)
        {
            var inner = WallInnerFaceService.GetInnerFace(wall, walls);
            float along = inner.DistanceFromInnerStart(floorPoint);
            Vector2 onFace = inner.PointAtDistance(along);
            float distance = (floorPoint - onFace).Length;

            if (distance > tolerance || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestWall = wall;
            bestInner = inner;
            bestAlong = along;
        }

        if (bestWall == null)
            return false;

        pick = new WallReferencePick
        {
            WallId = bestWall.Id,
            AnchorOnInnerFace = bestInner.PointAtDistance(bestAlong),
            InteriorNormal = bestInner.InteriorNormal,
            WallDirection = bestInner.Direction
        };

        return true;
    }

    public static float ComputeSignedOffset(WallReferencePick pick, Vector2 targetPoint) =>
        Vector2.Dot(targetPoint - pick.AnchorOnInnerFace, pick.InteriorNormal);

    public static Vector2 ComputeDraftStartReferenceCorner(WallReferencePick pick, float signedOffsetMm) =>
        pick.AnchorOnInnerFace + pick.InteriorNormal * signedOffsetMm;
}
