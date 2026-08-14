using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallSegmentationServiceTests
{
    [Fact]
    public void TrySplit_5000NaMetade_DoisSegmentos2500()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f))
        {
            HeightStart = 2600f,
            HeightEnd = 2600f,
            Thickness = 150f
        };

        bool ok = WallSegmentationService.TrySplit(wall, 2500f, out var segments);

        Assert.True(ok);
        Assert.Equal(2, segments.Count);
        Assert.InRange(segments[0].Length, 2498f, 2502f);
        Assert.InRange(segments[1].Length, 2498f, 2502f);
        Assert.Equal(wall.HeightStart, segments[0].HeightStart);
        Assert.Equal(wall.HeightEnd, segments[1].HeightEnd);
    }

    [Fact]
    public void TrySplit_PeDireitoVariavel_InterpolaAlturaNoCorte()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(4000f, 0f))
        {
            HeightStart = 2400f,
            HeightEnd = 2800f
        };

        bool ok = WallSegmentationService.TrySplit(wall, 2000f, out var segments);

        Assert.True(ok);
        Assert.Equal(2600f, segments[0].HeightEnd);
        Assert.Equal(2600f, segments[1].HeightStart);
        Assert.Equal(2800f, segments[1].HeightEnd);
    }

    [Fact]
    public void TrySplit_PortaNoPrimeiroSegmento_MantémAbertura()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f));
        wall.AddDoor(1000f, 800f, 2100f);

        bool ok = WallSegmentationService.TrySplit(wall, 3000f, out var segments);

        Assert.True(ok);
        Assert.Single(segments[0].Openings);
        Assert.Empty(segments[1].Openings);
        Assert.Equal(1000f, segments[0].Openings[0].DistanceFromStart);
    }

    [Fact]
    public void TrySplit_CortePassaPelaPorta_Falha()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f));
        wall.AddDoor(2500f, 800f, 2100f);

        bool ok = WallSegmentationService.TrySplit(wall, 2500f, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ReassignModulesAfterSplit_AtualizaDistanciaNoSegundoTrecho()
    {
        var modules = new List<ModuleInstance>
        {
            new ModuleInstance
            {
                DefinitionId = "balcao-2-portas",
                AttachedWallId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DistanceAlongWall = 1000f
            },
            new ModuleInstance
            {
                DefinitionId = "balcao-2-portas",
                AttachedWallId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DistanceAlongWall = 3500f
            }
        };

        var firstId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var secondId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        WallSegmentationService.ReassignModulesAfterSplit(
            modules,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            firstId,
            secondId,
            2500f);

        Assert.Equal(firstId, modules[0].AttachedWallId);
        Assert.Equal(1000f, modules[0].DistanceAlongWall);
        Assert.Equal(secondId, modules[1].AttachedWallId);
        Assert.Equal(1000f, modules[1].DistanceAlongWall);
    }
}
