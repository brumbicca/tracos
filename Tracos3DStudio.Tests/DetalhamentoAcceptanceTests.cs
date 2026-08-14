using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class DetalhamentoAcceptanceTests
{
    [Fact]
    public void ExportFixture_DetalhamentoScreenshots_ParaManual()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.Name = "Cozinha em L";

        var parts = PartsListService.Build(project);
        var drawing = TechnicalDrawingService.Build(project);

        string screenshotsDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "detalhamento"));

        Directory.CreateDirectory(screenshotsDir);

        string pngPath = Path.Combine(screenshotsDir, "planta-cotas-cozinha-L.png");
        string pdfPath = Path.Combine(screenshotsDir, "pdf-tecnico-cozinha-L.pdf");
        string dxfPiecesPath = Path.Combine(screenshotsDir, "pecas-cozinha-L.dxf");

        TechnicalFloorPlanPngExporter.Export(drawing, pngPath, project.Metadata.Name);
        TechnicalPdfExporter.Export(project, parts, drawing, pdfPath);
        DxfExporter.ExportPieces(parts, dxfPiecesPath);

        Assert.True(File.Exists(pngPath));
        Assert.True(new FileInfo(pdfPath).Length > 2000);
        Assert.Contains("FUROS", File.ReadAllText(dxfPiecesPath));
    }
}
