using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class RoomCompartmentPersistenceTests
{
    [Fact]
    public void RoundTrip_CompartimentoEParede_PreservaVinculo()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3000, 0), 150, 2600, WallOrientation.Right)
        ]);

        RoomCompartmentService.EnsureInitialized(project.Room, project.Metadata);
        project.Room.Compartments[0].DisplayName = "Cozinha";
        project.Room.Walls[0].CompartmentId = project.Room.Compartments[0].Id;

        var document = ProjectPersistence.CreateFromProject(project);
        var path = Path.Combine(Path.GetTempPath(), $"proj-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(document, path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Single(loaded.Room.Compartments);
            Assert.Equal("Cozinha", loaded.Room.Compartments[0].DisplayName);
            Assert.Equal(loaded.Room.Compartments[0].Id, loaded.Room.Walls[0].CompartmentId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
