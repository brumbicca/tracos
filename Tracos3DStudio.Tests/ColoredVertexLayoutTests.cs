using System.Runtime.InteropServices;
using Tracos3DStudio;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ColoredVertexLayoutTests
{
    [Fact]
    public void ColoredVertex_UsesTightFloatLayout()
    {
        Assert.Equal(28, Marshal.SizeOf<ColoredVertex>());
        Assert.Equal(12, Marshal.OffsetOf<ColoredVertex>(nameof(ColoredVertex.Cr)).ToInt32());
    }
}
