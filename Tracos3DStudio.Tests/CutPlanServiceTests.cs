using Xunit;

namespace Tracos3DStudio.Tests;

public class CutPlanServiceTests
{
    [Fact]
    public void CozinhaEmL_GeraPlanoComChapas()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var plan = CutPlanService.Build(project);

        Assert.True(plan.TotalSheets >= 1);
        Assert.True(plan.OverallUtilizationPercent > 0);
        Assert.True(plan.Sheets.Sum(s => s.Placements.Count) > 20);
    }

    [Fact]
    public void TodasPecasSaoPosicionadas()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        int expectedCount = parts.TotalPieceCount;

        var plan = CutPlanService.Build(project);
        int placed = plan.Sheets.Sum(s => s.Placements.Count);

        Assert.Equal(expectedCount, placed);
    }

    [Fact]
    public void ExportCsv_GeraArquivoValido()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var plan = CutPlanService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"corte-{Guid.NewGuid()}.csv");

        try
        {
            CutPlanCsvExporter.Export(plan, path);
            var content = File.ReadAllText(path);
            Assert.Contains("Chapa;Material", content);
            Assert.Contains("Fita_borda", content);
            Assert.True(content.Split('\n').Length > plan.TotalSheets);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ChapaMenor_AumentaNumeroDeChapas()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var defaultPlan = CutPlanService.Build(project);

        project.Metadata.SheetLengthMm = 1200f;
        project.Metadata.SheetWidthMm = 800f;
        var smallPlan = CutPlanService.Build(project);

        Assert.True(smallPlan.TotalSheets >= defaultPlan.TotalSheets);
    }

    [Fact]
    public void CozinhaEmL_MaxRects_MelhorOuIgualAproveitamentoQueGreedy()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var improved = CutPlanService.Build(project);
        var greedy = GreedyCutPlanBenchmark.Build(project);

        Assert.Equal(GreedyCutPlanBenchmark.TotalPieceCount(project), improved.Sheets.Sum(s => s.Placements.Count));
        Assert.True(
            improved.TotalSheets <= greedy.TotalSheets,
            $"Chapas MaxRects={improved.TotalSheets} vs greedy={greedy.TotalSheets}");
        Assert.True(
            improved.OverallUtilizationPercent >= greedy.OverallUtilizationPercent - 0.1f,
            $"Aproveitamento MaxRects={improved.OverallUtilizationPercent:F1}% vs greedy={greedy.OverallUtilizationPercent:F1}%");
    }
}
