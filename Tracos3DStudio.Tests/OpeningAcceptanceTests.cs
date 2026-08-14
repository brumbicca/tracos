using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Aceite portas e janelas (Fase 1.2).</summary>
public sealed class OpeningAcceptanceTests
{
    [Fact]
    public void Quadrado5000_PortaEJanela_PersisteERestaura()
    {
        var project = BuildQuadradoWithDoorAndWindow();
        var path = Path.Combine(Path.GetTempPath(), $"aberturas-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.True(restored.Room.IsClosed);
            int openingCount = restored.Room.Walls.Sum(w => w.Openings.Count);
            Assert.Equal(2, openingCount);

            var doorWall = restored.Room.Walls.First(w => w.Openings.Any(o => o.Type == OpeningType.Door));
            var windowWall = restored.Room.Walls.First(w => w.Openings.Any(o => o.Type == OpeningType.Window));

            var door = doorWall.Openings.Single(o => o.Type == OpeningType.Door);
            Assert.Equal(800f, door.Width);
            Assert.Equal(2100f, door.Height);
            Assert.Equal(0f, door.SillHeight);

            var window = windowWall.Openings.Single(o => o.Type == OpeningType.Window);
            Assert.Equal(1200f, window.Width);
            Assert.Equal(1000f, window.Height);
            Assert.Equal(1100f, window.SillHeight);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportFixture_QuadradoPortaJanela_ParaTesteVisual()
    {
        var project = BuildQuadradoWithDoorAndWindow();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples", "quadrado-5000-porta-janela.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
        Assert.True(File.Exists(path));
    }

    public static Project BuildQuadradoWithDoorAndWindow()
    {
        var project = new Project();
        project.Metadata.Name = "Quadrado 5000 porta+janela";

        project.Room.SetWalls([
            new WallSegment(new Vector2(-150, -150), new Vector2(5150, -150), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(5150, -150), new Vector2(5150, 5150), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(5150, 5150), new Vector2(-150, 5150), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(new Vector2(-150, 5150), new Vector2(-150, -150), 150, 2600, WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            }
        ]);

        var southWall = project.Room.Walls[0];
        var eastWall = project.Room.Walls[1];

        var door = WallOpeningPlacement.CreateOpening(OpeningType.Door, 2000f);
        Assert.True(WallOpeningPlacement.TryAddOpening(southWall, door));

        var window = WallOpeningPlacement.CreateOpening(OpeningType.Window, 1500f);
        Assert.True(WallOpeningPlacement.TryAddOpening(eastWall, window));

        return project;
    }
}
