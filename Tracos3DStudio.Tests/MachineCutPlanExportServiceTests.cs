using System.Text.Json;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class MachineCutPlanExportServiceTests
{
    [Fact]
    public void Build_CozinhaEmL_ContemChapasPosicoesEFuros()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var document = MachineCutPlanExportService.Build(project);

        Assert.Equal(1, document.SchemaVersion);
        Assert.Equal("tracos-cut-plan", document.Format);
        Assert.True(document.Summary.TotalSheets >= 1);
        Assert.True(document.Summary.TotalPlacedPieces > 20);
        Assert.True(document.Summary.OverallUtilizationPercent > 0f);
        Assert.NotEmpty(document.Sheets);

        var firstSheet = document.Sheets[0];
        Assert.NotEmpty(firstSheet.Pieces);
        Assert.True(firstSheet.Pieces[0].SheetXmm >= 0f);
        Assert.True(firstSheet.Pieces[0].LengthMm > 0f);

        Assert.Contains(document.Sheets.SelectMany(s => s.Pieces), p => p.Holes.Count > 0);
    }

    [Fact]
    public void ExportToFile_GeraJsonValido()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var path = Path.Combine(Path.GetTempPath(), $"cut-plan-{Guid.NewGuid()}.json");

        try
        {
            MachineCutPlanExportService.ExportToFile(project, path);

            var content = File.ReadAllText(path);
            Assert.Contains("\"format\": \"tracos-cut-plan\"", content);
            Assert.Contains("\"sheets\"", content);
            Assert.Contains("\"holes\"", content);

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
    public void ExportKitchenLMachineCutPlan_ParaTesteVisual()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "producao", "fase-E.1-amostra-plano-corte-maquina.json");

        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        MachineCutPlanExportService.ExportToFile(project, path);

        Assert.True(new FileInfo(path).Length > 500);
        Assert.Contains("\"tracos-cut-plan\"", File.ReadAllText(path));
    }
}
