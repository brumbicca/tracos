using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class WallCornerChamferService
{
    public const float DefaultChamferMm = 150f;
    public const float MinChamferMm = 50f;
    public const float MinWallRemainMm = 300f;
    public const float PickToleranceMm = 450f;

    public static bool TryPickEndpoint(
        WallSegment wall,
        Vector2 floorPoint,
        out bool atStart)
    {
        atStart = false;

        float distStart = (floorPoint - wall.Start).Length;
        float distEnd = (floorPoint - wall.End).Length;

        if (MathF.Min(distStart, distEnd) > PickToleranceMm)
            return false;

        atStart = distStart <= distEnd;
        return true;
    }

    public static float GetMaxChamfer(WallSegment wall, bool atStart)
    {
        float other = atStart ? wall.ChamferEndMm : wall.ChamferStartMm;
        return MathF.Max(0f, wall.Length - other - MinWallRemainMm);
    }

    public static bool TryApply(WallSegment wall, bool atStart, float chamferMm)
    {
        if (chamferMm < MinChamferMm)
            return false;

        float max = GetMaxChamfer(wall, atStart);

        if (chamferMm > max)
            return false;

        if (atStart)
            wall.ChamferStartMm = chamferMm;
        else
            wall.ChamferEndMm = chamferMm;

        return true;
    }

    public static Vector2 GetEndpointVertex(WallSegment wall, bool atStart) =>
        atStart ? wall.Start : wall.End;
}
