using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class GridLayoutServiceTests
{
    [Fact]
    public void ComputeUniformDivisions_5000x5000_Passo500_DivideExato()
    {
        var (cols, rows, stepX, stepY) = GridLayoutService.ComputeUniformDivisions(
            new Vector2(0, 0),
            new Vector2(5000, 5000),
            500f);

        Assert.Equal(10, cols);
        Assert.Equal(10, rows);
        Assert.Equal(500f, stepX);
        Assert.Equal(500f, stepY);
    }

    [Fact]
    public void Geometry2D_ContainsPoint_RetanguloInterno()
    {
        var poly = new List<Vector2>
        {
            new(0, 0),
            new(5000, 0),
            new(5000, 5000),
            new(0, 5000)
        };

        Assert.True(Geometry2D.ContainsPoint(poly, new Vector2(2500, 2500)));
        Assert.False(Geometry2D.ContainsPoint(poly, new Vector2(6000, 2500)));
    }
}
