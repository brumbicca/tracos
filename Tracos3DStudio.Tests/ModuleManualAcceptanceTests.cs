using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Fixtures e aceite para manual de colisão e dormitório.</summary>
public sealed class ModuleManualAcceptanceTests
{
    [Fact]
    public void DormitorioQuadrado_TresModulos_PersisteERestaura()
    {
        var project = BuildDormitorioQuadradoProject();
        Assert.Equal(3, project.Modules.Count);

        var path = Path.Combine(Path.GetTempPath(), $"dormitorio-{Guid.NewGuid()}.tracos");
        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.True(restored.Room.IsClosed);
            Assert.Equal(3, restored.Modules.Count);
            Assert.Contains(restored.Modules, m => m.DefinitionId == "guarda-roupa-2p");
            Assert.Contains(restored.Modules, m => m.DefinitionId == "criado-mudo");
            Assert.Contains(restored.Modules, m => m.DefinitionId == "comoda-4g");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ColisaoModulos_DetectaSobreposicao()
    {
        var project = BuildCollisionModulesProject();
        var colliding = ModuleCollisionService.FindCollidingModuleIds(project.Modules);

        Assert.Equal(2, colliding.Count);
    }

    [Fact]
    public void ExportFixture_DormitorioQuadrado_ParaTesteVisual()
    {
        var project = BuildDormitorioQuadradoProject();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples", "dormitorio-quadrado.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ExportFixture_ColisaoModulos_ParaTesteVisual()
    {
        var project = BuildCollisionModulesProject();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples", "colisao-modulos.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
        Assert.True(File.Exists(path));
    }

    public static Project BuildDormitorioQuadradoProject()
    {
        var project = new Project();
        project.Metadata.Name = "Dormitório quadrado";

        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(4000, 0), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(4000, 0), new Vector2(4000, 3500), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(4000, 3500), new Vector2(0, 3500), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(0, 3500), new Vector2(0, 0), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            }
        ]);

        PlaceModuleOnWall(project, "guarda-roupa-2p", new Vector2(800, 400));
        PlaceModuleOnWall(project, "criado-mudo", new Vector2(2800, 400));
        PlaceModuleOnWall(project, "comoda-4g", new Vector2(2800, 2200));

        return project;
    }

    public static Project BuildCollisionModulesProject()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.Name = "Colisão módulos";

        var first = project.Modules.First(m => m.DefinitionId == "balcao-2-portas");
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var duplicate = project.AddModule("balcao-2-portas", first.Position);
        duplicate.ApplyPlacement(
            first.Position,
            first.RotationYDegrees,
            definition,
            first.AttachedWallId,
            first.DistanceAlongWall);

        return project;
    }

    private static void PlaceModuleOnWall(Project project, string definitionId, Vector2 floorClick)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);
        var placement = ModulePlacementService.Compute(
            floorClick,
            project.Room.Walls,
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth);

        var instance = project.AddModule(definitionId, placement.Position);
        instance.ApplyPlacement(
            placement.Position,
            placement.RotationYDegrees,
            definition,
            placement.WallId,
            placement.DistanceAlongWall);
    }
}
