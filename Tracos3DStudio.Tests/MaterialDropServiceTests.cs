using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class MaterialDropServiceTests
{
    [Fact]
    public void PickRegionAt_EscolheRegiaoQueContemPonto()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);

        float along = (region!.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var picked = MaterialDropService.PickRegionAt(
            wall,
            FaceType.Internal,
            along,
            height);

        Assert.NotNull(picked);
        Assert.Equal(region.Id, picked!.Id);
    }

    [Fact]
    public void PickBandAt_EscolheFaixaHorizontal()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);

        var picked = MaterialDropService.PickBandAt(wall, 2500f, 2300f);

        Assert.NotNull(picked);
        Assert.Equal(band!.Id, picked.Id);
    }

    [Fact]
    public void PickBandAt_EscolheFaixaVertical()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddVerticalBand(wall, 1900f, 3100f, out var band, out _);

        var picked = MaterialDropService.PickBandAt(wall, 2500f, 1300f);

        Assert.NotNull(picked);
        Assert.Equal(band!.Id, picked.Id);
    }

    [Fact]
    public void TryResolveTarget_ModoFaixaIgnoraFaceLivre()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = 2500f,
            Height = 2300f,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            MaterialApplicationMode.WallBand,
            out var context,
            out var kind));

        Assert.Equal(MaterialApplicationTarget.WallBand, kind);
        Assert.Equal(band!.Id, context.WallBandId);
        Assert.Null(context.WallFace);
    }

    [Fact]
    public void TryResolveTarget_ModoFaixaFalhaForaDaFaixa()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddDefaultUpperBand(wall, out _, out _);

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = 2500f,
            Height = 500f,
            Face = FaceType.Internal
        };

        Assert.False(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            MaterialApplicationMode.WallBand,
            out _,
            out _));
    }

    [Fact]
    public void TryApplyMaterial_DropEmFaixa_AtualizaMaterialId()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        var project = CreateProjectWithWall(wall);

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = 2500f,
            Height = 2300f,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            project,
            hit,
            MaterialApplicationMode.WallBand,
            out var context,
            out var kind));
        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project,
            context,
            "mdf-madeirado",
            out var applied,
            out _));

        Assert.Equal(MaterialApplicationTarget.WallBand, applied);
        Assert.Equal("mdf-madeirado", wall.Bands.First(b => b.Id == band!.Id).MaterialId);
    }

    [Fact]
    public void TryResolveTarget_RegiaoTemPrioridadeSobreFaixa()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallBandService.TryAddDefaultUpperBand(wall, out _, out _);
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);

        float along = (region!.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = along,
            Height = height,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            out var context,
            out var kind));

        Assert.Equal(MaterialApplicationTarget.WallRegion, kind);
        Assert.Equal(region.Id, context.WallRegionId);
    }

    [Fact]
    public void TryResolveTarget_ModuloNaFrenteDaParede()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);

        var hit = new MaterialDropRayHit
        {
            Module = module,
            ModuleDistance = 50f,
            Wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)),
            WallDistance = 200f
        };

        Assert.True(MaterialDropService.TryResolveTarget(project, hit, out var context, out var kind));
        Assert.Equal(MaterialApplicationTarget.Module, kind);
        Assert.Equal(module.Id, context.ModuleId);
    }

    [Fact]
    public void TryResolveTarget_PisoComRegiao()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)),
            new WallSegment(new Vector2(5000, 0), new Vector2(5000, 5000)),
            new WallSegment(new Vector2(5000, 5000), new Vector2(0, 5000)),
            new WallSegment(new Vector2(0, 5000), new Vector2(0, 0))
        ]);

        Assert.NotNull(project.Room.Floor);
        FloorZoneService.TryAddDefaultRectZone(project.Room.Floor!, out var zone, out _);

        var center = new Vector2(
            (zone!.MinX + zone.MaxX) * 0.5f,
            (zone.MinY + zone.MaxY) * 0.5f);

        var hit = new MaterialDropRayHit
        {
            HasFloorHit = true,
            FloorDistance = 300f,
            FloorPoint = center
        };

        Assert.True(MaterialDropService.TryResolveTarget(project, hit, out var context, out var kind));
        Assert.Equal(MaterialApplicationTarget.FloorZone, kind);
        Assert.Equal(zone.Id, context.FloorZoneId);
    }

    [Fact]
    public void TryResolveTarget_FaceLivreQuandoSemFaixaNemRegiao()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = 2500f,
            Height = 1300f,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            out var context,
            out var kind));

        Assert.Equal(MaterialApplicationTarget.WallFace, kind);
        Assert.Equal(wall.Id, context.WallId);
        Assert.Equal(FaceType.Internal, context.WallFace);
    }

    [Fact]
    public void TryResolveTarget_ModoFaceIgnoraRegiao()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);

        float along = (region!.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = along,
            Height = height,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            MaterialApplicationMode.WallFace,
            out var context,
            out var kind));

        Assert.Equal(MaterialApplicationTarget.WallFace, kind);
        Assert.Null(context.WallRegionId);
        Assert.Equal(FaceType.Internal, context.WallFace);
    }

    [Fact]
    public void TryResolveTarget_ModoRegiaoIgnoraFaceLivre()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);

        float along = (region!.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = along,
            Height = height,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(
            CreateProjectWithWall(wall),
            hit,
            MaterialApplicationMode.WallRegion,
            out var context,
            out var kind));

        Assert.Equal(MaterialApplicationTarget.WallRegion, kind);
        Assert.Equal(region.Id, context.WallRegionId);
    }

    [Fact]
    public void PickRegionAt_RegiaoRotacionada()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            2000f,
            3000f,
            1200f,
            2000f,
            out var region,
            out _);

        Assert.True(WallRegionService.TryRotateRegionByDelta(wall, region!.Id, 45f, out _));

        float cx = (region.StartAlongMm + region.EndAlongMm) * 0.5f;
        float cy = (region.BottomMm + region.TopMm) * 0.5f;

        var picked = MaterialDropService.PickRegionAt(wall, FaceType.Internal, cx, cy);

        Assert.NotNull(picked);
        Assert.Equal(region.Id, picked!.Id);
    }

    [Fact]
    public void TryApplyMaterial_DropEmRegiao_AtualizaMaterialId()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        var project = CreateProjectWithWall(wall);

        float along = (region!.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = along,
            Height = height,
            Face = FaceType.Internal
        };

        Assert.True(MaterialDropService.TryResolveTarget(project, hit, out var context, out var kind));
        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project,
            context,
            "mdf-branco",
            out var applied,
            out _));

        Assert.Equal(MaterialApplicationTarget.WallRegion, applied);
        Assert.Equal("mdf-branco", wall.Regions.First(r => r.Id == region.Id).MaterialId);
    }

    private static Project CreateProjectWithWall(WallSegment wall)
    {
        var project = new Project();
        project.Room.SetWalls([wall]);
        return project;
    }
}
