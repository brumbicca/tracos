using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleLibraryHierarchyTests
{
    [Fact]
    public void Inferiores_Subpastas_NaOrdemDoPromob()
    {
        Assert.Equal(
        [
            "Cantos",
            "Balcões",
            "Especiais",
            "Gaveteiros",
            "Diagonais",
            "Cantoneiras",
            "Fechamentos"
        ], ModuleLibraryHierarchy.InferioresSubGroupOrder);
    }

    [Fact]
    public void Inferiores_Catalogo_ContemSkusDasImagensPromob()
    {
        var inferiores = ModuleCatalog.GetCozinhaCatalog()
            .Where(m => m.LibraryGroup == ModuleLibraryHierarchy.GroupInferiores)
            .ToList();

        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantos" && m.DisplayName == "CR 950mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantos" &&
                                        m.Id == "canto-bifold-l-esq-950" &&
                                        m.DisplayName == "\"L\" 3P 950mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Balcões" && m.DisplayName == "2P 800mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Gaveteiros" && m.DisplayName == "4G Curvo 400mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Balcões" && m.Id == "pia-1gav-basc-800");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Balcões" && m.Id == "pia-2p-4g-1200");
        Assert.DoesNotContain(inferiores, m => m.LibrarySubGroup == "p/ Eletros");
        Assert.DoesNotContain(inferiores, m => m.LibrarySubGroup == "Pias");
        Assert.False(ModuleCatalog.TryGet("pia-2gav-800", out _));
        Assert.False(ModuleCatalog.TryGet("pia-1p-600", out _));
        Assert.False(ModuleCatalog.TryGet("pia-2p-600", out _));
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Diagonais" && m.Id == "diag-300" && m.DisplayName == "Diagonal 300mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Diagonais" && m.Id == "diag-chanf-300" && m.DisplayName == "Chanfrado 300mm");
        Assert.DoesNotContain(inferiores, m => m.LibrarySubGroup == "Diagonais" && m.DisplayName.Contains("Curvo", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inferiores, m => m.LibrarySubGroup == "Diagonais" && m.DisplayName.StartsWith("Z ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantoneiras" && m.DisplayName == "Curva Dir 300mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Fechamentos" && m.DisplayName == "Fechamento");

        Assert.Equal(7, inferiores.Select(m => m.LibrarySubGroup).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Inferiores_Cantos_OrdemCatalogo()
    {
        var cantos = ModuleCatalog.GetCozinhaCatalog()
            .Where(m => m.LibrarySubGroup == ModuleLibraryHierarchy.SubCantos)
            .OrderBy(m => m.CatalogOrder)
            .Select(m => m.DisplayName)
            .ToList();

        Assert.Equal(
        [
            "CR 950mm",
            "CR 2P 1245mm",
            "\"L\" 2P 950mm",
            "\"L\" 3P 950mm",
            "Canto Oblíquo 800x800"
        ], cantos);
        Assert.DoesNotContain("Canto Gaveteiro 3G 900mm", cantos);
        Assert.Contains("Canto Oblíquo 800x800", cantos);
        Assert.Contains("\"L\" 2P 950mm", cantos);
        Assert.Contains("\"L\" 3P 950mm", cantos);
        Assert.DoesNotContain("\"L\" Esq 950mm", cantos);
        Assert.DoesNotContain("\"L\" Dir 950mm", cantos);
        Assert.DoesNotContain(cantos, name => name.Contains("Curvo", StringComparison.OrdinalIgnoreCase));
        Assert.Single(cantos, name => name.Contains("Oblíquo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5, cantos.Count);
    }

    [Fact]
    public void Despenseiro_ResolveSlot_CozinhaDespenseiro()
    {
        var definition = ModuleCatalog.GetRequired("despenseiro-2p-600");
        Assert.Equal(ModuleDimensionSlot.CozinhaDespenseiro, DimensionConfiguratorService.ResolveSlot(definition));
    }

    [Fact]
    public void Filter_BuscaPorSubpasta()
    {
        Assert.True(ModuleCatalogFilterService.Matches(
            ModuleCatalog.GetRequired("gav-4g-curvo-400"),
            "Gaveteiros"));
    }

    [Fact]
    public void ShapeKind_CantoL_Distinto()
    {
        Assert.Equal(ModuleShapeKind.CornerLLeft, ModuleCatalog.GetRequired("canto-l-2p-esq-950").ShapeKind);
        Assert.Equal(ModuleShapeKind.BlindCornerLeft, ModuleCatalog.GetRequired("canto-cr-esq-950").ShapeKind);
        Assert.Equal(ModuleShapeKind.Filler, ModuleCatalog.GetRequired("fechamento").ShapeKind);
    }
}
