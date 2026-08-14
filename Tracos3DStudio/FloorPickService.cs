using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class FloorPickService
{
    public const float PickPlaneY = 8f;

    public static bool TryPickRay(
        Vector3 origin,
        Vector3 direction,
        IReadOnlyList<Vector2> floorPoints,
        out float hitDistance)
    {
        hitDistance = float.MaxValue;

        if (floorPoints.Count < 3)
            return false;

        if (!Geometry3D.TryRayHorizontalPlane(origin, direction, PickPlaneY, out float t, out Vector3 hitPoint))
            return false;

        Vector2 hitFloor = Geometry3D.HitPointToFloor(hitPoint);

        if (!Geometry2D.ContainsPoint(floorPoints, hitFloor))
            return false;

        hitDistance = t;
        return true;
    }
}
