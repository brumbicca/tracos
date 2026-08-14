using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallInnerFaceServiceTests
{
    [Fact]
    public void GetInnerFace_RetanguloFechadoRoomTests_MedeInterno()
    {
        var reference = new List<Vector2>
        {
            new(150, 150),
            new(1850, 150),
            new(1850, 1350),
            new(150, 1350),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, true, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        Assert.InRange(WallInnerFaceService.GetInnerLength(walls[0], walls), 1650f, 1750f);
        Assert.InRange(WallInnerFaceService.GetInnerLength(walls[1], walls), 1150f, 1250f);
    }

    [Fact]
    public void ReferenceSegmentToAxis_InteriorEsquerda_DeslocaEixo()
    {
        var (start, end) = WallInnerFaceService.ReferenceSegmentToAxis(
            Vector2.Zero,
            new Vector2(5000f, 0),
            150f,
            interiorOnLeft: true,
            WallMeasureSide.Interior);

        Assert.Equal(5000f, (end - start).Length, 0.5f);
        Assert.Equal(-150f, start.Y, 0.5f);
    }

    [Fact]
    public void ReferenceSegmentToAxis_InteriorDireita_ReferenciaExterna_DeslocaEixo()
    {
        var (start, end) = WallInnerFaceService.ReferenceSegmentToAxis(
            Vector2.Zero,
            new Vector2(0, 5000f),
            150f,
            interiorOnLeft: false,
            WallMeasureSide.Interior);

        Assert.Equal(5000f, (end - start).Length, 0.5f);
        Assert.Equal(150f, start.X, 0.5f);
    }

    [Fact]
    public void BuildWallsFromReferenceCorners_Horario_OrientacaoInterna_Interno5000()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(5000, 0),
            new(5000, 5000),
            new(0, 5000),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, true, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        foreach (var wall in walls)
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), 4998f, 5002f);
    }

    [Fact]
    public void BuildWallsFromReferenceCorners_Antihorario_OrientacaoInterna_InternoMenor()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, 5000),
            new(5000, 5000),
            new(5000, 0),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, true, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        foreach (var wall in walls)
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), 4698f, 4702f);
    }

    [Fact]
    public void BuildWallsFromReferenceCorners_Antihorario_OrientacaoExterna_Interno5000()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, 5000),
            new(5000, 5000),
            new(5000, 0),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, true, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Exterior);

        foreach (var wall in walls)
            Assert.InRange(WallInnerFaceService.GetInnerLength(wall, walls), 4998f, 5002f);
    }

    [Fact]
    public void GetInnerFace_TresParedesEmL_SemComprimentoMaiorQueEixo()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, 5000),
            new(5000, 5000),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, false, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        var innerFace = WallInnerFaceService.GetInnerFace(walls[^1], walls);

        Assert.InRange(innerFace.Length, 4998f, 5002f);
        Assert.True(Geometry2D.AlmostEqual(innerFace.InnerStart, new Vector2(0, 5000), 3f));
        Assert.True(Geometry2D.AlmostEqual(innerFace.InnerEnd, new Vector2(5000, 5000), 3f));
    }

    [Fact]
    public void CantosReferencia_EmL_EncontroNaFaceTracejada()
    {
        var reference = new List<Vector2>
        {
            new(0, 0),
            new(0, 4000),
            new(4000, 4000),
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, false, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        var inner1 = WallInnerFaceService.GetInnerFace(walls[0], walls);
        var inner2 = WallInnerFaceService.GetInnerFace(walls[1], walls);

        Assert.True(Geometry2D.AlmostEqual(inner1.InnerEnd, inner2.InnerStart, 3f));
    }

    [Fact]
    public void GetInnerFace_ParedeIsolada_HorizontalReferenciaInterna()
    {
        var reference = new List<Vector2> { new(0, 0), new(4000, 0) };
        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            reference, false, 150f, 2600f, WallOrientation.Right, WallMeasureSide.Interior);

        var face = WallInnerFaceService.GetInnerFace(walls[0], walls);

        Assert.Equal(4000f, face.Length, precision: 1);
        Assert.InRange(face.InnerStart.Y, -5f, 5f);
    }

    [Fact]
    public void ReferenceIsOnInteriorRoomSide_PromobModelo()
    {
        Assert.True(WallInnerFaceService.ReferenceIsOnInteriorRoomSide(WallMeasureSide.Interior, interiorOnLeft: true));
        Assert.False(WallInnerFaceService.ReferenceIsOnInteriorRoomSide(WallMeasureSide.Interior, interiorOnLeft: false));
        Assert.False(WallInnerFaceService.ReferenceIsOnInteriorRoomSide(WallMeasureSide.Exterior, interiorOnLeft: true));
        Assert.True(WallInnerFaceService.ReferenceIsOnInteriorRoomSide(WallMeasureSide.Exterior, interiorOnLeft: false));
    }
}
