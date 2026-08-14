namespace Tracos3DStudio;

public sealed class WallOpening
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public OpeningType Type { get; set; } = OpeningType.Door;

    public float DistanceFromStart { get; set; }

    public float Width { get; set; } = 800f;

    public float Height { get; set; } = 2100f;

    public float SillHeight { get; set; } = 0f;

    public bool AutoCutWall { get; set; } = true;

    public float EndDistance => DistanceFromStart + Width;

    public float TopHeight => SillHeight + Height;

    public bool OverlapsWith(WallOpening other)
    {
        return DistanceFromStart < other.EndDistance && other.DistanceFromStart < EndDistance;
    }

    public static WallOpening Door(float distanceFromStart, float width = 800f, float height = 2100f)
    {
        return new WallOpening
        {
            Type = OpeningType.Door,
            DistanceFromStart = distanceFromStart,
            Width = width,
            Height = height,
            SillHeight = 0f,
            AutoCutWall = true
        };
    }

    public static WallOpening Window(float distanceFromStart, float width = 1200f, float height = 1000f, float sillHeight = 1100f)
    {
        return new WallOpening
        {
            Type = OpeningType.Window,
            DistanceFromStart = distanceFromStart,
            Width = width,
            Height = height,
            SillHeight = sillHeight,
            AutoCutWall = true
        };
    }
}