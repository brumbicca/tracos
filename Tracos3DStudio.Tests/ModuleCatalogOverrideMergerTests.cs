using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleCatalogOverrideMergerTests
{
    [Fact]
    public void Merge_SobrescreveNomeEMantemCategoriaBuiltIn()
    {
        var builtIn = ModuleCatalog.GetRequired("balcao-2-portas");
        var patch = new ModuleDefinition
        {
            Id = "balcao-2-portas",
            DisplayName = "Balcão 2P Premium",
            Category = ModuleCategory.Dormitorio,
            DefaultWidth = 800f,
            DefaultHeight = 850f,
            DefaultDepth = 550f,
            DoorCount = 2
        };

        var merged = ModuleCatalogOverrideMerger.Merge(builtIn, patch);

        Assert.Equal("Balcão 2P Premium", merged.DisplayName);
        Assert.Equal(ModuleCategory.Cozinha, merged.Category);
        Assert.Equal(ModuleLibraryHierarchy.GroupInferiores, merged.LibraryGroup);
        Assert.Equal(ModuleLibraryHierarchy.SubBalcoes, merged.LibrarySubGroup);
        Assert.True(merged.IsWallMounted == builtIn.IsWallMounted);
    }
}
