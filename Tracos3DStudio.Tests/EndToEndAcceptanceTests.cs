using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Aceite ponta a ponta: persistência, orçamento, corte e etiquetas.</summary>
public sealed class EndToEndAcceptanceTests
{
    [Fact]
    public void CozinhaEmL_SalvarReabrir_OrcamentoCorteEtiquetasExportam()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        project.Metadata.ClientName = "Aceite E2E";
        project.Metadata.Name = "Cozinha em L";

        var path = Path.Combine(Path.GetTempPath(), $"e2e-{Guid.NewGuid()}.tracos");
        var budgetPdf = Path.Combine(Path.GetTempPath(), $"e2e-orcamento-{Guid.NewGuid()}.pdf");
        var cutCsv = Path.Combine(Path.GetTempPath(), $"e2e-corte-{Guid.NewGuid()}.csv");
        var cutJson = Path.Combine(Path.GetTempPath(), $"e2e-corte-maquina-{Guid.NewGuid()}.json");
        var cutDrillCsv = Path.Combine(Path.GetTempPath(), $"e2e-furos-cnc-{Guid.NewGuid()}.csv");
        var cutCncJob = Path.Combine(Path.GetTempPath(), $"e2e-cnc-job-{Guid.NewGuid()}.json");
        var labelsPdf = Path.Combine(Path.GetTempPath(), $"e2e-etiquetas-{Guid.NewGuid()}.pdf");
        var technicalPdf = Path.Combine(Path.GetTempPath(), $"e2e-tecnico-{Guid.NewGuid()}.pdf");
        var dxfPlanta = Path.Combine(Path.GetTempPath(), $"e2e-planta-{Guid.NewGuid()}.dxf");
        var dxfPecas = Path.Combine(Path.GetTempPath(), $"e2e-pecas-{Guid.NewGuid()}.dxf");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.True(restored.Room.IsClosed);
            Assert.Equal(4, restored.Modules.Count);
            Assert.All(restored.Modules, m => Assert.True(m.AttachedWallId.HasValue));

            var budget = BudgetService.Build(restored);
            Assert.True(budget.GrandTotal > 0m);
            Assert.Equal(4, budget.Items.Count);

            var cutPlan = CutPlanService.Build(restored);
            Assert.True(cutPlan.TotalSheets >= 1);
            Assert.InRange(cutPlan.OverallUtilizationPercent, 1f, 100f);

            var labels = PartLabelsService.Build(restored);
            Assert.True(labels.TotalCount > 20);

            var parts = PartsListService.Build(restored);
            var drawing = TechnicalDrawingService.Build(restored);

            BudgetPdfExporter.Export(budget, budgetPdf);
            CutPlanCsvExporter.Export(cutPlan, cutCsv);
            MachineCutPlanExportService.ExportToFile(restored, cutJson);
            MachineCutPlanExportService.ExportDrillCsv(restored, cutDrillCsv);
            MachineCutPlanExportService.ExportCncJob(restored, cutCncJob);
            PartLabelsPdfExporter.Export(labels, labelsPdf);
            TechnicalPdfExporter.Export(restored, parts, drawing, technicalPdf);
            DxfExporter.ExportFloorPlan(drawing, dxfPlanta);
            DxfExporter.ExportPieces(parts, dxfPecas);

            Assert.True(new FileInfo(budgetPdf).Length > 5000);
            Assert.True(new FileInfo(cutCsv).Length > 100);
            Assert.Contains("\"tracos-cut-plan\"", File.ReadAllText(cutJson));
            Assert.True(new FileInfo(cutJson).Length > 500);
            Assert.Contains("ChapaX_mm", File.ReadAllText(cutDrillCsv));
            Assert.True(new FileInfo(cutDrillCsv).Length > 100);
            Assert.Contains("\"tracos-cnc-job\"", File.ReadAllText(cutCncJob));
            Assert.True(new FileInfo(cutCncJob).Length > 500);
            Assert.True(new FileInfo(labelsPdf).Length > 1000);
            Assert.True(new FileInfo(technicalPdf).Length > 2000);
            Assert.Contains("PAREDES", File.ReadAllText(dxfPlanta));
            Assert.Contains("PECAS", File.ReadAllText(dxfPecas));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(budgetPdf))
                File.Delete(budgetPdf);
            if (File.Exists(cutCsv))
                File.Delete(cutCsv);
            if (File.Exists(cutJson))
                File.Delete(cutJson);
            if (File.Exists(cutDrillCsv))
                File.Delete(cutDrillCsv);
            if (File.Exists(cutCncJob))
                File.Delete(cutCncJob);
            if (File.Exists(labelsPdf))
                File.Delete(labelsPdf);
            if (File.Exists(technicalPdf))
                File.Delete(technicalPdf);
            if (File.Exists(dxfPlanta))
                File.Delete(dxfPlanta);
            if (File.Exists(dxfPecas))
                File.Delete(dxfPecas);
        }
    }
}
