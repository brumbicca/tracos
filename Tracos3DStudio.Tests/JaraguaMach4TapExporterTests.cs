using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class JaraguaMach4TapExporterTests
{
    [Fact]
    public void Export_CozinhaEmL_ContemHeaderMach4EFooter()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var job = CncJobExporter.Build(MachineCutPlanExportService.Build(project));
        string tap = JaraguaMach4TapExporter.Export(job);

        Assert.Contains("(JrgCnC - Vision V1.01 - by Aspire)", tap);
        Assert.Contains("M6T3", tap);
        Assert.Contains("S18000", tap);
        Assert.Contains("G0 G43 H3", tap);
        Assert.Contains("(Corte 1)", tap);
        Assert.Contains("G2X", tap);
        Assert.Contains("Z-0.1", tap);
        Assert.Contains("G0 G53 Z0", tap);
        Assert.Contains("M30", tap);
        Assert.EndsWith("%" + Environment.NewLine, tap);
    }

    [Fact]
    public void Export_CozinhaEmL_GeraCortesEFuros()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var job = CncJobExporter.Build(MachineCutPlanExportService.Build(project));
        string tap = JaraguaMach4TapExporter.Export(job);

        int cutCount = job.Sheets[0].Operations.Count(o => o is CncCutOperation);
        Assert.Equal(cutCount, CountOccurrences(tap, "(Corte "));

        Assert.Contains("G1X", tap);
        Assert.True(tap.Length > 5000);
    }

    [Fact]
    public void ExportToFile_MultiplasChapas_GeraArquivosSufixados()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var job = CncJobExporter.Build(MachineCutPlanExportService.Build(project));
        Assert.True(job.Sheets.Count > 1);

        string directory = Path.Combine(Path.GetTempPath(), $"jaragua-tap-{Guid.NewGuid()}");
        Directory.CreateDirectory(directory);
        string basePath = Path.Combine(directory, "cozinha.tap");

        try
        {
            JaraguaMach4TapExporter.ExportToFile(job, basePath);

            foreach (var sheet in job.Sheets)
            {
                string sheetPath = Path.Combine(directory, $"cozinha-chapa-{sheet.Index:D2}.tap");
                Assert.True(File.Exists(sheetPath), sheetPath);
                Assert.Contains("M6T3", File.ReadAllText(sheetPath));
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExportKitchenL_ParaAmostraDocumentacao()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var folder = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "producao"));
        var basePath = Path.Combine(folder, "fase-E.4-amostra-cozinha.tap");
        var sheetPath = Path.Combine(folder, "fase-E.4-amostra-cozinha-chapa-01.tap");

        Directory.CreateDirectory(folder);
        MachineCutPlanExportService.ExportJaraguaTap(project, basePath);

        Assert.True(File.Exists(sheetPath), sheetPath);
        Assert.True(new FileInfo(sheetPath).Length > 1000);
        Assert.Contains("JrgCnC", File.ReadAllText(sheetPath));
    }

    [Fact]
    public void ExportKitchenL_DxfChapasNesting_ParaComparacaoTap()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.Name = "Cozinha em L";

        string samplesDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples"));
        string docsDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "producao"));

        Directory.CreateDirectory(samplesDir);
        Directory.CreateDirectory(docsDir);

        string basePath = Path.Combine(samplesDir, "fase-2-cozinha-L.dxf");
        MachineCutPlanExportService.ExportCutPlanDxf(project, basePath);

        var chapa1 = Path.Combine(samplesDir, "fase-2-cozinha-L-chapa-01.dxf");
        Assert.True(File.Exists(chapa1));
        Assert.Contains("CHAPA", File.ReadAllText(chapa1));
        Assert.Contains("PECAS", File.ReadAllText(chapa1));

        File.Copy(chapa1, Path.Combine(docsDir, "fase-E.4-comparacao-chapa-01.dxf"), overwrite: true);

        var parts = PartsListService.Build(project);
        DxfExporter.ExportPieces(parts, Path.Combine(samplesDir, "fase-2-cozinha-L-pecas.dxf"));
        Assert.True(File.Exists(Path.Combine(samplesDir, "fase-2-cozinha-L-pecas.dxf")));
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
