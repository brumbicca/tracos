using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class WallMoveService
{
    public static Vector2 ComputePerpendicularDragDelta(
        WallSegment wall,
        Vector2 dragStartFloor,
        Vector2 dragCurrentFloor)
    {
        Vector2 drag = dragCurrentFloor - dragStartFloor;

        if (drag.LengthSquared < 0.01f)
            return Vector2.Zero;

        Vector2 normal = wall.LeftNormal;

        if (normal.LengthSquared < 0.01f)
            normal = Vector2.UnitY;

        float signed = Vector2.Dot(drag, normal);
        return normal * signed;
    }

    public static float ComputeSignedOffsetMm(WallSegment wall, Vector2 delta) =>
        Vector2.Dot(delta, wall.LeftNormal);

    public static void ApplyTranslation(WallSegment wall, Vector2 delta)
    {
        wall.Start += delta;
        wall.End += delta;
    }

    public static bool CanDragInView(CameraViewMode viewMode, bool groupSelected) =>
        viewMode == CameraViewMode.Top && !groupSelected;
}
