using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class BudgetServiceTests
{
    [Fact]
    public void Build_ComQuatroModulos_CalculaTotal()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Maria Silva";
        project.Metadata.ClientPhone = "(11) 99999-0000";

        var summary = BudgetService.Build(project);

        Assert.Equal(4, summary.Items.Count);
        Assert.Equal("Maria Silva", summary.ClientName);
        Assert.True(summary.GrandTotal > 0m);
    }

    [Fact]
    public void Build_MaterialMadeirado_AdicionaValor()
    {
        var project = new Project();
        project.Modules.Add(ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero));
        project.Modules[0].MaterialId = "mdf-madeirado";

        var summary = BudgetService.Build(project);
        var item = summary.Items[0];

        Assert.Equal(180m, item.MaterialAddOn);
        Assert.Equal(BudgetService.GetDefaultBasePrice("balcao-2-portas") + 180m, item.Total);
    }

    [Fact]
    public void Build_PrecoCustomizadoPorModulo_UsaValorSalvo()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var module = project.Modules[0];
        project.Metadata.CustomModulePrices = new Dictionary<string, decimal>
        {
            [module.Id.ToString()] = 2500m
        };

        var summary = BudgetService.Build(project);
        var item = summary.Items.First(i => i.ModuleId == module.Id);

        Assert.Equal(2500m, item.BasePrice);
    }

    [Fact]
    public void Build_GeraSecoesModulosEPecas()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var summary = BudgetService.Build(project);

        Assert.Equal(2, summary.Sections.Count);
        Assert.Equal("— Módulos", summary.Sections[0].Name);
        Assert.Equal("— Peças", summary.Sections[1].Name);
        Assert.Equal(4, summary.Sections[0].Items.Count);
        Assert.True(summary.Sections[1].Items.Count > summary.Sections[0].Items.Count);
        Assert.True(summary.GrandTotal > summary.Sections[0].Subtotal);
    }

    [Fact]
    public void Build_UsaObraEAmbienteDoMetadata()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.WorkName = "Cozinha L — Ed. Aurora";
        project.Metadata.EnvironmentName = "Cozinha integrada";

        var summary = BudgetService.Build(project);

        Assert.Equal("Cozinha L — Ed. Aurora", summary.WorkName);
        Assert.Equal("Cozinha integrada", summary.EnvironmentTitle);
    }

    [Fact]
    public void Build_PrecoZero_MarcaItemSemPreco()
    {
        var project = new Project();
        project.Modules.Add(ModuleCatalog.CreateInstance("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero));
        project.Metadata.CustomModulePrices = new Dictionary<string, decimal>
        {
            [project.Modules[0].Id.ToString()] = 0m
        };

        var summary = BudgetService.Build(project);

        Assert.False(summary.Items[0].HasPrice);
        Assert.True(summary.HasUnpricedItems);
    }
}
