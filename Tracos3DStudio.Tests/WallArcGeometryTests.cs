using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallArcGeometryTests
{
    [Fact]
    public void FromChord_Flecha500_RaioCoerente()
    {
        var arc = WallArcGeometry.FromChord(Vector2.Zero, new Vector2(5000f, 0f), 500f);

        Assert.False(arc.IsStraight);
        Assert.InRange(arc.ChordLength, 4998f, 5002f);
        Assert.InRange(arc.ArcLength, 5100f, 5600f);
        Assert.InRange(arc.GetArcAngleDegrees(), 40f, 70f);
    }

    [Fact]
    public void TryApplyFlecha_Reta_ComprimentoIgualCorda()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(4000f, 0f)) { FlechaMm = 0f };

        Assert.InRange(wall.Length, 3998f, 4002f);
    }

    [Fact]
    public void BuildFacePolylines_Curva_GeraMaisDeDoisPontos()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f))
        {
            FlechaMm = 400f,
            Thickness = 150f
        };

        var arc = WallArcGeometry.FromWall(wall);
        var (faceA, faceB) = arc.BuildFacePolylines(wall.Thickness, wall.Orientation);

        Assert.True(faceA.Count > 2);
        Assert.Equal(faceA.Count, faceB.Count);
    }

    [Fact]
    public void SignedFlechaFromPoint_PositivoQuandoAcimaDaCorda()
    {
        var arc = WallArcGeometry.FromChord(Vector2.Zero, new Vector2(5000f, 0f), 0f);
        float signed = arc.SignedFlechaFromPoint(new Vector2(2500f, 300f));

        Assert.True(signed > 0f);
    }
}
