using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleInstanceNamingServiceTests
{
    [Fact]
    public void GetEffectiveDisplayName_SemCustomizado_UsaCatalogo()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);

        Assert.Equal("2P 800mm", ModuleInstanceNamingService.GetEffectiveDisplayName(module));
    }

    [Fact]
    public void TryApplyRename_PersisteNomeCustomizado()
    {
        var module = ModuleCatalog.CreateInstance("gaveteiro", Vector3.Zero);

        Assert.True(ModuleInstanceNamingService.TryApplyRename(module, "  Gaveteiro pia  ", out _));
        Assert.Equal("Gaveteiro pia", module.InstanceDisplayName);
        Assert.Equal("Gaveteiro pia", ModuleInstanceNamingService.GetEffectiveDisplayName(module));
    }

    [Fact]
    public void TryApplyRename_Vazio_RemoveCustomizado()
    {
        var module = ModuleCatalog.CreateInstance("aereo", Vector3.Zero);
        module.InstanceDisplayName = "Aéreo cozinha";

        Assert.True(ModuleInstanceNamingService.TryApplyRename(module, "   ", out _));

        Assert.Null(module.InstanceDisplayName);
        Assert.Equal("Aéreo 2P 800mm", ModuleInstanceNamingService.GetEffectiveDisplayName(module));
    }

    [Fact]
    public void TryApplyRename_NomeLongo_Rejeita()
    {
        var module = ModuleCatalog.CreateInstance("aereo", Vector3.Zero);
        string longName = new('X', ModuleInstanceNamingService.MaxLength + 1);

        Assert.False(ModuleInstanceNamingService.TryApplyRename(module, longName, out string? error));

        Assert.NotNull(error);
        Assert.Null(module.InstanceDisplayName);
    }

    [Fact]
    public void FormatListLabel_UsaNomeCustomizado()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        module.InstanceDisplayName = "Balcão principal";

        string label = SceneModuleListService.FormatListLabel(module);

        Assert.Equal("Balcão principal — 800×850×550 mm", label);
    }

    [Fact]
    public void Persistencia_RoundTripNomeCustomizado()
    {
        var project = new Project();
        var module = project.AddModule("comoda-4g", Vector3.Zero);
        module.InstanceDisplayName = "Cômoda casal";

        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");

        try
        {
            var document = ProjectPersistence.CreateFromProject(project);
            ProjectPersistence.SaveToFile(document, path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Single(restored.Modules);
            Assert.Equal("Cômoda casal", restored.Modules[0].InstanceDisplayName);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
