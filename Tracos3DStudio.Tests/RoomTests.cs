using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class RoomTests
{
    [Fact]
    public void AmbienteNovo_PisoIniciaSolidoSemGradeEAjustadoAosExtremosDasParedes()
    {
        var draft = new WallDraft
        {
            Thickness = 150f,
            Height = 2600f
        };
        draft.Start(new Vector2(0f, 5000f));
        draft.ConfirmPoint(new Vector2(0f, 0f));
        draft.ConfirmPoint(new Vector2(5000f, 0f));

        var room = new Room();
        room.SetWalls(draft.BuildWalls());
        room.SeedFloorFromBounds();

        Assert.NotNull(room.Floor);
        Assert.False(room.ShowFloorGrid);
        Assert.Equal(FloorMaterialCatalog.DefaultMaterialId, room.Floor!.DefaultMaterialId);
        Assert.Equal(FloorMaterialPattern.Solid, FloorMaterialCatalog.GetDefault().Pattern);
        Assert.True(room.TryGetFloorBounds(out var min, out var max));
        Assert.InRange(min.X, -1f, 1f);
        Assert.InRange(min.Y, -1f, 1f);
        Assert.InRange(max.X, 4999f, 5001f);
        Assert.InRange(max.Y, 4999f, 5001f);
    }

    [Fact]
    public void SetWalls_AmbienteFechado_GeraPisoAutomatico()
    {
        var walls = new List<WallSegment>
        {
            new(new Vector2(0, 0), new Vector2(2000, 0)),
            new(new Vector2(2000, 0), new Vector2(2000, 1500)),
            new(new Vector2(2000, 1500), new Vector2(0, 1500)),
            new(new Vector2(0, 1500), new Vector2(0, 0))
        };

        var room = new Room();
        room.SetWalls(walls);

        Assert.True(room.IsClosed);
        Assert.NotNull(room.Floor);
        Assert.Equal(4, room.Floor!.Points.Count);
        Assert.True(room.Floor.Mesh.Vertices.Count > 0);
        Assert.True(room.TryGetFloorBounds(out var min, out var max));
        // Face interna das paredes (espessura 150 mm, orientação Right)
        Assert.Equal(150f, min.X, 1f);
        Assert.Equal(150f, min.Y, 1f);
        Assert.Equal(1850f, max.X, 1f);
        Assert.Equal(1350f, max.Y, 1f);
    }

    [Fact]
    public void PisoAutomatico_Horario5000Interno_AlcancaFaceInterna()
    {
        const float size = 5000f;
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(size, 0),
            new(size, size),
            new(0, size),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference,
            isClosed: true,
            150f,
            2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        var room = new Room();
        room.SetWalls(walls);

        Assert.True(room.IsClosed);
        Assert.NotNull(room.Floor);
        Assert.True(room.TryGetFloorBounds(out var min, out var max));

        Assert.InRange(min.X, -2f, 2f);
        Assert.InRange(min.Y, -2f, 2f);
        Assert.InRange(max.X, size - 2f, size + 2f);
        Assert.InRange(max.Y, size - 2f, size + 2f);
    }

    [Fact]
    public void PisoAutomatico_Antihorario5000Interno_SegueFaceInternaReal()
    {
        const float size = 5000f;
        const float thickness = 150f;
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, size),
            new(size, size),
            new(size, 0),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference,
            isClosed: true,
            thickness,
            2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        var room = new Room();
        room.SetWalls(walls);

        Assert.True(room.IsClosed);
        Assert.NotNull(room.Floor);
        Assert.True(room.TryGetFloorBounds(out var min, out var max));

        float innerSpan = size - thickness * 2f;
        Assert.InRange(max.X - min.X, innerSpan - 5f, innerSpan + 5f);
        Assert.InRange(max.Y - min.Y, innerSpan - 5f, innerSpan + 5f);
    }

    [Fact]
    public void RecalculateClosedState_TresParedesAbertas_RetornaFalse()
    {
        var room = new Room();
        room.SetWalls(
        [
            new WallSegment(new Vector2(0, 0), new Vector2(2000, 0)),
            new WallSegment(new Vector2(2000, 0), new Vector2(2000, 1500)),
            new WallSegment(new Vector2(2000, 1500), new Vector2(0, 1500))
        ]);

        Assert.False(room.IsClosed);
        Assert.Null(room.Floor);
    }

    [Fact]
    public void RebuildAutomaticFloor_AmbienteAberto_GeraRetanguloInterno()
    {
        var room = new Room();
        room.SetWalls(
        [
            new WallSegment(new Vector2(0, 0), new Vector2(2000, 0)),
            new WallSegment(new Vector2(2000, 0), new Vector2(2000, 1500)),
            new WallSegment(new Vector2(2000, 1500), new Vector2(0, 1500))
        ]);

        room.RebuildAutomaticFloor();

        Assert.False(room.IsClosed);
        Assert.NotNull(room.Floor);
        Assert.Equal(4, room.Floor!.Points.Count);
    }
}
