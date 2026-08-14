using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleLayerTests
{
    [Fact]
    public void TryAddCustomLayer_CriaIdUnicoEVisivel()
    {
        var metadata = new ProjectMetadata();
        Assert.True(WallLayerCatalog.TryAddCustomLayer(metadata, "Decoração", out string layerId, out _));
        Assert.Equal("decoracao", layerId);
        Assert.True(WallLayerCatalog.IsLayerVisible(metadata, layerId));
        Assert.Equal("Decoração", metadata.CustomLayerNames![layerId]);
    }

    [Fact]
    public void TryAddCustomLayer_EvitaColisaoDeId()
    {
        var metadata = new ProjectMetadata();
        metadata.CustomLayerNames = new Dictionary<string, string> { ["decoracao"] = "Decoração" };

        Assert.True(WallLayerCatalog.TryAddCustomLayer(metadata, "Decoração", out string layerId, out _));
        Assert.Equal("decoracao-2", layerId);
    }

    [Fact]
    public void SetLayerLocked_ImpedePickNaCamada()
    {
        var metadata = new ProjectMetadata();
        WallLayerCatalog.SetLayerLocked(metadata, "modulo", true);

        Assert.True(WallLayerCatalog.IsLayerLocked(metadata, "modulo"));
        Assert.False(WallLayerCatalog.CanPickOnLayer(metadata, "modulo"));
        Assert.True(WallLayerCatalog.CanPickOnLayer(metadata, "parede"));
    }

    [Fact]
    public void CountModulesOnLayer_ContaPorCamada()
    {
        var project = new Project();
        project.Modules.Add(new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            LayerId = "modulo"
        });
        project.Modules.Add(new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            LayerId = "decoracao"
        });

        Assert.Equal(1, WallLayerCatalog.CountModulesOnLayer(project.Modules, "modulo"));
        Assert.Equal(1, WallLayerCatalog.CountModulesOnLayer(project.Modules, "decoracao"));
        Assert.Equal(0, WallLayerCatalog.CountModulesOnLayer(project.Modules, "parede"));
    }

    [Fact]
    public void GetDefinitions_IncluiModuloECamadaCustom()
    {
        var metadata = new ProjectMetadata();
        WallLayerCatalog.TryAddCustomLayer(metadata, "Iluminação", out _, out _);

        var ids = WallLayerCatalog.GetDefinitions(metadata).Select(d => d.Id).ToList();

        Assert.Contains("modulo", ids);
        Assert.Contains("parede", ids);
        Assert.Contains("iluminacao", ids);
    }

    [Fact]
    public void TryRemoveEmptyCustomLayers_RemoveCamadaCustomSemItens()
    {
        var project = new Project();
        WallLayerCatalog.TryAddCustomLayer(project.Metadata, "Decoração", out string emptyId, out _);
        WallLayerCatalog.TryAddCustomLayer(project.Metadata, "Iluminação", out string usedId, out _);
        WallLayerCatalog.SetLayerLocked(project.Metadata, emptyId, true);
        WallLayerCatalog.SetLayerVisible(project.Metadata, emptyId, false);

        project.Modules.Add(new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            LayerId = usedId
        });

        int removed = WallLayerCatalog.TryRemoveEmptyCustomLayers(
            project.Metadata,
            project.Room.Walls,
            project.Modules,
            out IReadOnlyList<string> names);

        Assert.Equal(1, removed);
        Assert.Equal(["Decoração"], names);
        Assert.DoesNotContain(emptyId, project.Metadata.CustomLayerNames!.Keys);
        Assert.Contains(usedId, project.Metadata.CustomLayerNames.Keys);
        Assert.False(project.Metadata.WallLayerVisibility!.ContainsKey(emptyId));
        Assert.False(project.Metadata.LayerLocked!.ContainsKey(emptyId));
    }

    [Fact]
    public void TryRemoveEmptyCustomLayers_NaoRemoveCamadaComParede()
    {
        var project = new Project();
        WallLayerCatalog.TryAddCustomLayer(project.Metadata, "Divisória extra", out string layerId, out _);

        project.Room.Walls.Add(new WallSegment(new Vector2(0, 0), new Vector2(1000, 0))
        {
            LayerId = layerId
        });

        int removed = WallLayerCatalog.TryRemoveEmptyCustomLayers(
            project.Metadata,
            project.Room.Walls,
            project.Modules,
            out _);

        Assert.Equal(0, removed);
        Assert.Contains(layerId, project.Metadata.CustomLayerNames!.Keys);
    }

    [Fact]
    public void GetEmptyCustomLayers_IgnoraCamadasEmbutidas()
    {
        var project = new Project();

        var empty = WallLayerCatalog.GetEmptyCustomLayers(
            project.Metadata,
            project.Room.Walls,
            project.Modules);

        Assert.DoesNotContain(empty, layer => layer.Id == "parede");
        Assert.DoesNotContain(empty, layer => layer.Id == "modulo");
    }

    [Fact]
    public void LayerFillMode_PersisteERestaura()
    {
        var project = new Project();
        WallLayerCatalog.SetLayerFillMode(project.Metadata, "referencia", LayerFillMode.Ghost);
        WallLayerCatalog.SetLayerFillMode(project.Metadata, "divisoria", LayerFillMode.OutlineOnly);

        var path = Path.Combine(Path.GetTempPath(), $"layer-fill-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal(LayerFillMode.Ghost, WallLayerCatalog.GetLayerFillMode(restored.Metadata, "referencia"));
            Assert.Equal(LayerFillMode.OutlineOnly, WallLayerCatalog.GetLayerFillMode(restored.Metadata, "divisoria"));
            Assert.Equal(LayerFillMode.Default, WallLayerCatalog.GetLayerFillMode(restored.Metadata, "parede"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void SetLayerFillMode_DefaultRemoveEntrada()
    {
        var metadata = new ProjectMetadata();
        WallLayerCatalog.SetLayerFillMode(metadata, "parede", LayerFillMode.Ghost);
        WallLayerCatalog.SetLayerFillMode(metadata, "parede", LayerFillMode.Default);

        Assert.Equal(LayerFillMode.Default, WallLayerCatalog.GetLayerFillMode(metadata, "parede"));
        Assert.Null(metadata.LayerFillModes);
    }

    [Fact]
    public void RemoveEmptyCustomLayer_LimpaFillMode()
    {
        var project = new Project();
        WallLayerCatalog.TryAddCustomLayer(project.Metadata, "Vazia", out string layerId, out _);
        WallLayerCatalog.SetLayerFillMode(project.Metadata, layerId, LayerFillMode.OutlineOnly);

        WallLayerCatalog.TryRemoveEmptyCustomLayers(
            project.Metadata,
            project.Room.Walls,
            project.Modules,
            out _);

        Assert.Null(project.Metadata.LayerFillModes);
    }

    [Fact]
    public void ModuloCamada_PersisteERestaura()
    {
        var project = new Project();
        WallLayerCatalog.TryAddCustomLayer(project.Metadata, "Iluminação", out string customId, out _);
        WallLayerCatalog.SetLayerLocked(project.Metadata, customId, true);

        var module = project.AddModule("balcao-2-portas", Vector3.Zero);
        module.LayerId = customId;

        var path = Path.Combine(Path.GetTempPath(), $"modulo-camada-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal(customId, restored.Modules.Single().LayerId);
            Assert.True(restored.Metadata.LayerLocked![customId]);
            Assert.Equal("Iluminação", restored.Metadata.CustomLayerNames![customId]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
