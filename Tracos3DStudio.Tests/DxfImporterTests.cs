using Xunit;

namespace Tracos3DStudio.Tests;

public class DxfImporterTests
{
    [Fact]
    public void ImportFloorPlan_LeLinhasDoDxfExportado()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var drawing = TechnicalDrawingService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"planta-{Guid.NewGuid()}.dxf");

        try
        {
            DxfExporter.ExportFloorPlan(drawing, path);
            var result = DxfImporter.ImportFloorPlan(path);

            Assert.True(result.Walls.Count > 0);
            Assert.True(result.LineCount > 0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
