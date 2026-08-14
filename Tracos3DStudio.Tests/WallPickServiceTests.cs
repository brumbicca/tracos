using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallPickServiceTests
{
    [Fact]
    public void TryPickFloor_PertoDaParede_EncontraSegmento()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Thickness = 150f
        };

        bool found = WallPickService.TryPickFloor(
            new Vector2(1500, 80),
            new[] { wall },
            out Guid wallId,
            out float distanceAlong);

        Assert.True(found);
        Assert.Equal(wall.Id, wallId);
        Assert.InRange(distanceAlong, 1400f, 1600f);
    }

    [Fact]
    public void TryPickRay_RaioNaFaceDaParede_EncontraSegmento()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var origin = new Vector3(1500, 1300, 800);
        var direction = Vector3.Normalize(new Vector3(0, 0, -1));

        bool found = WallPickService.TryPickRay(
            origin,
            direction,
            new[] { target },
            out Guid wallId,
            out float distanceAlong,
            out float hitDistance,
            out _);

        Assert.True(found);
        Assert.Equal(wall.Id, wallId);
        Assert.InRange(distanceAlong, 1400f, 1600f);
        Assert.True(hitDistance > 0);
    }

    [Fact]
    public void TryPickModuleInsertionFace_RaioNaFaceLateral_EncontraFaceInterna()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var origin = new Vector3(1500, 500, 800);
        var direction = Vector3.Normalize(new Vector3(0, 0, -1));

        bool found = WallPickService.TryPickModuleInsertionFace(
            origin,
            direction,
            new[] { target },
            out Guid wallId,
            out float distanceAlong,
            out Vector2 interiorNormal,
            out _);

        Assert.True(found);
        Assert.Equal(wall.Id, wallId);
        Assert.InRange(distanceAlong, 1400f, 1600f);
    }

    [Fact]
    public void TryPickRay_RaioNoTopoHorizontal_SelecionaGrupo()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var origin = new Vector3(1500, 3000, 75);
        var direction = Vector3.Normalize(new Vector3(0, -1, 0));

        bool found = WallPickService.TryPickRay(
            origin,
            direction,
            new[] { target },
            out Guid wallId,
            out _,
            out _,
            out bool hitTopFace);

        Assert.True(found);
        Assert.Equal(wall.Id, wallId);
        Assert.True(hitTopFace);
    }

    [Fact]
    public void TryPickRay_RaioNaFaceLateralBaixa_NaoSelecionaGrupo()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var origin = new Vector3(1500, 1300, 800);
        var direction = Vector3.Normalize(new Vector3(0, 0, -1));

        bool found = WallPickService.TryPickRay(
            origin,
            direction,
            new[] { target },
            out Guid wallId,
            out _,
            out _,
            out bool hitTopFace);

        Assert.True(found);
        Assert.Equal(wall.Id, wallId);
        Assert.False(hitTopFace);
    }

    [Fact]
    public void TryComputeFromScreenRay_VistaFrontal_PosicionaEncostadoNaParede()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var camera = new CameraController
        {
            ViewMode = CameraViewMode.Front,
            Target = new Vector3(1500, 850, 400),
            Distance = 5000f
        };
        camera.SetupForViewport(1280, 720, forceTopView: false);

        var result = ModulePlacementService.TryComputeFromScreenRay(
            640,
            360,
            1280,
            720,
            camera.View,
            camera.Projection,
            new[] { wall },
            new[] { target },
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth);

        Assert.NotNull(result);
        Assert.True(result.Value.SnappedToWall);
        Assert.Equal(wall.Id, result.Value.WallId);
        Assert.Equal(150f, result.Value.Position.Z, precision: 1);
        Assert.InRange(result.Value.Position.Y, 0f, 2600f - definition.DefaultHeight);
    }
}
