using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class RoomCameraBoundsTests
{
    [Fact]
    public void Compute_SemParedes_RetornaPadrao()
    {
        RoomCameraBounds.Compute([], out var center, out var planExtent, out var maxHeight);

        Assert.Equal(0f, center.X);
        Assert.Equal(1300f, center.Y);
        Assert.Equal(0f, center.Z);
        Assert.Equal(6000f, planExtent);
        Assert.Equal(2600f, maxHeight);
    }

    [Fact]
    public void Compute_RetanguloFechado_CentralizaAmbiente()
    {
        var walls = new List<WallSegment>
        {
            new(new Vector2(0, 0), new Vector2(4000, 0)),
            new(new Vector2(4000, 0), new Vector2(4000, 3000)),
            new(new Vector2(4000, 3000), new Vector2(0, 3000)),
            new(new Vector2(0, 3000), new Vector2(0, 0))
        };

        RoomCameraBounds.Compute(walls, out var center, out var planExtent, out var maxHeight);

        Assert.Equal(2000f, center.X);
        Assert.Equal(1500f, center.Z);
        Assert.True(planExtent > 4000f);
        Assert.Equal(2600f, maxHeight);
    }
}
