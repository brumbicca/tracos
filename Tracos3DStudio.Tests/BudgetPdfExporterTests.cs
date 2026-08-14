using Xunit;

namespace Tracos3DStudio.Tests;

public class BudgetPdfExporterTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Export_GeraArquivoPdf()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Cliente Teste";
        var summary = BudgetService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"orcamento-{Guid.NewGuid()}.pdf");

        try
        {
            BudgetPdfExporter.Export(summary, path);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 1000);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Export_ArtefatosAceite_GeraPdfComViewport3D()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.Name = "Cozinha em L";
        project.Metadata.ClientName = "Maria Silva";
        project.Metadata.ClientPhone = "(11) 99999-0000";
        project.Metadata.ClientCity = "São Paulo";
        project.Metadata.ClientState = "SP";

        var summary = BudgetService.Build(project);
        var pdfPath = Path.Combine(RepoRoot, "fase-3-orcamento.pdf");
        var viewportPath = Path.Combine(RepoRoot, "docs", "screenshots", "fase-6", "fase-6-biblioteca.png");

        byte[]? viewportPng = File.Exists(viewportPath)
            ? File.ReadAllBytes(viewportPath)
            : null;

        BudgetPdfExporter.Export(summary, pdfPath, viewportPng);

        Assert.True(File.Exists(pdfPath));
        Assert.True(new FileInfo(pdfPath).Length > 5000);
    }
}
