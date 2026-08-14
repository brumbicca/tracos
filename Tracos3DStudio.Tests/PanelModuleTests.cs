using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class PanelModuleTests
{
    [Fact]
    public void ModuleCatalog_ContemTresModulosPaineis()
    {
        var panels = ModuleCatalog.BuiltIn.Where(m => m.Category == ModuleCategory.Paineis).ToList();

        Assert.Equal(3, panels.Count);
        Assert.True(ModuleCatalog.TryGet("painel-liso", out _));
        Assert.True(ModuleCatalog.TryGet("painel-canaletado", out _));
        Assert.True(ModuleCatalog.TryGet("painel-ripado", out _));
    }

    [Theory]
    [InlineData("painel-liso", "P")]
    [InlineData("painel-canaletado", "C")]
    [InlineData("painel-ripado", "R")]
    public void Thumbnail_UsaHintDePainel(string definitionId, string expected)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);

        Assert.Equal(expected, ModuleCatalogThumbnail.GetIconHint(definition));
    }

    [Fact]
    public void PainelLiso_GeraMalhaFinaComFrente()
    {
        var instance = ModuleCatalog.CreateInstance("painel-liso", Vector3.Zero);

        Assert.Equal(18f, instance.Depth);
        Assert.True(instance.Mesh.Vertices.Count > 0);
        Assert.Contains(instance.Mesh.Faces, face => face.Kind == FaceKind.ModuleFront);
    }

    [Fact]
    public void Decompose_PainelGeraUmaPeca()
    {
        var instance = ModuleCatalog.CreateInstance("painel-liso", Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("painel-liso");

        var pieces = ModuleDecompositionService.Decompose(instance, definition, 18f, 6f);

        Assert.Single(pieces);
        Assert.Equal("Painel", pieces[0].Name);
        Assert.Equal(800f, pieces[0].LengthMm);
        Assert.Equal(2100f, pieces[0].WidthMm);
        Assert.Equal(18f, pieces[0].ThicknessMm);
    }

    [Fact]
    public void Placement_PainelUsaPisoDaParede()
    {
        var wall = new WallSegment(new Vector2(0f, 0f), new Vector2(5000f, 0f)) { Height = 2700f };

        var definition = ModuleCatalog.GetRequired("painel-liso");
        var result = ModulePlacementService.PlaceOnInsertionFace(
            wall,
            [wall],
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth,
            2500f,
            new Vector2(0f, 1f),
            moduleHeight: definition.DefaultHeight);

        Assert.Equal(0f, result.Position.Y);
    }

    [Fact]
    public void Budget_PainelTemPrecoProprio()
    {
        Assert.Equal(420m, BudgetService.GetDefaultBasePrice("painel-liso"));
        Assert.Equal(580m, BudgetService.GetDefaultBasePrice("painel-canaletado"));
    }
}
