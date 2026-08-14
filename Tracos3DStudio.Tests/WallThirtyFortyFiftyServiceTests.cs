using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallThirtyFortyFiftyServiceTests
{
    [Fact]
    public void TryComputeTargetAngle_300_400_500_Retorna90()
    {
        bool ok = WallThirtyFortyFiftyService.TryComputeTargetAngleDegrees(300f, 400f, 500f, out float angle);

        Assert.True(ok);
        Assert.InRange(angle, 89f, 91f);
    }

    [Fact]
    public void TryFindCorner_ParedesPerpendiculares_EncontraVertice()
    {
        var refWall = new WallSegment(Vector2.Zero, new Vector2(3000f, 0f));
        var movWall = new WallSegment(Vector2.Zero, new Vector2(0f, 3000f));

        bool ok = WallThirtyFortyFiftyService.TryFindCorner(refWall, movWall, out var joint);

        Assert.True(ok);
        Assert.True(Geometry2D.AlmostEqual(joint.Vertex, Vector2.Zero, 1f));
    }

    [Fact]
    public void TryApply_304050_AjustaParedeMovelPara90()
    {
        var refWall = new WallSegment(Vector2.Zero, new Vector2(4000f, 0f));
        var movWall = new WallSegment(Vector2.Zero, new Vector2(0f, 4000f));
        movWall.End = new Vector2(1000f, 4000f);

        bool ok = WallThirtyFortyFiftyService.TryApply(
            refWall,
            movWall,
            300f,
            400f,
            500f,
            out float angle);

        Assert.True(ok);
        Assert.InRange(angle, 89f, 91f);

        float applied = MathHelper.RadiansToDegrees(
            MathF.Atan2(movWall.Direction.Y, movWall.Direction.X));

        Assert.InRange(applied, 89f, 91f);
    }

    [Fact]
    public void TryFindAdjacentWall_RetornaOutraParedeNoCanto()
    {
        var walls = new List<WallSegment>
        {
            new(Vector2.Zero, new Vector2(3000f, 0f)),
            new(Vector2.Zero, new Vector2(0f, 3000f))
        };

        var adjacent = WallThirtyFortyFiftyService.TryFindAdjacentWall(walls[0], walls);

        Assert.NotNull(adjacent);
        Assert.Equal(walls[1].Id, adjacent!.Id);
    }
}
