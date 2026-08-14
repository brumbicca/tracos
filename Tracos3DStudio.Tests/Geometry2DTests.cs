using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class Geometry2DTests
{
    [Fact]
    public void AlmostEqual_DentroDaTolerancia_RetornaTrue()
    {
        var a = new Vector2(1000, 0);
        var b = new Vector2(1005, 0);

        Assert.True(Geometry2D.AlmostEqual(a, b, 10f));
    }

    [Fact]
    public void SnapAngle_45Graus_PreservaComprimentoEAlinhaAngulo()
    {
        var origin = Vector2.Zero;
        var current = new Vector2(1000, 50);

        var snapped = Geometry2D.SnapAngle(origin, current, 45f);

        Assert.Equal(current.Length, snapped.Length, precision: 1);
        Assert.True(MathF.Abs(snapped.Y) < 1f);
        Assert.True(snapped.X > 999f);
    }

    [Fact]
    public void PolygonArea_Retangulo2000x1500_RetornaAreaCorreta()
    {
        var points = new List<Vector2>
        {
            new(0, 0),
            new(2000, 0),
            new(2000, 1500),
            new(0, 1500)
        };

        var area = MathF.Abs(Geometry2D.PolygonArea(points));

        Assert.Equal(3_000_000f, area, precision: 1);
    }

    [Fact]
    public void RemoveDuplicates_RemovePontoFinalDuplicado()
    {
        var points = new List<Vector2>
        {
            new(0, 0),
            new(2000, 0),
            new(2000, 1500),
            new(0, 1500),
            new(0, 0)
        };

        var clean = Geometry2D.RemoveDuplicates(points, 2f);

        Assert.Equal(4, clean.Count);
    }
}
