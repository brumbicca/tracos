namespace Tracos3DStudio;

public static class WallLabelService
{
    public const string UnattachedGroupTitle = "Sem parede";

    public static int GetWallNumber(WallSegment wall, IReadOnlyList<WallSegment> walls)
    {
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i].Id == wall.Id)
                return i + 1;
        }

        return 0;
    }

    public static string FormatWallGroupTitle(WallSegment wall, IReadOnlyList<WallSegment> walls)
    {
        int number = GetWallNumber(wall, walls);
        float innerLength = WallInnerFaceService.GetInnerFace(wall, walls).Length;
        return number > 0
            ? $"Parede {number} — {innerLength:0} mm"
            : $"Parede — {innerLength:0} mm";
    }

    public static WallSegment? FindWall(IReadOnlyList<WallSegment> walls, Guid wallId)
    {
        foreach (var wall in walls)
        {
            if (wall.Id == wallId)
                return wall;
        }

        return null;
    }
}
