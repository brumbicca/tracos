using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModulationRulesTests
{
    [Fact]
    public void RoundTrip_ModulationRules_PreservaEstruturaEPecas()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);

        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "balcao-regras",
                    DisplayName = "Balcão com regras",
                    DefaultWidth = 800f,
                    DefaultHeight = 850f,
                    DefaultDepth = 550f,
                    DoorCount = 2,
                    ModulationRules = rules
                }
            ]
        };

        var path = Path.Combine(Path.GetTempPath(), $"lib-{Guid.NewGuid()}{LibraryPersistence.FileExtension}");

        try
        {
            LibraryPersistence.SaveToFile(document, path);
            var loaded = LibraryPersistence.LoadFromFile(path);

            Assert.Equal(LibraryDocument.CurrentSchemaVersion, loaded.SchemaVersion);
            var module = Assert.Single(loaded.Modules);
            Assert.NotNull(module.ModulationRules);
            Assert.Equal(ModulationTemplateKinds.Box, module.ModulationRules!.TemplateKind);
            Assert.Equal(2, module.ModulationRules.Structure.FrontBays.Count);
            Assert.All(module.ModulationRules.Structure.FrontBays, bay =>
                Assert.Equal(ModulationFrontType.Door, bay.Type));
            Assert.Contains(module.ModulationRules.Pieces, p => p.Role == "lateral" && p.Quantity == 2);
            Assert.Contains(module.ModulationRules.Pieces, p => p.Role == "frente-porta");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_SchemaV1_MigraParaV2SemExigirRegras()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lib-v1-{Guid.NewGuid()}{LibraryPersistence.FileExtension}");
        const string json = """
            {
              "schemaVersion": 1,
              "name": "Legado",
              "modules": [
                {
                  "id": "mod-legado",
                  "displayName": "Legado",
                  "defaultWidth": 600,
                  "defaultHeight": 800,
                  "defaultDepth": 500,
                  "doorCount": 2
                }
              ]
            }
            """;

        try
        {
            File.WriteAllText(path, json);
            var loaded = LibraryPersistence.LoadFromFile(path);

            Assert.Equal(2, loaded.SchemaVersion);
            Assert.Null(loaded.Modules[0].ModulationRules);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ApplyToCatalogs_PropagaModulationRulesParaModuleDefinition()
    {
        ModuleCatalog.ResetUserLibrary();

        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 0, drawerCount: 2);
        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "gaveteiro-regras",
                    DisplayName = "Gaveteiro regras",
                    DefaultWidth = 600f,
                    DefaultHeight = 850f,
                    DefaultDepth = 550f,
                    DrawerCount = 2,
                    ModulationRules = rules
                }
            ]
        };

        LibraryPersistence.ApplyToCatalogs(document);

        Assert.True(ModuleCatalog.TryGet("gaveteiro-regras", out var definition));
        Assert.NotNull(definition!.ModulationRules);
        Assert.Equal(2, definition.ModulationRules!.Structure.FrontBays.Count);
        Assert.All(definition.ModulationRules.Structure.FrontBays, bay =>
            Assert.Equal(ModulationFrontType.Drawer, bay.Type));

        ModuleCatalog.ResetUserLibrary();
    }

    [Fact]
    public void FixtureSample_ModulacaoBalcaoRegras_CarregaDoRepositorio()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples",
            "modulacao-balcao-regras.tracos-lib"));

        Assert.True(File.Exists(path), $"Fixture ausente: {path}");

        var loaded = LibraryPersistence.LoadFromFile(path);
        var module = Assert.Single(loaded.Modules, m => m.Id == "balcao-regras-demo");
        Assert.NotNull(module.ModulationRules);
        Assert.True(module.ModulationRules!.Pieces.Count >= 5);
    }

    [Fact]
    public void CreateStandardBox_Gaveteiro_GeraFrentesPorGaveta()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(0, 3);

        Assert.Equal(3, rules.Structure.FrontBays.Count);
        Assert.Equal(3, rules.Pieces.Count(p => p.Role == "frente-gaveta"));
    }
}
