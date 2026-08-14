using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallReferenceServiceTests
{
    [Fact]
    public void TryPickInnerFace_AceitaPontoProximoDaFaceInterna()
    {
        var walls = SampleProjects.BuildQuadrado5000Horario();
        var south = walls[3];
        var inner = WallInnerFaceService.GetInnerFace(south, walls);
        Vector2 probe = inner.PointAtDistance(inner.Length * 0.5f) + inner.InteriorNormal * 80f;

        bool ok = WallReferenceService.TryPickInnerFace(probe, walls, out var pick);

        Assert.True(ok);
        Assert.Equal(south.Id, pick.WallId);
        Assert.True((pick.AnchorOnInnerFace - inner.PointAtDistance(inner.Length * 0.5f)).Length < 120f);
    }

    [Fact]
    public void Offset1000mm_ParaInterior_GeraCantoReferenciaCorreto()
    {
        var walls = SampleProjects.BuildQuadrado5000Horario();
        var south = walls[3];
        var inner = WallInnerFaceService.GetInnerFace(south, walls);
        var pick = new WallReferencePick
        {
            WallId = south.Id,
            AnchorOnInnerFace = inner.PointAtDistance(inner.Length * 0.5f),
            InteriorNormal = inner.InteriorNormal,
            WallDirection = inner.Direction
        };

        float offset = 1000f;
        Vector2 start = WallReferenceService.ComputeDraftStartReferenceCorner(pick, offset);
        Vector2 expected = pick.AnchorOnInnerFace + inner.InteriorNormal * offset;

        Assert.True(Geometry2D.AlmostEqual(start, expected, 1f));
        Assert.InRange(WallReferenceService.ComputeSignedOffset(pick, start), offset - 1f, offset + 1f);
    }

    [Fact]
    public void AppendPartitionWalls_PreservaPisoEmAmbienteFechado()
    {
        var room = new Room();
        room.SetWalls(SampleProjects.BuildQuadrado5000Horario());
        room.RebuildAutomaticFloor();

        Assert.True(room.IsClosed);
        Assert.NotNull(room.Floor);

        var partitionDraft = new WallDraft
        {
            Thickness = 150f,
            Height = 2600f,
            MeasureSide = WallMeasureSide.Interior
        };

        partitionDraft.Start(new Vector2(1000f, 1000f));
        partitionDraft.ConfirmPoint(new Vector2(4000f, 1000f), 3000f);

        room.AppendPartitionWalls(partitionDraft.BuildWalls());

        Assert.True(room.IsClosed);
        Assert.NotNull(room.Floor);
        Assert.Equal(5, room.Walls.Count);
    }

    [Fact]
    public void TryPickInnerFace_ParticaoInternaDoSample()
    {
        var project = SampleProjects.BuildQuadrado5000ComParticaoMovel();
        var partition = project.Room.Walls.First(w => w.IsMovable);
        var inner = WallInnerFaceService.GetInnerFace(partition, project.Room.Walls);
        Vector2 probe = inner.PointAtDistance(inner.Length * 0.5f);

        bool ok = WallReferenceService.TryPickInnerFace(probe, project.Room.Walls, out var pick);

        Assert.True(ok);
        Assert.Equal(partition.Id, pick.WallId);
    }
}
