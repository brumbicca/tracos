using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class Geometry3DTests
{
    [Fact]
    public void TryRayTriangleIntersect_RaioNaDirecaoDoTriangulo_RetornaTrue()
    {
        var origin = new Vector3(100, 100, -500);
        var direction = Vector3.UnitZ;

        bool hit = Geometry3D.TryRayTriangleIntersect(
            origin,
            direction,
            new Vector3(0, 0, 0),
            new Vector3(200, 0, 0),
            new Vector3(200, 200, 0),
            out float t);

        Assert.True(hit);
        Assert.InRange(t, 499f, 501f);
    }

    [Fact]
    public void TryRayQuadIntersect_ParedeVertical_RetornaHitNaFace()
    {
        var origin = new Vector3(0, 1300, 2000);
        var direction = Vector3.Normalize(new Vector3(0, 0, -1));

        bool hit = Geometry3D.TryRayQuadIntersect(
            origin,
            direction,
            new Vector3(0, 0, 0),
            new Vector3(2000, 0, 0),
            new Vector3(2000, 2600, 0),
            new Vector3(0, 2600, 0),
            out float t,
            out Vector3 hitPoint);

        Assert.True(hit);
        Assert.True(t > 0);
        Assert.Equal(0f, hitPoint.Z, precision: 1);
    }
}
