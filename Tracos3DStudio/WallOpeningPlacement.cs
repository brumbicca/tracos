namespace Tracos3DStudio;

public static class WallOpeningPlacement
{
    public const float MinEdgeMargin = 50f;
    public const float SnapStep = 50f;

    public const float DefaultDoorWidth = 800f;
    public const float DefaultDoorHeight = 2100f;
    public const float DefaultWindowWidth = 1200f;
    public const float DefaultWindowHeight = 1000f;
    public const float DefaultWindowSill = 1100f;

    public static float MinWallLengthForStandardDoor =>
        DefaultDoorWidth + MinEdgeMargin * 2f;

    public static WallOpening CreateOpening(OpeningType type, float distanceFromStart)
    {
        return type switch
        {
            OpeningType.Window => WallOpening.Window(
                distanceFromStart,
                DefaultWindowWidth,
                DefaultWindowHeight,
                DefaultWindowSill),
            _ => WallOpening.Door(distanceFromStart, DefaultDoorWidth, DefaultDoorHeight)
        };
    }

    public static float ComputeStartDistance(float clickDistance, float openingWidth, float wallLength)
    {
        float half = openingWidth / 2f;
        float start = clickDistance - half;
        return SnapDistance(ClampStart(start, openingWidth, wallLength));
    }

    public static float ClampStart(float distanceFromStart, float openingWidth, float wallLength)
    {
        if (wallLength <= openingWidth + MinEdgeMargin * 2f)
            return MinEdgeMargin;

        float maxStart = wallLength - openingWidth - MinEdgeMargin;
        return Math.Clamp(distanceFromStart, MinEdgeMargin, maxStart);
    }

    public static float SnapDistance(float distanceFromStart)
    {
        return MathF.Round(distanceFromStart / SnapStep) * SnapStep;
    }

    public static bool CanPlace(WallSegment wall, WallOpening opening)
    {
        if (wall.Length <= 0.001f)
            return false;

        if (opening.DistanceFromStart < MinEdgeMargin)
            return false;

        if (opening.EndDistance > wall.Length - MinEdgeMargin)
            return false;

        if (opening.TopHeight > wall.Height + 0.01f)
            return false;

        if (opening.SillHeight < 0f)
            return false;

        foreach (var existing in wall.Openings)
        {
            if (existing.Id == opening.Id)
                continue;

            if (opening.OverlapsWith(existing))
                return false;
        }

        return true;
    }

    public static bool TryAddOpening(WallSegment wall, WallOpening opening)
    {
        if (!CanPlace(wall, opening))
            return false;

        wall.Openings.Add(opening);
        return true;
    }
}
