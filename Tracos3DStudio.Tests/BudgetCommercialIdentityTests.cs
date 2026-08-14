using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class BudgetCommercialIdentityTests
{
    [Fact]
    public void Build_PropagaVendedorEObservacoes()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetSalesPerson = "Ana Silva";
        project.Metadata.BudgetCommercialNotes = "Prazo de montagem: 15 dias úteis após aprovação.";

        var summary = BudgetService.Build(project);

        Assert.Equal("Ana Silva", summary.BudgetSalesPerson);
        Assert.Equal("Prazo de montagem: 15 dias úteis após aprovação.", summary.BudgetCommercialNotes);
    }

    [Fact]
    public void Persistencia_SalvaVendedorEObservacoes()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetSalesPerson = "Carlos Projetista";
        project.Metadata.BudgetCommercialNotes = "Frete não incluso.";

        var path = Path.Combine(Path.GetTempPath(), $"comercial-id-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal("Carlos Projetista", restored.Metadata.BudgetSalesPerson);
            Assert.Equal("Frete não incluso.", restored.Metadata.BudgetCommercialNotes);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportPdf_ArtefatoAceite_ComVendedorEObservacoes()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Cliente Comercial D3";
        project.Metadata.BudgetSalesPerson = "Ana Silva";
        project.Metadata.BudgetCommercialNotes = "Montagem inclusa na região metropolitana.";

        var summary = BudgetService.Build(project);
        var pdfPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "orcamento", "fase-D.3-vendedor-observacoes.pdf"));

        Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
        BudgetPdfExporter.Export(summary, pdfPath);

        Assert.True(new FileInfo(pdfPath).Length > 5000);
    }
}
