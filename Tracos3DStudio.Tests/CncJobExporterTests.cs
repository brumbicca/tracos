using System.Text.Json;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class CncJobExporterTests
{
    [Fact]
    public void BuildCutContour_RetanguloFechadoNaChapa()
    {
        var placement = new MachineCutPlacement
        {
            InstanceId = 1,
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão",
            PieceName = "Lateral",
            SheetXmm = 10,
            SheetYmm = 20,
            LengthMm = 800,
            WidthMm = 550,
            Rotated = false
        };

        var contour = CncJobExporter.BuildCutContour(placement);

        Assert.Equal(4, contour.Count);
        Assert.Equal([10f, 20f], contour[0]);
        Assert.Equal([810f, 20f], contour[1]);
        Assert.Equal([810f, 570f], contour[2]);
        Assert.Equal([10f, 570f], contour[3]);
    }

    [Fact]
    public void Build_CozinhaEmL_ContemCortesEFurosNaChapa()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var source = MachineCutPlanExportService.Build(project);
        var job = CncJobExporter.Build(source);

        Assert.Equal(1, job.SchemaVersion);
        Assert.Equal("tracos-cnc-job", job.Format);
        Assert.Equal("mm", job.Units);
        Assert.True(job.Summary.TotalCutOperations > 20);
        Assert.True(job.Summary.TotalDrillOperations > 10);
        Assert.Equal(job.Summary.TotalCutOperations, job.Sheets.Sum(s => s.Operations.Count(o => o is CncCutOperation)));
        Assert.Equal(job.Summary.TotalDrillOperations, job.Sheets.Sum(s => s.Operations.Count(o => o is CncDrillOperation)));

        var drill = job.Sheets.SelectMany(s => s.Operations).OfType<CncDrillOperation>().First();
        Assert.True(drill.SheetXmm > 0f);
        Assert.True(drill.DiameterMm > 0f);

        var cut = job.Sheets.SelectMany(s => s.Operations).OfType<CncCutOperation>().First();
        Assert.Equal(4, cut.ContourMm.Count);
    }

    [Fact]
    public void Export_GeraJsonValidoComDiscriminador()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var source = MachineCutPlanExportService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"cnc-job-{Guid.NewGuid()}.json");

        try
        {
            CncJobExporter.Export(source, path);

            var content = File.ReadAllText(path);
            Assert.Contains("\"format\": \"tracos-cnc-job\"", content);
            Assert.Contains("\"type\": \"cut\"", content);
            Assert.Contains("\"type\": \"drill\"", content);
            Assert.Contains("\"contourMm\"", content);
            Assert.Contains("\"sheetXmm\"", content);

            using var json = JsonDocument.Parse(content);
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("sheets").ValueKind);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportKitchenLCncJob_ParaTesteVisual()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var source = MachineCutPlanExportService.Build(project);
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "producao", "fase-E.3-amostra-cnc-job.json"));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        CncJobExporter.Export(source, path);

        Assert.True(new FileInfo(path).Length > 1000);
        Assert.Contains("\"tracos-cnc-job\"", File.ReadAllText(path));
    }
}
