using Xunit;

namespace Tracos3DStudio.Tests;

public class BudgetAuditServiceTests
{
    [Fact]
    public void Run_ProjetoVazio_RetornaErroSemModulos()
    {
        var project = new Project();
        var report = BudgetAuditService.Run(project);

        Assert.True(report.HasErrors);
        Assert.Contains(report.Findings, f => f.Code == "NO_MODULES");
    }

    [Fact]
    public void Run_CozinhaEmL_SemCliente_RetornaAviso()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var report = BudgetAuditService.Run(project);

        Assert.False(report.HasErrors);
        Assert.Contains(report.Findings, f => f.Code == "CLIENT_MISSING");
    }

    [Fact]
    public void Run_ModuloSemPreco_RetornaErro()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.CustomModulePrices = new Dictionary<string, decimal>
        {
            [project.Modules[0].Id.ToString()] = 0m
        };

        var report = BudgetAuditService.Run(project);

        Assert.True(report.HasErrors);
        Assert.Contains(report.Findings, f => f.Code == "MODULE_NO_PRICE");
    }

    [Fact]
    public void Run_ProjetoLimpo_NaoTemErros()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Cliente Teste";

        var report = BudgetAuditService.Run(project);

        Assert.False(report.HasErrors);
        Assert.False(report.HasWarnings);
    }
}
