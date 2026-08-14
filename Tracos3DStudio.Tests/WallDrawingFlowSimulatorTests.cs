using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>
/// Simula o fluxo do MeasureBox: pontos do draft = cantos da face de referência (Orientação Promob).
/// </summary>
public class WallDrawingFlowSimulatorTests
{
    private const float Thickness = 150f;
    private const float Height = 2600f;
    private const float Inner = 5000f;
    private const float OuterApprox = Inner - Thickness * 2f;

    [Fact]
    public void Horario_OrientacaoInterna_ReferenciaEInterno5000()
    {
        var walls = SimulateRoom(
            [Vector2.UnitX, new(0, 1), new(-1, 0), new(0, -1)],
            WallMeasureSide.Interior);

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }

    [Fact]
    public void Antihorario_OrientacaoInterna_Referencia5000_InternoMenor()
    {
        var walls = SimulateRoom(
            [new(0, 1), Vector2.UnitX, new(0, -1), new(-1, 0)],
            WallMeasureSide.Interior);

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), OuterApprox - 2f, OuterApprox + 2f);
        }
    }

    [Fact]
    public void Antihorario_OrientacaoExterna_ReferenciaEInterno5000()
    {
        var walls = SimulateRoom(
            [new(0, 1), Vector2.UnitX, new(0, -1), new(-1, 0)],
            WallMeasureSide.Exterior);

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }

    [Fact]
    public void Horario_FechaNoPontoInicial()
    {
        var walls = SimulateRoom(
            [Vector2.UnitX, new(0, 1), new(-1, 0), new(0, -1)],
            WallMeasureSide.Interior);

        Assert.Equal(4, walls.Count);
        Assert.True(Geometry2D.AlmostEqual(walls[0].Start, walls[^1].End, 5f));
    }

    [Fact]
    public void PainelMostraComprimentoNaFaceDeReferencia()
    {
        var walls = SimulateRoom([Vector2.UnitX, new(0, 1), new(-1, 0), new(0, -1)], WallMeasureSide.Interior);

        foreach (var wall in walls)
        {
            Assert.Equal(Inner, wall.InnerLengthTarget);
            Assert.InRange(WallInnerFaceService.GetDisplayReferenceLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }

    private static List<WallSegment> SimulateRoom(Vector2[] directions, WallMeasureSide measureSide)
    {
        var draft = new WallDraft { Thickness = Thickness, Height = Height, MeasureSide = measureSide };
        draft.Start(Vector2.Zero);

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 direction = Vector2.Normalize(directions[i]);
            bool closing = i == directions.Length - 1;

            Vector2 point = closing
                ? draft.Points[0]
                : draft.Points[^1] + direction * Inner;

            draft.ConfirmPoint(point, Inner);
        }

        return draft.BuildWalls();
    }
}
