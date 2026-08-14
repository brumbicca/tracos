using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class MaterialApplicationServiceTests
{
    [Fact]
    public void GetFilteredOptions_TodosIncluiModulosEPisos()
    {
        var all = MaterialApplicationService.GetFilteredOptions(MaterialListFilter.All);
        var modules = MaterialApplicationService.GetFilteredOptions(MaterialListFilter.Modules);
        var floors = MaterialApplicationService.GetFilteredOptions(MaterialListFilter.Floors);

        Assert.True(all.Count >= modules.Count + floors.Count);
        Assert.Equal(MaterialCatalog.All.Count, modules.Count);
        Assert.Equal(FloorMaterialCatalog.All.Count, floors.Count);
        Assert.Contains(all, o => o.Id == MaterialCatalog.DefaultMaterialId);
        Assert.Contains(all, o => o.Id == FloorMaterialCatalog.DefaultMaterialId);
    }

    [Fact]
    public void TryApplyToModule_AceitaMaterialModulo()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);

        Assert.True(MaterialApplicationService.TryApplyToModule(
            project, module.Id, "mdf-madeirado", out _));

        Assert.Equal("mdf-madeirado", module.MaterialId);
        Assert.Equal("mdf-madeirado", MaterialApplicationService.ActiveMaterialId);
    }

    [Fact]
    public void TryApplyToModule_RejeitaMaterialPiso()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);
        module.MaterialId = MaterialCatalog.DefaultMaterialId;

        Assert.False(MaterialApplicationService.TryApplyToModule(
            project, module.Id, FloorMaterialCatalog.DefaultMaterialId, out string? error));

        Assert.NotNull(error);
        Assert.Equal(MaterialCatalog.DefaultMaterialId, module.MaterialId);
    }

    [Fact]
    public void TryApplyMaterial_PriorizaModuloSobrePiso()
    {
        var project = new Project();
        var module = project.AddModule("balcao-2-portas", Vector3.Zero);

        var context = new MaterialApplicationContext
        {
            ModuleId = module.Id,
            FloorSelected = true
        };

        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project,
            context,
            "mdf-madeirado",
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal(MaterialApplicationTarget.Module, target);
        Assert.Equal("mdf-madeirado", module.MaterialId);
    }

    [Fact]
    public void TryApplyMaterial_RegiaoParede()
    {
        var project = new Project();
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0));
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        project.Room.SetWalls([wall]);

        var context = new MaterialApplicationContext
        {
            WallId = wall.Id,
            WallRegionId = region!.Id
        };

        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project,
            context,
            "ceramica-bege",
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal(MaterialApplicationTarget.WallRegion, target);
        Assert.Equal("ceramica-bege", wall.Regions[0].MaterialId);
    }

    [Fact]
    public void TryApplyMaterial_FaceLivreParede()
    {
        var project = new Project();
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0));
        project.Room.SetWalls([wall]);

        var context = new MaterialApplicationContext
        {
            WallId = wall.Id,
            WallFace = FaceType.Internal
        };

        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project,
            context,
            "ceramica-bege",
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal(MaterialApplicationTarget.WallFace, target);
        Assert.Equal("ceramica-bege", wall.InternalFaceMaterialId);
    }

    [Fact]
    public void TryResolveEffectiveContext_ModoFaceIgnoraRegiaoSelecionada()
    {
        MaterialApplicationService.ApplicationMode = MaterialApplicationMode.WallFace;

        try
        {
            var context = new MaterialApplicationContext
            {
                WallId = Guid.NewGuid(),
                WallRegionId = Guid.NewGuid(),
                WallFace = FaceType.External
            };

            Assert.True(MaterialApplicationService.TryResolveEffectiveContext(
                context,
                out var effective,
                out _));

            Assert.Equal(FaceType.External, effective.WallFace);
            Assert.Null(effective.WallRegionId);
        }
        finally
        {
            MaterialApplicationService.ApplicationMode = MaterialApplicationMode.Auto;
        }
    }

    [Fact]
    public void TryApplyMaterial_SemAlvoDefineAtivo()
    {
        MaterialApplicationService.ActiveMaterialId = MaterialCatalog.DefaultMaterialId;

        Assert.True(MaterialApplicationService.TryApplyMaterial(
            project: new Project(),
            context: new MaterialApplicationContext(),
            materialId: "laminado-madeira",
            out MaterialApplicationTarget target,
            out _));

        Assert.Equal(MaterialApplicationTarget.None, target);
        Assert.Equal("laminado-madeira", MaterialApplicationService.ActiveMaterialId);
    }
}
