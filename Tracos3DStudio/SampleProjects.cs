using System.IO;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class SampleProjects
{
    public const float Quadrado5000SideMm = 5000f;

    public static List<WallSegment> BuildQuadrado5000Horario(
        float thickness = 150f,
        float height = 2600f,
        WallMeasureSide measureSide = WallMeasureSide.Interior)
    {
        var draft = new WallDraft
        {
            Thickness = thickness,
            Height = height,
            MeasureSide = measureSide,
            Orientation = WallOrientation.Right
        };

        draft.Start(Vector2.Zero);

        Vector2[] directions =
        [
            Vector2.UnitX,
            new(0f, 1f),
            new(-1f, 0f),
            new(0f, -1f)
        ];

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 direction = Vector2.Normalize(directions[i]);
            bool closing = i == directions.Length - 1;
            Vector2 point = closing
                ? draft.Points[0]
                : draft.Points[^1] + direction * Quadrado5000SideMm;

            draft.ConfirmPoint(point, Quadrado5000SideMm);
        }

        return draft.BuildWalls();
    }

    public static void SaveQuadrado5000Horario(string filePath)
    {
        var project = new Project
        {
            Metadata = new ProjectMetadata { Name = "Quadrado 5000 horário" }
        };

        project.Room.SetWalls(BuildQuadrado5000Horario());
        project.Room.RebuildAutomaticFloor();

        var document = ProjectPersistence.CreateFromProject(project);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        ProjectPersistence.SaveToFile(document, filePath);
    }

    public const float ParticaoInternaOffsetY = 2000f;
    public const float ParticaoInternaStartX = 1000f;
    public const float ParticaoInternaEndX = 4000f;
    public const float ParticaoInternaComprimento = ParticaoInternaEndX - ParticaoInternaStartX;

    public static List<WallSegment> BuildParticaoInternaHorizontalMovel(
        float thickness = 150f,
        float height = 2600f,
        WallMeasureSide measureSide = WallMeasureSide.Interior)
    {
        var draft = new WallDraft
        {
            Thickness = thickness,
            Height = height,
            MeasureSide = measureSide,
            Orientation = WallOrientation.Right
        };

        draft.Start(new Vector2(ParticaoInternaStartX, ParticaoInternaOffsetY));
        draft.ConfirmPoint(new Vector2(ParticaoInternaEndX, ParticaoInternaOffsetY), ParticaoInternaComprimento);

        var walls = draft.BuildWalls();

        foreach (var wall in walls)
            wall.IsMovable = true;

        return walls;
    }

    public static Project BuildQuadrado5000ComParticaoMovel()
    {
        var project = new Project
        {
            Metadata = new ProjectMetadata { Name = "Quadrado 5000 + partição móvel" }
        };

        project.Room.SetWalls(BuildQuadrado5000Horario());
        project.Room.RebuildAutomaticFloor();
        project.Room.AppendPartitionWalls(BuildParticaoInternaHorizontalMovel());

        return project;
    }

    public static void SaveQuadrado5000ComParticaoMovel(string filePath)
    {
        var project = BuildQuadrado5000ComParticaoMovel();
        var document = ProjectPersistence.CreateFromProject(project);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        ProjectPersistence.SaveToFile(document, filePath);
    }
}
