using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Portas e janelas em parede curva (G2 + aberturas).</summary>
public sealed class CurvedWallOpeningTests
{
    [Fact]
    public void CanPlace_PortaEmParedeCurva5000Flecha400_RetornaTrue()
    {
        var wall = CreateCurvedWall5000Flecha400();
        float arcLength = wall.Length;
        float clickAt = arcLength * 0.5f;
        var door = WallOpeningPlacement.CreateOpening(OpeningType.Door, clickAt);

        Assert.True(arcLength > 5000f);
        Assert.True(WallOpeningPlacement.CanPlace(wall, door));
    }

    [Fact]
    public void CurvedWall_Porta_PersisteERestaura()
    {
        var project = BuildCurvedWallWithDoor();
        var path = Path.Combine(Path.GetTempPath(), $"curva-porta-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            var wall = restored.Room.Walls.Single();
            Assert.InRange(wall.FlechaMm, 398f, 402f);

            var door = wall.Openings.Single();
            Assert.Equal(OpeningType.Door, door.Type);
            Assert.Equal(800f, door.Width);
            Assert.Equal(2100f, door.Height);
            Assert.True(door.DistanceFromStart >= 50f);
            Assert.True(door.EndDistance <= wall.Length - 50f);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportFixture_CurvaPorta_ParaTesteVisual()
    {
        var project = BuildCurvedWallWithDoor();
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples", "curva-porta.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
        Assert.True(File.Exists(path));
    }

    public static WallSegment CreateCurvedWall5000Flecha400()
    {
        return new WallSegment(Vector2.Zero, new Vector2(5000f, 0f), 150, 2600, WallOrientation.Right)
        {
            FlechaMm = 400f,
            MeasureSide = WallMeasureSide.Interior
        };
    }

    public static Project BuildCurvedWallWithDoor()
    {
        var project = new Project();
        project.Metadata.Name = "Parede curva com porta";

        var wall = CreateCurvedWall5000Flecha400();
        project.Room.SetWalls([wall]);

        float clickAt = wall.Length * 0.5f;
        var door = WallOpeningPlacement.CreateOpening(OpeningType.Door, clickAt);
        Assert.True(WallOpeningPlacement.TryAddOpening(wall, door));

        return project;
    }
}
