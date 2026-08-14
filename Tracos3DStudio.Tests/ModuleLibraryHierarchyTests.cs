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
            "Cantos Bifold",
            "Balcões",
            "Especiais",
            "Gaveteiros",
            "p/ Eletros",
            "Pias",
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

        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantos" && m.DisplayName == "CR Esq 950mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantos Bifold" && m.Id == "canto-bifold-l-esq-950");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Balcões" && m.DisplayName == "2P 800mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Gaveteiros" && m.DisplayName == "4G Curvo 400mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "p/ Eletros" && m.DisplayName == "Forno 600mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Pias" && m.DisplayName == "2Gav 800mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Diagonais" && m.DisplayName == "Z Dir 300mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Cantoneiras" && m.DisplayName == "Curva Dir 300mm");
        Assert.Contains(inferiores, m => m.LibrarySubGroup == "Fechamentos" && m.DisplayName == "Fechamento");

        Assert.Equal(10, inferiores.Select(m => m.LibrarySubGroup).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Inferiores_Cantos_OrdemCatalogo()
    {
        var cantos = ModuleCatalog.GetCozinhaCatalog()
            .Where(m => m.LibrarySubGroup == ModuleLibraryHierarchy.SubCantos)
            .OrderBy(m => m.CatalogOrder)
            .Select(m => m.DisplayName)
            .ToList();

        Assert.Equal("CR Esq 950mm", cantos[0]);
        Assert.Equal("Obliquo 1P Ajust 900mm", cantos[^1]);
        Assert.Equal(11, cantos.Count);
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
        Assert.Equal(ModuleShapeKind.CornerLLeft, ModuleCatalog.GetRequired("canto-l-esq-950").ShapeKind);
        Assert.Equal(ModuleShapeKind.BlindCornerLeft, ModuleCatalog.GetRequired("canto-cr-esq-950").ShapeKind);
        Assert.Equal(ModuleShapeKind.Filler, ModuleCatalog.GetRequired("fechamento").ShapeKind);
    }
}
