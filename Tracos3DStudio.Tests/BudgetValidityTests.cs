using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class BudgetValidityTests
{
    [Fact]
    public void Build_PropagaValidadeDoMetadata()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetValidityDays = 45;

        var summary = BudgetService.Build(project);

        Assert.Equal(45, summary.BudgetValidityDays);
    }

    [Fact]
    public void GetBudgetValidUntil_SomaDiasNaDataBase()
    {
        var metadata = new ProjectMetadata { BudgetValidityDays = 30 };
        var baseDate = new DateTime(2026, 6, 19, 15, 30, 0);

        Assert.Equal(new DateTime(2026, 7, 19), metadata.GetBudgetValidUntil(baseDate));
    }

    [Fact]
    public void Persistencia_SalvaERestauraValidade()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.BudgetValidityDays = 15;

        var path = Path.Combine(Path.GetTempPath(), $"validade-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal(15, restored.Metadata.BudgetValidityDays);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportPdf_ArtefatoAceite_ComValidade()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Cliente Validade";
        project.Metadata.BudgetValidityDays = 30;

        var summary = BudgetService.Build(project);
        var pdfPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "orcamento", "fase-D.1-validade-orcamento.pdf"));

        Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
        BudgetPdfExporter.Export(summary, pdfPath);

        Assert.True(new FileInfo(pdfPath).Length > 5000);
    }
}
