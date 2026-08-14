using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallManualDimensionServiceTests
{
    [Fact]
    public void TryCreateLinear_5000mmEntreCantos()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(5000, 0);
        var hint = new Vector2(2500, 500);

        var dim = WallManualDimensionService.TryCreateLinear(a, b, hint);

        Assert.NotNull(dim);
        Assert.Equal(WallManualDimensionKind.Linear, dim!.Kind);
        Assert.InRange(dim.DisplayValue, 4998f, 5002f);
    }

    [Fact]
    public void TryCreateAngular_90DegreesNoVertice()
    {
        var vertex = new Vector2(0, 0);
        var a = new Vector2(1000, 0);
        var c = new Vector2(0, 1000);
        var hint = new Vector2(400, 400);

        var dim = WallManualDimensionService.TryCreateAngular(a, vertex, c, hint);

        Assert.NotNull(dim);
        Assert.Equal(WallManualDimensionKind.Angular, dim!.Kind);
        Assert.InRange(dim.DisplayValue, 89f, 91f);
    }

    [Fact]
    public void ComputeAngleDegrees_RetaRetorna180()
    {
        float angle = WallManualDimensionService.ComputeAngleDegrees(
            new Vector2(0, 0),
            new Vector2(-1000, 0),
            new Vector2(1000, 0));

        Assert.InRange(angle, 179f, 181f);
    }

    [Fact]
    public void TryPick_SelecionaCotaProxima()
    {
        var dim = new WallManualDimension
        {
            Kind = WallManualDimensionKind.Linear,
            PointA = new Vector2(0, 0),
            PointB = new Vector2(3000, 0),
            DimStart = new Vector2(0, 280),
            DimEnd = new Vector2(3000, 280),
            DisplayValue = 3000
        };

        bool picked = WallManualDimensionService.TryPick(
            new Vector2(1500, 280),
            [dim],
            out Guid id);

        Assert.True(picked);
        Assert.Equal(dim.Id, id);
    }

    [Fact]
    public void SnapPoint_AncoraNoVerticeDaParede()
    {
        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            [
                new Vector2(0, 0),
                new Vector2(5000, 0),
                new Vector2(5000, 5000),
                new Vector2(0, 5000)
            ],
            isClosed: true,
            thickness: 150f,
            height: 2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        Vector2 snapped = WallManualDimensionService.SnapPoint(new Vector2(12, 8), walls);

        Assert.True((snapped - new Vector2(0, 0)).Length < 50f);
    }
}
