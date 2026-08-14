using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleCatalogFilterServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Matches_EmptyQuery_AceitaQualquerModulo(string? query)
    {
        var definition = ModuleCatalog.GetRequired("gaveteiro");

        Assert.True(ModuleCatalogFilterService.Matches(definition, query));
    }

    [Theory]
    [InlineData("gav", "gaveteiro")]
    [InlineData("2P", "balcao-2-portas")]
    [InlineData("2-portas", "balcao-2-portas")]
    [InlineData("Gaveteiros", "gaveteiro")]
    [InlineData("Inferiores", "balcao-2-portas")]
    public void Matches_PorNomeOuId(string query, string definitionId)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);

        Assert.True(ModuleCatalogFilterService.Matches(definition, query));
    }

    [Fact]
    public void Filter_RetornaSomenteCorrespondencias()
    {
        var results = ModuleCatalogFilterService.Filter(ModuleCatalog.BuiltIn, "balcao");

        Assert.True(results.Count >= 2);
        Assert.All(results, item => Assert.Contains("balcao", item.Id, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filter_SemCorrespondencia_RetornaVazio()
    {
        var results = ModuleCatalogFilterService.Filter(ModuleCatalog.BuiltIn, "xyz-inexistente");

        Assert.Empty(results);
    }
}
