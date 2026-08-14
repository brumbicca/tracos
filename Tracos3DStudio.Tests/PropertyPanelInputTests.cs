using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class PropertyPanelInputTests
{
    [Fact]
    public void TryReadWallDimensions_Valores5000_2200_200_ParseiaCorretamente()
    {
        bool ok = PropertyPanelInput.TryReadWallDimensions("5000", "2200", "200", out float l, out float h, out float t);

        Assert.True(ok);
        Assert.Equal(5000f, l);
        Assert.Equal(2200f, h);
        Assert.Equal(200f, t);
    }

    [Fact]
    public void ApplyWallDimensions_AtualizaComprimentoAlturaEspessura()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(1000, 0));

        PropertyPanelInput.ApplyWallDimensions(wall, 5000f, 2200f, 200f);

        Assert.Equal(5000f, wall.Length, 1);
        Assert.Equal(2200f, wall.Height);
        Assert.Equal(200f, wall.Thickness);
    }

    [Fact]
    public void TryParseMm_CampoVazio_RetornaFalse()
    {
        Assert.False(PropertyPanelInput.TryParseMm("", out _));
        Assert.False(PropertyPanelInput.TryParseMm("   ", out _));
    }

    [Fact]
    public void TryParseMm_VirgulaDecimal_Aceita()
    {
        Assert.True(PropertyPanelInput.TryParseMm("900,5", out float value));
        Assert.Equal(900.5f, value);
    }

    [Fact]
    public void TryReadPosition_TresEixos_ParseiaCorretamente()
    {
        bool ok = PropertyPanelInput.TryReadPosition("1200", "0", "350", out var position);

        Assert.True(ok);
        Assert.Equal(new Vector3(1200f, 0f, 350f), position);
    }

    [Fact]
    public void Rotate90Degrees_NormalizaPara360()
    {
        Assert.Equal(90f, PropertyPanelInput.Rotate90Degrees(0f));
        Assert.Equal(0f, PropertyPanelInput.Rotate90Degrees(270f));
    }

    [Fact]
    public void ApplyModulePosition_AtualizaMalha()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        float initialX = module.Mesh.Vertices[0].X;

        PropertyPanelInput.ApplyModulePosition(module, definition, new Vector3(500f, 0f, 200f));

        Assert.Equal(500f, module.Position.X);
        Assert.NotEqual(initialX, module.Mesh.Vertices[0].X);
    }
}
