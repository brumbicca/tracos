using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SceneModuleListServiceTests
{
    [Fact]
    public void FormatListLabel_UsaNomeDoCatalogoEDimensoes()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        module.SetDimensions(900f, 860f, 560f, ModuleCatalog.GetRequired("balcao-2-portas"));

        string label = SceneModuleListService.FormatListLabel(module);

        Assert.Equal("2P 800mm — 900×860×560 mm", label);
    }

    [Fact]
    public void BuildItems_PreservaOrdemDosModulos()
    {
        var project = new Project();
        project.AddModule("balcao-2-portas", Vector3.Zero);
        project.AddModule("gaveteiro", Vector3.Zero);

        var items = SceneModuleListService.BuildItems(project.Modules);

        Assert.Equal(2, items.Count);
        Assert.Equal("balcao-2-portas", items[0].Module.DefinitionId);
        Assert.Equal("gaveteiro", items[1].Module.DefinitionId);
        Assert.Contains("2P 800mm", items[0].DisplayLabel);
        Assert.Contains("4G 400mm", items[1].DisplayLabel);
    }

    [Fact]
    public void BuildItems_ListaVazia_QuandoSemModulos()
    {
        var items = SceneModuleListService.BuildItems(Array.Empty<ModuleInstance>());

        Assert.Empty(items);
    }
}
