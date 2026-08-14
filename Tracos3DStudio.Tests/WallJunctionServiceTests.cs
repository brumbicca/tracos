using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallJunctionServiceTests
{
    [Fact]
    public void TryApplyCornerJoin_AjustaExtremidadeProximaDaIntersecao()
    {
        var moving = new WallSegment(new Vector2(0f, 0f), new Vector2(4000f, 0f));
        var other = new WallSegment(new Vector2(0f, 0f), new Vector2(0f, 3000f));

        bool ok = WallJunctionService.TryApplyCornerJoin(moving, other, out bool adjustedStart);

        Assert.True(ok);
        Assert.True(adjustedStart);
        Assert.Equal(0f, moving.Start.X);
        Assert.Equal(0f, moving.Start.Y);
        Assert.Equal(4000f, moving.End.X);
    }

    [Fact]
    public void TryApplyCornerJoin_ExtendeEixoAteCanto()
    {
        var moving = new WallSegment(new Vector2(1000f, 0f), new Vector2(4000f, 0f));
        var other = new WallSegment(new Vector2(0f, 0f), new Vector2(0f, 3000f));

        bool ok = WallJunctionService.TryApplyCornerJoin(moving, other, out bool adjustedStart);

        Assert.True(ok);
        Assert.True(adjustedStart);
        Assert.Equal(0f, moving.Start.X);
        Assert.Equal(0f, moving.Start.Y);
        Assert.Equal(4000f, moving.Length);
    }

    [Fact]
    public void TryApplyTJoin_AjustaExtremidadeMaisProximaDaParede()
    {
        var moving = new WallSegment(new Vector2(2000f, 0f), new Vector2(2000f, 2500f));
        var through = new WallSegment(new Vector2(0f, 0f), new Vector2(4000f, 0f));

        bool ok = WallJunctionService.TryApplyTJoin(moving, through);

        Assert.True(ok);
        Assert.Equal(2000f, moving.Start.X);
        Assert.Equal(0f, moving.Start.Y);
        Assert.Equal(2000f, moving.End.X);
        Assert.Equal(2500f, moving.End.Y);
    }

    [Fact]
    public void TryApplyTJoin_ParticaoSample_ExtendeEixoAteVertical()
    {
        var through = new WallSegment(new Vector2(-150f, 5150f), new Vector2(-150f, -150f));
        var moving = new WallSegment(new Vector2(1000f, 1850f), new Vector2(4000f, 1850f));

        bool ok = WallJunctionService.TryApplyTJoin(moving, through);

        Assert.True(ok);
        Assert.Equal(-150f, moving.Start.X);
        Assert.Equal(1850f, moving.Start.Y);
        Assert.Equal(4000f, moving.End.X);
        Assert.Equal(4150f, moving.Length);
    }

    [Fact]
    public void TryPickSecondWall_Canto_ExigeIntersecaoNoSegmentoDaSegunda()
    {
        var first = new WallSegment(new Vector2(0f, 0f), new Vector2(4000f, 0f));
        var adjacent = new WallSegment(new Vector2(0f, 0f), new Vector2(0f, 3000f));
        var semIntersecaoNoSegmento = new WallSegment(new Vector2(5000f, 1000f), new Vector2(5000f, 3000f));

        Assert.True(WallJunctionService.TryPickSecondWall(first, adjacent, WallJunctionKind.Corner));
        Assert.False(WallJunctionService.TryPickSecondWall(first, semIntersecaoNoSegmento, WallJunctionKind.Corner));
    }

    [Fact]
    public void TryPickSecondWall_T_AceitaParedeDePassagemNoEixo()
    {
        var partition = new WallSegment(new Vector2(1000f, 1850f), new Vector2(4000f, 1850f));
        var vertical = new WallSegment(new Vector2(-150f, 5150f), new Vector2(-150f, -150f));

        Assert.True(WallJunctionService.TryPickSecondWall(partition, vertical, WallJunctionKind.T));
    }

    [Fact]
    public void TryPickSecondWall_T_RejeitaParalelas()
    {
        var first = new WallSegment(new Vector2(0f, 0f), new Vector2(4000f, 0f));
        var parallel = new WallSegment(new Vector2(0f, 500f), new Vector2(4000f, 500f));

        Assert.False(WallJunctionService.TryPickSecondWall(first, parallel, WallJunctionKind.T));
    }
}
