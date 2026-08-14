using Xunit;

namespace Tracos3DStudio.Tests;

public class PartLabelsServiceTests
{
    [Fact]
    public void Build_CozinhaEmL_ExpandeQuantidades()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        var labels = PartLabelsService.Build(project);

        Assert.Equal(parts.TotalPieceCount, labels.TotalCount);
        Assert.Equal(labels.TotalCount, labels.Labels[^1].Total);
        Assert.Equal(1, labels.Labels[0].Index);
        Assert.All(labels.Labels, l => Assert.False(string.IsNullOrWhiteSpace(l.ShortCode)));
    }
}

public class PartLabelsPdfExporterTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Export_GeraArquivoPdf()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var labels = PartLabelsService.Build(project);
        var path = Path.Combine(Path.GetTempPath(), $"etiquetas-{Guid.NewGuid()}.pdf");

        try
        {
            PartLabelsPdfExporter.Export(labels, path);
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
    public void Export_ArtefatoAceite_GeraPdfNaRaiz()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var labels = PartLabelsService.Build(project);
        var path = Path.Combine(RepoRoot, "fase-5-etiquetas.pdf");

        PartLabelsPdfExporter.Export(labels, path);

        Assert.True(File.Exists(path));
        Assert.True(labels.TotalCount > 20);
    }
}
