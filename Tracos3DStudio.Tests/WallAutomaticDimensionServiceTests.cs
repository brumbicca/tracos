using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallAutomaticDimensionServiceTests
{
    [Fact]
    public void Horario5000Interna_QuatroCotas5000NaReferencia()
    {
        var corners = new List<Vector2>
        {
            new(0, 0),
            new(5000, 0),
            new(5000, 5000),
            new(0, 5000)
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            corners,
            isClosed: true,
            thickness: 150f,
            height: 2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        var dims = WallAutomaticDimensionService.BuildForWalls(walls);

        Assert.Equal(4, dims.Count);
        Assert.All(dims, d => Assert.InRange(d.LengthMm, 4998f, 5002f));
    }

    [Fact]
    public void Antihorario5000Interna_Referencia5000EmTodasAsParedes()
    {
        var corners = new List<Vector2>
        {
            new(0, 0),
            new(0, 5000),
            new(5000, 5000),
            new(5000, 0)
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            corners,
            isClosed: true,
            thickness: 150f,
            height: 2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        var dims = WallAutomaticDimensionService.BuildForWalls(walls);

        Assert.Equal(4, dims.Count);
        Assert.All(dims, d => Assert.InRange(d.LengthMm, 4998f, 5002f));
    }

    [Fact]
    public void Cotas_Deslocadas280mmDaFaceDeReferencia()
    {
        var corners = new List<Vector2>
        {
            new(0, 0),
            new(3000, 0),
            new(3000, 3000),
            new(0, 3000)
        };

        var walls = WallInnerFaceService.BuildWallsFromReferenceCorners(
            corners,
            isClosed: true,
            thickness: 150f,
            height: 2600f,
            WallOrientation.Right,
            WallMeasureSide.Interior);

        var dims = WallAutomaticDimensionService.BuildForWalls(walls);

        foreach (var dim in dims)
        {
            Assert.InRange((dim.DimStart - dim.FaceStart).Length, 275f, 285f);
            Assert.InRange((dim.DimEnd - dim.FaceEnd).Length, 275f, 285f);
        }
    }
}
