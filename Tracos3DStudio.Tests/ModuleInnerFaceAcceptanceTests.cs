using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Aceite módulo na face interna + cotas (Promob ambientação).</summary>
public sealed class ModuleInnerFaceAcceptanceTests
{
    [Fact]
    public void CozinhaEmL_ModulosEncostamNaFaceInterna()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var walls = project.Room.Walls;

        foreach (var module in project.Modules)
        {
            Assert.True(module.AttachedWallId.HasValue, $"{module.DefinitionId} sem parede vinculada.");

            var wall = walls.First(w => w.Id == module.AttachedWallId!.Value);
            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);

            Vector2 interiorNormal = ModulePlacementService.InteriorNormalFromRotation(module.RotationYDegrees);
            float halfWidth = module.Width * 0.5f;
            float leftAlong = module.DistanceAlongWall - halfWidth;
            Vector2 backCorner = ModulePlacementService.ComputeBackCornerOnInnerFace(
                wall, walls, leftAlong, interiorNormal);

            Assert.Equal(backCorner.X, module.Position.X, precision: 1);
            Assert.Equal(backCorner.Y, module.Position.Z, precision: 1);
            Assert.InRange(innerFace.Length, module.Width, 4000f);
        }
    }

    [Fact]
    public void CozinhaEmL_CotasHorizontaisFechamNaFaceInterna()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var walls = project.Room.Walls;

        foreach (var module in project.Modules)
        {
            if (!module.AttachedWallId.HasValue)
                continue;

            var wall = walls.First(w => w.Id == module.AttachedWallId.Value);
            var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);

            Assert.NotNull(cotas);

            var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
            float span = cotas!.Value.Anterior + module.Width + cotas.Value.Posterior;

            Assert.InRange(span, innerFace.Length - 1f, innerFace.Length + 1f);
        }
    }

    [Fact]
    public void CozinhaEmL_AplicarCotaAnterior_RefechaNaFaceInterna()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var module = project.Modules.First(m => m.DefinitionId == "balcao-2-portas");
        var wall = project.Room.Walls.First(w => w.Id == module.AttachedWallId!.Value);
        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        var innerFace = WallInnerFaceService.GetInnerFace(wall, project.Room.Walls);

        float targetAnterior = 400f;

        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, project.Room.Walls, definition, ModuleCotaAxis.Anterior, targetAnterior, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, project.Room.Walls);

        Assert.NotNull(cotas);
        Assert.Equal(targetAnterior, cotas!.Value.Anterior, precision: 1);

        float span = cotas.Value.Anterior + module.Width + cotas.Value.Posterior;
        Assert.InRange(span, innerFace.Length - 1f, innerFace.Length + 1f);
    }

    [Fact]
    public void FixtureCozinhaL_RegeneraComParedeVinculada()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "fase-2-cozinha-L.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);

        var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

        Assert.All(restored.Modules, m => Assert.True(m.AttachedWallId.HasValue));
    }

    [Fact]
    public void FixtureCozinhaL_ImportFromPreservaParedeEMesh()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "fase-2-cozinha-L.tracos"));

        var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));
        var project = new Project();
        project.ImportFrom(loaded);

        Assert.Equal(4, project.Modules.Count);
        Assert.All(project.Modules, m => Assert.True(m.AttachedWallId.HasValue));
        Assert.All(project.Modules, m => Assert.True(m.Mesh.Indices.Count > 0));
    }
}
