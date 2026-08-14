using Xunit;

namespace Tracos3DStudio.Tests;

public class WallOpeningPlacementTests
{
    [Fact]
    public void CanPlace_PortaPadraoEmParede3000_RetornaTrue()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(3000, 0));
        var opening = WallOpeningPlacement.CreateOpening(OpeningType.Door, 1100f);

        Assert.True(WallOpeningPlacement.CanPlace(wall, opening));
    }

    [Fact]
    public void CanPlace_PortaSobreposta_RetornaFalse()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(4000, 0));
        var first = WallOpeningPlacement.CreateOpening(OpeningType.Door, 500f);
        var second = WallOpeningPlacement.CreateOpening(OpeningType.Door, 900f);

        WallOpeningPlacement.TryAddOpening(wall, first);

        Assert.False(WallOpeningPlacement.CanPlace(wall, second));
    }

    [Fact]
    public void TryAddOpening_JanelaComPeitoril_ValidaAlturaDaParede()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(3000, 0))
        {
            Height = 2600f
        };

        var opening = WallOpeningPlacement.CreateOpening(OpeningType.Window, 600f);

        Assert.True(WallOpeningPlacement.TryAddOpening(wall, opening));
        Assert.Single(wall.Openings);
        Assert.Equal(OpeningType.Window, wall.Openings[0].Type);
        Assert.Equal(1100f, wall.Openings[0].SillHeight);
    }

    [Fact]
    public void ComputeStartDistance_CentralizaAberturaNoClique()
    {
        float start = WallOpeningPlacement.ComputeStartDistance(1500f, 800f, 3000f);

        Assert.Equal(1100f, start);
    }

    [Fact]
    public void MinWallLengthForStandardDoor_Retorna900mm()
    {
        Assert.Equal(900f, WallOpeningPlacement.MinWallLengthForStandardDoor);
    }

    [Fact]
    public void OverlapsWith_AberturasAdjacentes_RetornaFalse()
    {
        var first = WallOpening.Door(500f, 800f, 2100f);
        var second = WallOpening.Door(1300f, 800f, 2100f);

        Assert.False(first.OverlapsWith(second));
    }
}
