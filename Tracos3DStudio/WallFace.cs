using OpenTK.Mathematics;

namespace Tracos3DStudio;

public enum FaceType
{
    Internal,
    External
}

public class WallFace
{
    public WallSegment Wall { get; set; }
    public FaceType Type { get; set; }

    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }

    public bool Selected { get; set; }
    public bool Transparent { get; set; }
    public bool Lowered { get; set; }

    public WallFace(WallSegment wall, FaceType type, Vector2 start, Vector2 end)
    {
        Wall = wall;
        Type = type;
        Start = start;
        End = end;
    }
}