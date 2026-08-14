using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class WallMoveIntegrationTests
{
    [Fact]
    public void ParticaoMovel_Desloca500mmPerpendicularMantendoComprimento()
    {
        var project = SampleProjects.BuildQuadrado5000ComParticaoMovel();
        var partition = project.Room.Walls.First(w => w.IsMovable);
        Vector2 originalStart = partition.Start;
        Vector2 originalEnd = partition.End;
        float originalLength = WallInnerFaceService.GetDisplayReferenceLength(partition, project.Room.Walls);

        Vector2 delta = partition.LeftNormal * 500f;
        WallMoveService.ApplyTranslation(partition, delta);

        Assert.True(Geometry2D.AlmostEqual(partition.Start, originalStart + delta, 1f));
        Assert.True(Geometry2D.AlmostEqual(partition.End, originalEnd + delta, 1f));
        Assert.InRange(
            WallInnerFaceService.GetDisplayReferenceLength(partition, project.Room.Walls),
            originalLength - 2f,
            originalLength + 2f);
        Assert.True(project.Room.IsClosed);
        Assert.NotNull(project.Room.Floor);
    }
}
