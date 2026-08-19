using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SceneOcclusionServiceTests
{
    [Fact]
    public void HidePart_NaoRemovePecaDaMalhaMasImpedeSelecao()
    {
        var module = new ModuleInstance { DefinitionId = "teste", Width = 1f, Height = 1f, Depth = 1f };
        module.Mesh.AddQuad(
            new(0f, 0f, 0f), new(1f, 0f, 0f), new(1f, 1f, 0f), new(0f, 1f, 0f),
            FaceKind.ModuleFront, module.Id, "Porta teste");

        Assert.True(ModulePartPickService.TryPickPart(
            new(.5f, .5f, 1f), -Vector3.UnitZ, module, out _, out _));
        Assert.True(SceneOcclusionService.HidePart(module, "Porta teste"));
        Assert.Single(module.Mesh.Faces);
        Assert.False(module.IsPartVisible("Porta teste"));
        Assert.False(ModulePartPickService.TryPickPart(
            new(.5f, .5f, 1f), -Vector3.UnitZ, module, out _, out _));
    }

    [Fact]
    public void RevealAll_ReexibeModulosELimpaPecasOcultas()
    {
        var project = new Project();
        var first = ModuleCatalog.CreateInstance("balcao-1p-400", Vector3.Zero);
        var second = ModuleCatalog.CreateInstance("balcao-2-portas", new(500f, 0f, 0f));
        project.Modules.AddRange([first, second]);
        SceneOcclusionService.HideModules([first]);
        SceneOcclusionService.HidePart(second, "Porta porta-1");

        RevealHiddenResult result = SceneOcclusionService.RevealAll(project);

        Assert.Equal(1, result.Modules);
        Assert.Equal(1, result.Parts);
        Assert.All(project.Modules, module => Assert.True(module.IsVisible));
        Assert.All(project.Modules, module => Assert.Empty(module.HiddenPartLabels));
    }

    [Fact]
    public void ProjectPersistence_PreservaPecasOcultas()
    {
        var project = new Project();
        var module = ModuleCatalog.CreateInstance("balcao-1p-400", Vector3.Zero);
        module.HiddenPartLabels.Add("Lateral esq.");
        project.Modules.Add(module);

        var restored = ProjectPersistence.LoadProject(ProjectPersistence.CreateFromProject(project))
            .Modules.Single();

        Assert.Contains("Lateral esq.", restored.HiddenPartLabels);
        Assert.False(restored.IsPartVisible("Lateral esq."));
    }
}
