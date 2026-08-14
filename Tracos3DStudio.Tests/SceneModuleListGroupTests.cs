using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SceneModuleListGroupTests
{
    [Fact]
    public void BuildGroupedEntries_AgrupaModulosPorParede()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(5000, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(5000, 0), new Vector2(5000, 4000), 150, 2600, WallOrientation.Right)
        ]);

        RoomCompartmentService.EnsureInitialized(project.Room, project.Metadata);

        var wall1 = project.Room.Walls[0];
        var wall2 = project.Room.Walls[1];

        var moduleA = project.AddModule("balcao-2-portas", Vector3.Zero);
        moduleA.AttachedWallId = wall1.Id;
        moduleA.DistanceAlongWall = 1200f;

        var moduleB = project.AddModule("gaveteiro", Vector3.Zero);
        moduleB.AttachedWallId = wall1.Id;
        moduleB.DistanceAlongWall = 300f;

        var moduleC = project.AddModule("aereo", Vector3.Zero);
        moduleC.AttachedWallId = wall2.Id;
        moduleC.DistanceAlongWall = 800f;

        var entries = SceneModuleListService.BuildGroupedEntries(
            project.Modules,
            project.Room.Walls,
            project.Room.Compartments);

        Assert.Equal(6, entries.Count);
        Assert.Equal(SceneModuleListGroupKind.Compartment, ((SceneModuleListGroupEntry)entries[0]).Kind);
        Assert.Equal("Parede 1 — 4850 mm", ((SceneModuleListGroupEntry)entries[1]).GroupTitle);
        Assert.Equal("gaveteiro", ((SceneModuleListItem)entries[2]).Module.DefinitionId);
        Assert.Equal("balcao-2-portas", ((SceneModuleListItem)entries[3]).Module.DefinitionId);
        Assert.Equal("Parede 2 — 3850 mm", ((SceneModuleListGroupEntry)entries[4]).GroupTitle);
        Assert.Equal("aereo", ((SceneModuleListItem)entries[5]).Module.DefinitionId);
    }

    [Fact]
    public void BuildGroupedEntries_AgrupaPorComodoEParede()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(4000, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(4000, 0), new Vector2(4000, 3000), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(4000, 3000), new Vector2(8000, 3000), 150, 2600, WallOrientation.Right)
        ]);

        RoomCompartmentService.EnsureInitialized(project.Room, project.Metadata);
        var cozinha = project.Room.Compartments[0];
        cozinha.DisplayName = "Cozinha";

        var suite = RoomCompartmentService.AddCompartment(project.Room);
        suite.DisplayName = "Suíte";

        project.Room.Walls[0].CompartmentId = cozinha.Id;
        project.Room.Walls[1].CompartmentId = cozinha.Id;
        project.Room.Walls[2].CompartmentId = suite.Id;

        var cozinhaModule = project.AddModule("balcao-2-portas", Vector3.Zero);
        cozinhaModule.AttachedWallId = project.Room.Walls[0].Id;

        var suiteModule = project.AddModule("guarda-roupa-2p", Vector3.Zero);
        suiteModule.AttachedWallId = project.Room.Walls[2].Id;

        var entries = SceneModuleListService.BuildGroupedEntries(
            project.Modules,
            project.Room.Walls,
            project.Room.Compartments);

        Assert.Equal(6, entries.Count);
        Assert.Equal("Cômodo 1 — Cozinha", ((SceneModuleListGroupEntry)entries[0]).GroupTitle);
        Assert.Equal("Parede 1 — 3850 mm", ((SceneModuleListGroupEntry)entries[1]).GroupTitle);
        Assert.Equal("Cômodo 2 — Suíte", ((SceneModuleListGroupEntry)entries[3]).GroupTitle);
        Assert.Equal("Parede 3 — 4150 mm", ((SceneModuleListGroupEntry)entries[4]).GroupTitle);
    }

    [Fact]
    public void BuildGroupedEntries_SemParede_AgrupaNoFinal()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3000, 0), 150, 2600, WallOrientation.Right)
        ]);

        RoomCompartmentService.EnsureInitialized(project.Room, project.Metadata);

        var attached = project.AddModule("balcao-2-portas", Vector3.Zero);
        attached.AttachedWallId = project.Room.Walls[0].Id;

        var free = project.AddModule("gaveteiro", Vector3.Zero);
        free.AttachedWallId = null;

        var entries = SceneModuleListService.BuildGroupedEntries(
            project.Modules,
            project.Room.Walls,
            project.Room.Compartments);

        Assert.Equal(5, entries.Count);
        Assert.Equal(WallLabelService.UnattachedGroupTitle, ((SceneModuleListGroupEntry)entries[3]).GroupTitle);
        Assert.Equal("gaveteiro", ((SceneModuleListItem)entries[4]).Module.DefinitionId);
    }

    [Fact]
    public void BuildGroupedEntries_ListaVazia_QuandoSemModulos()
    {
        var entries = SceneModuleListService.BuildGroupedEntries(
            Array.Empty<ModuleInstance>(),
            [new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))],
            [new RoomCompartment { DisplayName = "Cômodo 1" }]);

        Assert.Empty(entries);
    }
}
