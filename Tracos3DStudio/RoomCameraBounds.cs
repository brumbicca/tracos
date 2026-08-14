using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class RoomCameraBounds
{
    public static void Compute(
        IReadOnlyList<WallSegment> walls,
        out Vector3 center,
        out float planExtent,
        out float maxHeight)
    {
        if (walls.Count == 0)
        {
            center = new Vector3(0f, 1300f, 0f);
            planExtent = 6000f;
            maxHeight = 2600f;
            return;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;
        maxHeight = 0f;

        foreach (var wall in walls)
        {
            minX = MathF.Min(minX, MathF.Min(wall.Start.X, wall.End.X));
            maxX = MathF.Max(maxX, MathF.Max(wall.Start.X, wall.End.X));
            minZ = MathF.Min(minZ, MathF.Min(wall.Start.Y, wall.End.Y));
            maxZ = MathF.Max(maxZ, MathF.Max(wall.Start.Y, wall.End.Y));
            maxHeight = MathF.Max(maxHeight, wall.Height);
        }

        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;

        center = new Vector3(
            (minX + maxX) * 0.5f,
            maxHeight * 0.5f,
            (minZ + maxZ) * 0.5f);

        planExtent = MathF.Max(MathF.Max(sizeX, sizeZ), 2000f) * 1.35f + 1500f;
    }
}
