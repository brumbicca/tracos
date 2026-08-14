using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallSegmentTests
{
    [Fact]
    public void Length_Parede2000mm_RetornaComprimentoCorreto()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(2000, 0));

        Assert.Equal(2000f, wall.Length, precision: 1);
    }

    [Fact]
    public void AddDoor_AdicionaAberturaNaParede()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0));

        wall.AddDoor(500f, 800f, 2100f);

        Assert.Single(wall.Openings);
        Assert.Equal(OpeningType.Door, wall.Openings[0].Type);
        Assert.Equal(800f, wall.Openings[0].Width);
    }

    [Fact]
    public void GetPointAtDistance_RetornaPontoAoLongoDaParede()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(2000, 0));

        var point = wall.GetPointAtDistance(500f);

        Assert.Equal(500f, point.X, precision: 1);
        Assert.Equal(0f, point.Y, precision: 1);
    }
}
