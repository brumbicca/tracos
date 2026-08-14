using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class Phase2AcceptanceTests
{
    [Fact]
    public void CozinhaEmL_QuatroTiposModulo_PersisteERestaura()
    {
        var project = BuildKitchenLProject();

        Assert.Equal(4, project.Modules.Count);
        Assert.Contains(project.Modules, m => m.DefinitionId == "balcao-2-portas");
        Assert.Contains(project.Modules, m => m.DefinitionId == "balcao-3-portas");
        Assert.Contains(project.Modules, m => m.DefinitionId == "gaveteiro");
        Assert.Contains(project.Modules, m => m.DefinitionId == "aereo");

        var path = Path.Combine(Path.GetTempPath(), $"fase2-{Guid.NewGuid()}.tracos");

        try
        {
            var document = ProjectPersistence.CreateFromProject(project);
            ProjectPersistence.SaveToFile(document, path);

            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.True(restored.Room.IsClosed);
            Assert.Equal(4, restored.Modules.Count);
            Assert.Equal(4, restored.Modules.Select(m => m.DefinitionId).Distinct().Count());

            foreach (var original in project.Modules)
            {
                var loaded = restored.Modules.Single(m => m.Id == original.Id);
                Assert.Equal(original.DefinitionId, loaded.DefinitionId);
                Assert.Equal(original.Width, loaded.Width);
                Assert.Equal(original.Position.X, loaded.Position.X, precision: 1);
                Assert.Equal(original.Position.Y, loaded.Position.Y, precision: 1);
                Assert.Equal(original.RotationYDegrees, loaded.RotationYDegrees, precision: 1);
                Assert.True(loaded.Mesh.Vertices.Count > 0);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportKitchenLFixture_ParaTesteVisual()
    {
        var project = BuildKitchenLProject();
        var document = ProjectPersistence.CreateFromProject(project);
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "fase-2-cozinha-L.tracos"));

        ProjectPersistence.SaveToFile(document, path);
        Assert.True(File.Exists(path));
    }

    public static Project BuildKitchenLProject()
    {
        var project = new Project();
        project.Metadata.Name = "Cozinha em L";

        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3500, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3500, 0), new Vector2(3500, 2500), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3500, 2500), new Vector2(0, 2500), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(0, 2500), new Vector2(0, 0), 150, 2600, WallOrientation.Right)
        ]);

        PlaceModuleOnWall(project, "balcao-2-portas", new Vector2(500, 500));
        PlaceModuleOnWall(project, "balcao-3-portas", new Vector2(1800, 500));
        PlaceModuleOnWall(project, "gaveteiro", new Vector2(3100, 900));
        PlaceModuleOnWall(project, "aereo", new Vector2(3100, 1600));

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
