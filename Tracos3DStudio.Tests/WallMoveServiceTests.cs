using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallMoveServiceTests
{
    [Fact]
    public void DragDelta_SegueNormalPerpendicular()
    {
        var wall = new WallSegment(Vector2.Zero, new Vector2(5000f, 0f))
        {
            Thickness = 150f,
            IsMovable = true
        };

        Vector2 delta = WallMoveService.ComputePerpendicularDragDelta(
            wall,
            dragStartFloor: Vector2.Zero,
            dragCurrentFloor: new Vector2(0f, 1200f));

        Assert.InRange(WallMoveService.ComputeSignedOffsetMm(wall, delta), 1199f, 1201f);
        Assert.True(MathF.Abs(delta.X) < 1f);
        Assert.InRange(delta.Y, 1199f, 1201f);
    }

    [Fact]
    public void ApplyTranslation_DeslocaInicioEFim()
    {
        var wall = new WallSegment(new Vector2(1000f, 2000f), new Vector2(4000f, 2000f));
        var delta = new Vector2(0f, 500f);

        WallMoveService.ApplyTranslation(wall, delta);

        Assert.Equal(new Vector2(1000f, 2500f), wall.Start);
        Assert.Equal(new Vector2(4000f, 2500f), wall.End);
    }

    [Fact]
    public void CanDragInView_SoPlantaSemGrupo()
    {
        Assert.True(WallMoveService.CanDragInView(CameraViewMode.Top, groupSelected: false));
        Assert.False(WallMoveService.CanDragInView(CameraViewMode.Perspective, groupSelected: false));
        Assert.False(WallMoveService.CanDragInView(CameraViewMode.Top, groupSelected: true));
    }
}
