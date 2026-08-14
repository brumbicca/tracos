using Xunit;

namespace Tracos3DStudio.Tests;

public class Phase6AcceptanceTests
{
    [Fact]
    public void PerfilReforcado_AplicaEspessura25mm()
    {
        var project = new Project();
        ConstructionProfiles.Apply(project, ConstructionProfiles.Reforcado);

        Assert.Equal(25f, project.Metadata.PanelThicknessMm);
        Assert.Equal(ConstructionProfiles.Reforcado, project.Metadata.ConstructionProfileId);
    }

    [Fact]
    public void ErpExport_ContemOrcamentoPecasECorte()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var document = ErpExportService.Build(project, "Biblioteca teste");

        Assert.Equal(4, document.ModuleCount);
        Assert.Equal(4, document.Budget.Items.Count);
        Assert.True(document.Parts.TotalPieceCount > 0);
        Assert.True(document.CutPlan.TotalSheets >= 1);
        Assert.Equal("Biblioteca teste", document.LibraryName);
    }

    [Fact]
    public void ExportErp_GeraJson()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var path = Path.Combine(Path.GetTempPath(), $"erp-{Guid.NewGuid()}.json");

        try
        {
            ErpExportService.ExportToFile(project, path);
            var content = File.ReadAllText(path);
            Assert.Contains("\"budget\"", content);
            Assert.Contains("\"cutPlan\"", content);
            Assert.Contains("\"parts\"", content);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BackupZip_ContemProjeto()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var document = ProjectPersistence.CreateFromProject(project);
        var projectPath = Path.Combine(Path.GetTempPath(), $"proj-{Guid.NewGuid()}.tracos");
        var zipPath = Path.Combine(Path.GetTempPath(), $"backup-{Guid.NewGuid()}.zip");

        try
        {
            ProjectPersistence.SaveToFile(document, projectPath);
            ProjectBackupService.ExportZip(projectPath, zipPath);

            using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
            Assert.Contains(archive.Entries, e => e.Name.EndsWith(".tracos", StringComparison.Ordinal));
            Assert.Contains(archive.Entries, e => e.Name == "backup-manifest.json");
        }
        finally
        {
            if (File.Exists(projectPath))
                File.Delete(projectPath);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }

    [Fact]
    public void PrecoBiblioteca_UsadoNoOrcamento()
    {
        ModuleCatalog.ResetUserLibrary();
        LibraryState.ModulePrices = new Dictionary<string, decimal>
        {
            ["balcao-2-portas"] = 9999m
        };

        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var summary = BudgetService.Build(project);
        var item = summary.Items.First(i => i.DefinitionId == "balcao-2-portas");

        Assert.Equal(9999m, item.BasePrice);

        LibraryState.ModulePrices = null;
    }
}
