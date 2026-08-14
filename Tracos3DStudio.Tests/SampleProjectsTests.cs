using Xunit;

namespace Tracos3DStudio.Tests;

public class SampleProjectsTests
{
    [Fact]
    public void Quadrado5000Horario_GeraArquivoSample()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples",
            "quadrado-5000-horario.tracos"));

        SampleProjects.SaveQuadrado5000Horario(path);

        Assert.True(File.Exists(path));

        var document = ProjectPersistence.LoadFromFile(path);
        var project = ProjectPersistence.LoadProject(document);

        Assert.Equal(4, project.Room.Walls.Count);
        Assert.True(project.Room.IsClosed);
        Assert.NotNull(project.Room.Floor);
        Assert.Equal(4, project.Room.Floor!.Points.Count);

        foreach (var wall in project.Room.Walls)
        {
            Assert.InRange(
                WallInnerFaceService.GetDisplayReferenceLength(wall, project.Room.Walls),
                SampleProjects.Quadrado5000SideMm - 2f,
                SampleProjects.Quadrado5000SideMm + 2f);
        }
    }

    [Fact]
    public void Quadrado5000ComParticaoMovel_GeraAmbienteComParedeMovel()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples",
            "quadrado-5000-particao-movel.tracos"));

        SampleProjects.SaveQuadrado5000ComParticaoMovel(path);

        Assert.True(File.Exists(path));

        var document = ProjectPersistence.LoadFromFile(path);
        var project = ProjectPersistence.LoadProject(document);

        Assert.Equal(5, project.Room.Walls.Count);
        Assert.True(project.Room.IsClosed);
        Assert.NotNull(project.Room.Floor);

        var movable = project.Room.Walls.Where(w => w.IsMovable).ToList();
        Assert.Single(movable);

        float length = movable[0].Length;
        Assert.InRange(length, SampleProjects.ParticaoInternaComprimento - 2f, SampleProjects.ParticaoInternaComprimento + 2f);
    }
}
