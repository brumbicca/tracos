using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class MaterialCopyServiceTests
{
    [Fact]
    public void TryReadMaterial_Modulo()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);
        module.MaterialId = "mdf-madeirado";

        var context = new MaterialApplicationContext { ModuleId = module.Id };

        Assert.True(MaterialCopyService.TryReadMaterial(
            project,
            context,
            out string? materialId,
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal("mdf-madeirado", materialId);
        Assert.Equal(MaterialApplicationTarget.Module, target);
    }

    [Fact]
    public void TryReadMaterial_FaceLivreParede()
    {
        var project = new Project();
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0));
        wall.InternalFaceMaterialId = "ceramica-bege";
        project.Room.SetWalls([wall]);

        var context = new MaterialApplicationContext
        {
            WallId = wall.Id,
            WallFace = FaceType.Internal
        };

        Assert.True(MaterialCopyService.TryReadMaterial(
            project,
            context,
            out string? materialId,
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal("ceramica-bege", materialId);
        Assert.Equal(MaterialApplicationTarget.WallFace, target);
    }

    [Fact]
    public void TryReadMaterial_SemMaterialRetornaErro()
    {
        var project = new Project();
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0));
        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        project.Room.SetWalls([wall]);

        var context = new MaterialApplicationContext
        {
            WallId = wall.Id,
            WallBandId = band!.Id
        };

        Assert.False(MaterialCopyService.TryReadMaterial(
            project,
            context,
            out _,
            out _,
            out string? error));

        Assert.NotNull(error);
    }

    [Fact]
    public void TryReadMaterialFromRayHit_Regiao()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0)) { Height = 2600f };
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        region!.MaterialId = "laminado-madeira";

        var project = new Project();
        project.Room.SetWalls([wall]);

        float along = (region.StartAlongMm + region.EndAlongMm) * 0.5f;
        float height = (region.BottomMm + region.TopMm) * 0.5f;

        var hit = new MaterialDropRayHit
        {
            Wall = wall,
            WallDistance = 100f,
            Along = along,
            Height = height,
            Face = FaceType.Internal
        };

        Assert.True(MaterialCopyService.TryReadMaterialFromRayHit(
            project,
            hit,
            out string? materialId,
            out _,
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal("laminado-madeira", materialId);
        Assert.Equal(MaterialApplicationTarget.WallRegion, target);
    }

    [Fact]
    public void TryCaptureToActive_DefineMaterialAtivo()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);
        module.MaterialId = "mdf-branco";

        MaterialApplicationService.ActiveMaterialId = MaterialCatalog.DefaultMaterialId;

        Assert.True(MaterialCopyService.TryCaptureToActive(
            project,
            new MaterialApplicationContext { ModuleId = module.Id },
            out _,
            out _));

        Assert.Equal("mdf-branco", MaterialApplicationService.ActiveMaterialId);
    }
}
