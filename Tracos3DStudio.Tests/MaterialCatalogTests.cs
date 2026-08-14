using Xunit;

namespace Tracos3DStudio.Tests;

public class MaterialCatalogTests
{
    [Fact]
    public void DefaultMaterial_Existe()
    {
        var material = MaterialCatalog.GetDefault();
        Assert.Equal(MaterialCatalog.DefaultMaterialId, material.Id);
    }

    [Fact]
    public void NovoModulo_UsaMaterialPadrao()
    {
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        Assert.Equal(MaterialCatalog.DefaultMaterialId, instance.MaterialId);
    }

    [Theory]
    [InlineData("#FF0000", 1f, 0f, 0f)]
    [InlineData("#00FF00", 0f, 1f, 0f)]
    public void ParseHexRgb_ConverteCor(string hex, float r, float g, float b)
    {
        var (pr, pg, pb) = ColorParsing.ParseHexRgb(hex);
        Assert.Equal(r, pr, 2);
        Assert.Equal(g, pg, 2);
        Assert.Equal(b, pb, 2);
    }
}
