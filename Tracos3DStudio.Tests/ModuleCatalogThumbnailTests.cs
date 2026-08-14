using System.Windows.Media;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleCatalogThumbnailTests
{
    [Theory]
    [InlineData("balcao-2-portas", "2P")]
    [InlineData("gaveteiro", "4G")]
    [InlineData("aereo", "A")]
    [InlineData("comoda-4g", "4G")]
    public void GetIconHint_RefletePortasGavetasOuAereo(string definitionId, string expected)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);

        Assert.Equal(expected, ModuleCatalogThumbnail.GetIconHint(definition));
    }

    [Fact]
    public void GetAccentColor_DormitorioDiferenteDeCozinha()
    {
        var cozinha = ModuleCatalog.GetRequired("balcao-2-portas");
        var dormitorio = ModuleCatalog.GetRequired("guarda-roupa-2p");

        Assert.NotEqual(
            ModuleCatalogThumbnail.GetAccentColor(cozinha),
            ModuleCatalogThumbnail.GetAccentColor(dormitorio));
    }

    [Fact]
    public void BuildItems_IncluiMiniaturaPorModulo()
    {
        var project = new Project();
        project.AddModule("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);

        var items = SceneModuleListService.BuildItems(project.Modules);

        Assert.Single(items);
        Assert.Equal("2P", items[0].IconHint);
        Assert.IsType<SolidColorBrush>(items[0].AccentBrush);
    }
}
