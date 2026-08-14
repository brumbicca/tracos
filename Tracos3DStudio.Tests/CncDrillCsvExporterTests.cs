using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class CncDrillCsvExporterTests
{
    [Fact]
    public void ToSheetCoordinates_SemRotacao_SomaOrigemDaPeca()
    {
        var placement = new MachineCutPlacement
        {
            InstanceId = 1,
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão",
            PieceName = "Frente porta 1",
            SheetXmm = 100,
            SheetYmm = 200,
            LengthMm = 800,
            WidthMm = 550,
            Rotated = false,
            Holes =
            [
                new MachineCutHole
                {
                    Kind = DrillHoleKind.HingeCup,
                    Edge = DrillHoleEdge.Left,
                    PosXmm = 22,
                    PosYmm = 100,
                    DiameterMm = 35,
                    DepthMm = 13
                }
            ]
        };

        var (sheetX, sheetY) = CncDrillCoordinateService.ToSheetCoordinates(placement, placement.Holes[0]);

        Assert.Equal(122f, sheetX, precision: 2);
        Assert.Equal(300f, sheetY, precision: 2);
    }

    [Fact]
    public void ToSheetCoordinates_ComRotacao90Graus_ConverteCoordenadas()
    {
        var placement = new MachineCutPlacement
        {
            InstanceId = 2,
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão",
            PieceName = "Frente porta 1",
            SheetXmm = 50,
            SheetYmm = 60,
            LengthMm = 550,
            WidthMm = 800,
            Rotated = true,
            Holes =
            [
                new MachineCutHole
                {
                    Kind = DrillHoleKind.HingeCup,
                    Edge = DrillHoleEdge.Left,
                    PosXmm = 22,
                    PosYmm = 100,
                    DiameterMm = 35,
                    DepthMm = 13
                }
            ]
        };

        var (sheetX, sheetY) = CncDrillCoordinateService.ToSheetCoordinates(placement, placement.Holes[0]);

        Assert.Equal(150f, sheetX, precision: 2);
        Assert.Equal(838f, sheetY, precision: 2);
    }

    [Fact]
    public void Export_CozinhaEmL_GeraLinhasComFurosNaChapa()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var document = MachineCutPlanExportService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"cnc-drill-{Guid.NewGuid()}.csv");

        try
        {
            CncDrillCsvExporter.Export(document, path);

            var content = File.ReadAllText(path);
            Assert.Contains("ChapaX_mm", content);
            Assert.Contains("HingeCup", content);
            Assert.True(CncDrillCsvExporter.CountDrillRows(document) > 10);
            Assert.StartsWith("Chapa;", content);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportKitchenLDrills_ParaTesteVisual()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var document = MachineCutPlanExportService.Build(project);
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "docs", "screenshots", "producao", "fase-E.2-furos-cnc.csv"));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        CncDrillCsvExporter.Export(document, path);

        Assert.True(new FileInfo(path).Length > 200);
        Assert.True(CncDrillCsvExporter.CountDrillRows(document) > 0);
    }
}
