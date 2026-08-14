using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class CeilingTests
{
    [Fact]
    public void RebuildAutomaticCeiling_AmbienteFechado_GeraMalha()
    {
        var room = new Room { ShowAutomaticCeiling = true };
        room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3000, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3000, 0), new Vector2(3000, 2500), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3000, 2500), new Vector2(0, 2500), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(0, 2500), new Vector2(0, 0), 150, 2600, WallOrientation.Right)
        ]);

        Assert.NotNull(room.Ceiling);
        Assert.True(room.Ceiling!.Mesh.Vertices.Count > 0);
        Assert.Equal(2600f, room.Ceiling.Height);
    }

    [Fact]
    public void RebuildAutomaticCeiling_Desligado_NaoGera()
    {
        var room = new Room { ShowAutomaticCeiling = false };
        room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(2000, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(2000, 0), new Vector2(2000, 2000), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(2000, 2000), new Vector2(0, 2000), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(0, 2000), new Vector2(0, 0), 150, 2600, WallOrientation.Right)
        ]);

        Assert.Null(room.Ceiling);
    }
}
