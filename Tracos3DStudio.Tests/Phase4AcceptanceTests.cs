using Xunit;

namespace Tracos3DStudio.Tests;

public class Phase4AcceptanceTests
{
    [Fact]
    public void CozinhaEmL_ListaPecas_CorrespondeAosModulos()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var summary = PartsListService.Build(project);

        Assert.Equal(4, project.Modules.Count);
        Assert.True(summary.TotalPieceCount > 20);
        Assert.Equal(4, summary.Items.Select(p => p.ModuleId).Distinct().Count());
        Assert.All(summary.Items, p => Assert.True(p.LengthMm > 0));
    }

    [Fact]
    public void CozinhaEmL_PlantaTemParedesEModulos()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var drawing = TechnicalDrawingService.Build(project);

        Assert.Equal(4, drawing.FloorPlanWalls.Count);
        Assert.Equal(4, drawing.FloorPlanModules.Count);
        Assert.Equal(4, drawing.FloorPlanDimensions.Count);
        Assert.True(drawing.Elevations.Count >= 1);
    }

    [Fact]
    public void ExportDxf_Planta_GeraArquivo()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var drawing = TechnicalDrawingService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"planta-{Guid.NewGuid()}.dxf");

        try
        {
            DxfExporter.ExportFloorPlan(drawing, path);
            var content = File.ReadAllText(path);
            Assert.Contains("SECTION", content);
            Assert.Contains("PAREDES", content);
            Assert.Contains("MODULOS", content);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportDxf_Pecas_ContornosEFuros()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"pecas-{Guid.NewGuid()}.dxf");

        try
        {
            DxfExporter.ExportPieces(parts, path);
            var content = File.ReadAllText(path);
            Assert.Contains("PECAS", content);
            Assert.Contains("FUROS", content);
            Assert.Contains("CIRCLE", content);
            Assert.Contains(parts.Items, p => p.Holes.Any(h => h.Kind == DrillHoleKind.MinifixCam));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TechnicalPdf_ListaIncluiMinifixEDobradica()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);

        Assert.Contains(parts.Items, p => p.Holes.Any(h => h.Kind == DrillHoleKind.HingeCup));
        Assert.Contains(parts.Items, p => p.Holes.Any(h => h.Kind == DrillHoleKind.MinifixCam));
        Assert.Contains(parts.Items, p => p.DrillingText.Contains("Minifix", StringComparison.Ordinal));
    }

    [Fact]
    public void ExportTechnicalPdf_GeraArquivo()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        var drawing = TechnicalDrawingService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"tecnico-{Guid.NewGuid()}.pdf");

        try
        {
            TechnicalPdfExporter.Export(project, parts, drawing, path);
            Assert.True(new FileInfo(path).Length > 2000);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
