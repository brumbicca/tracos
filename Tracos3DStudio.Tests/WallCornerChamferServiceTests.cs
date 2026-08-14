using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallCornerChamferServiceTests
{
    [Fact]
    public void TryApply_150mm_NoInicio_DefineChanfro()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f));

        bool ok = WallCornerChamferService.TryApply(wall, atStart: true, 150f);

        Assert.True(ok);
        Assert.Equal(150f, wall.ChamferStartMm);
    }

    [Fact]
    public void TryApply_ExcedeMaximo_RetornaFalse()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(500f, 0f));

        bool ok = WallCornerChamferService.TryApply(wall, atStart: true, 400f);

        Assert.False(ok);
    }

    [Fact]
    public void TryPickEndpoint_PertoDoInicio_RetornaAtStart()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(4000f, 0f));

        bool ok = WallCornerChamferService.TryPickEndpoint(wall, new Vector2(80f, 0f), out bool atStart);

        Assert.True(ok);
        Assert.True(atStart);
    }

    [Fact]
    public void BuildWithCorners_ChamfroEncurtaFaceVisual()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f)) { ChamferStartMm = 200f };
        var visuals = WallVisualBuilder.BuildWithCorners([wall]);

        Assert.Single(visuals);
        Assert.InRange(visuals[0].A1.X, 198f, 202f);
        Assert.InRange(visuals[0].B1.X, 198f, 202f);
        Assert.Equal(5000f, visuals[0].A2.X);
    }
}
