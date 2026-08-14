using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModulePartAxisDisplayTests
{
    [Fact]
    public void PortaEsq_MapeiaVaoParaLarguraNoPainel()
    {
        var dims = new Vector3(18f, 662f, 421f);
        Assert.True(ModulePartAxisDisplay.FaceWidthIsDepth("Porta esq.", dims));
        Assert.Equal(PartHandleAxis.Depth, ModulePartAxisDisplay.PanelWidthAxis(true));
        Assert.Equal(PartHandleAxis.Width, ModulePartAxisDisplay.PanelDepthAxis(true));
        Assert.Equal(421f, ModulePartAxisDisplay.WidthValue(dims, true), 1);
        Assert.Equal(18f, ModulePartAxisDisplay.DepthValue(dims, true), 1);
        Assert.Equal("Largura (vão)", ModulePartAxisDisplay.WidthLabel(true));
        Assert.Equal("Espessura", ModulePartAxisDisplay.DepthLabel(true));
    }

    [Fact]
    public void PortaDir_MantemEixosPadrao()
    {
        var dims = new Vector3(400f, 662f, 18f);
        Assert.False(ModulePartAxisDisplay.FaceWidthIsDepth("Porta dir.", dims));
        Assert.Equal(PartHandleAxis.Width, ModulePartAxisDisplay.PanelWidthAxis(false));
        Assert.Equal(400f, ModulePartAxisDisplay.WidthValue(dims, false), 1);
        Assert.Equal(18f, ModulePartAxisDisplay.DepthValue(dims, false), 1);
    }
}
