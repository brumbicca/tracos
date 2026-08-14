using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class BudgetCommercialTermsTests
{
    [Fact]
    public void Build_ComDesconto_CalculaFinalTotal()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetDiscountPercent = 10m;
        project.Metadata.BudgetPaymentTerms = "30% na assinatura, 70% na entrega";

        var summary = BudgetService.Build(project);

        Assert.Equal(10m, summary.BudgetDiscountPercent);
        Assert.Equal("30% na assinatura, 70% na entrega", summary.BudgetPaymentTerms);
        Assert.True(summary.Subtotal > 0m);
        Assert.Equal(Math.Round(summary.Subtotal * 0.10m, 2, MidpointRounding.AwayFromZero), summary.DiscountAmount);
        Assert.Equal(Math.Round(summary.Subtotal - summary.DiscountAmount, 2, MidpointRounding.AwayFromZero), summary.FinalTotal);
    }

    [Fact]
    public void Build_DescontoAcimaDe100_Clampeia()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetDiscountPercent = 150m;

        var summary = BudgetService.Build(project);

        Assert.Equal(100m, summary.BudgetDiscountPercent);
        Assert.Equal(Math.Round(summary.Subtotal, 2, MidpointRounding.AwayFromZero), summary.DiscountAmount);
        Assert.Equal(0m, summary.FinalTotal);
    }

    [Fact]
    public void Persistencia_SalvaDescontoECondicoes()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetDiscountPercent = 5m;
        project.Metadata.BudgetPaymentTerms = "Entrada + 3x sem juros";

        var path = Path.Combine(Path.GetTempPath(), $"comercial-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal(5m, restored.Metadata.BudgetDiscountPercent);
            Assert.Equal("Entrada + 3x sem juros", restored.Metadata.BudgetPaymentTerms);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportPdf_ArtefatoAceite_ComDescontoECondicoes()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Cliente Comercial";
        project.Metadata.BudgetDiscountPercent = 10m;
        project.Metadata.BudgetPaymentTerms = "30% na assinatura, 70% na entrega";

        var summary = BudgetService.Build(project);
        var pdfPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "orcamento", "fase-D.2-desconto-condicoes.pdf"));

        Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
        BudgetPdfExporter.Export(summary, pdfPath);

        Assert.True(new FileInfo(pdfPath).Length > 5000);
        Assert.True(summary.FinalTotal < summary.Subtotal);
    }
}
