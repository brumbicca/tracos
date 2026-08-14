using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallInnerReconcileTests
{
    private const float Thickness = 150f;
    private const float Height = 2600f;
    private const float Inner = 5000f;
    private const float OuterApprox = Inner - Thickness * 2f;

    [Fact]
    public void GiroEsquerda_OrientacaoInterna_Referencia5000_InternoMenor()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, Inner),
            new(Inner, Inner),
            new(Inner, 0),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, isClosed: true, Thickness, Height, WallOrientation.Right,
            WallMeasureSide.Interior,
            Enumerable.Repeat(Inner, 4).ToList());

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), OuterApprox - 2f, OuterApprox + 2f);
        }
    }

    [Fact]
    public void GiroDireita_OrientacaoInterna_Referencia5000_Interno5000()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(Inner, 0),
            new(Inner, Inner),
            new(0, Inner),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, isClosed: true, Thickness, Height, WallOrientation.Right,
            WallMeasureSide.Interior,
            Enumerable.Repeat(Inner, 4).ToList());

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }

    [Fact]
    public void WallDraft_InnerTargets_SaoArmazenadosPorParede()
    {
        var draft = new WallDraft { Thickness = Thickness, Height = Height };

        draft.Start(new Vector2(0, 0));
        draft.ConfirmPoint(new Vector2(3000f, 0), 3000f);
        draft.ConfirmPoint(new Vector2(3000f, 4000f), 4000f);

        Assert.Equal(2, draft.InnerLengthTargets.Count);
        Assert.Equal(3000f, draft.InnerLengthTargets[0], 0.1f);
        Assert.Equal(4000f, draft.InnerLengthTargets[1], 0.1f);
    }

    [Fact]
    public void FluxoUI_Horario_OrientacaoInterna_ReferenciaEInterno5000()
    {
        var draft = new WallDraft { Thickness = Thickness, Height = Height, MeasureSide = WallMeasureSide.Interior };
        draft.Start(Vector2.Zero);
        draft.ConfirmPoint(new Vector2(Inner, 0), Inner);
        draft.ConfirmPoint(new Vector2(Inner, Inner), Inner);
        draft.ConfirmPoint(new Vector2(0, Inner), Inner);
        draft.ConfirmPoint(Vector2.Zero, Inner);

        var walls = draft.BuildWalls();

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }

    [Fact]
    public void FluxoUI_Antihorario_OrientacaoInterna_Referencia5000_InternoMenor()
    {
        var draft = new WallDraft { Thickness = Thickness, Height = Height, MeasureSide = WallMeasureSide.Interior };
        draft.Start(Vector2.Zero);
        draft.ConfirmPoint(new Vector2(0, Inner), Inner);
        draft.ConfirmPoint(new Vector2(Inner, Inner), Inner);
        draft.ConfirmPoint(new Vector2(Inner, 0), Inner);
        draft.ConfirmPoint(Vector2.Zero, Inner);

        var walls = draft.BuildWalls();

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), OuterApprox - 2f, OuterApprox + 2f);
        }
    }

    [Fact]
    public void FluxoUI_Antihorario_OrientacaoExterna_ReferenciaEInterno5000()
    {
        var draft = new WallDraft { Thickness = Thickness, Height = Height, MeasureSide = WallMeasureSide.Exterior };
        draft.Start(Vector2.Zero);
        draft.ConfirmPoint(new Vector2(0, Inner), Inner);
        draft.ConfirmPoint(new Vector2(Inner, Inner), Inner);
        draft.ConfirmPoint(new Vector2(Inner, 0), Inner);
        draft.ConfirmPoint(Vector2.Zero, Inner);

        var walls = draft.BuildWalls();

        foreach (var wall in walls)
        {
            Assert.InRange(WallInnerFaceService.GetReferenceFaceLength(wall, walls), Inner - 2f, Inner + 2f);
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), Inner - 2f, Inner + 2f);
        }
    }
}
