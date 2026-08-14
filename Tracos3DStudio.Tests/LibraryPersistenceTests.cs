using Xunit;

namespace Tracos3DStudio.Tests;

public class LibraryPersistenceTests
{
    [Fact]
    public void RoundTrip_ModuloCustomizado_PreservaDados()
    {
        var document = new LibraryDocument
        {
            Name = "Teste Marcenaria",
            Modules =
            [
                new CustomModuleData
                {
                    Id = "balcao-teste",
                    DisplayName = "Balcão Teste",
                    DefaultWidth = 700f,
                    DefaultHeight = 800f,
                    DefaultDepth = 500f,
                    DoorCount = 2
                }
            ],
            ModulePrices = new Dictionary<string, decimal> { ["balcao-teste"] = 1500m }
        };

        var path = Path.Combine(Path.GetTempPath(), $"lib-{Guid.NewGuid()}{LibraryPersistence.FileExtension}");

        try
        {
            LibraryPersistence.SaveToFile(document, path);
            var loaded = LibraryPersistence.LoadFromFile(path);

            Assert.Equal("Teste Marcenaria", loaded.Name);
            Assert.Single(loaded.Modules);
            Assert.Equal("balcao-teste", loaded.Modules[0].Id);
            Assert.Equal(1500m, loaded.ModulePrices!["balcao-teste"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ApplyToCatalogs_RegistraModuloCustomizado()
    {
        ModuleCatalog.ResetUserLibrary();

        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "mod-custom",
                    DisplayName = "Custom",
                    DefaultWidth = 500f,
                    DefaultHeight = 700f,
                    DefaultDepth = 400f
                }
            ]
        };

        LibraryPersistence.ApplyToCatalogs(document);

        Assert.True(ModuleCatalog.TryGet("mod-custom", out var definition));
        Assert.NotNull(definition);
        Assert.Equal("Custom", definition.DisplayName);
        Assert.True(ModuleCatalog.IsCustom("mod-custom"));

        ModuleCatalog.ResetUserLibrary();
    }

    [Fact]
    public void ApplyToCatalogs_SobrescreveModuloBuiltInSemRecompilar()
    {
        ModuleCatalog.ResetUserLibrary();

        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "balcao-2-portas",
                    DisplayName = "Balcão 2P Premium",
                    DefaultWidth = 800f,
                    DefaultHeight = 850f,
                    DefaultDepth = 550f,
                    DoorCount = 2
                }
            ]
        };

        LibraryPersistence.ApplyToCatalogs(document);

        Assert.True(ModuleCatalog.HasBuiltInOverride("balcao-2-portas"));
        Assert.Equal("Balcão 2P Premium", ModuleCatalog.GetRequired("balcao-2-portas").DisplayName);
        Assert.False(ModuleCatalog.IsCustom("balcao-2-portas"));

        ModuleCatalog.ResetUserLibrary();
    }

    [Fact]
    public void ReloadFromFile_AplicaDocumentoNoCatalogo()
    {
        ModuleCatalog.ResetUserLibrary();

        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "reload-test",
                    DisplayName = "Reload Test",
                    DefaultWidth = 600f,
                    DefaultHeight = 800f,
                    DefaultDepth = 500f
                }
            ]
        };

        var path = Path.Combine(Path.GetTempPath(), $"lib-{Guid.NewGuid()}{LibraryPersistence.FileExtension}");

        try
        {
            LibraryPersistence.SaveToFile(document, path);
            LibraryReloadService.ReloadFromFile(path);

            Assert.True(ModuleCatalog.TryGet("reload-test", out var definition));
            Assert.Equal("Reload Test", definition!.DisplayName);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);

            ModuleCatalog.ResetUserLibrary();
        }
    }
}
