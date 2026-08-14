using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallSurfaceMaterialTests
{
    [Fact]
    public void FindOption_MaterialEChapaDePiso()
    {
        Assert.NotNull(WallSurfaceMaterialCatalog.FindOption("mdf-branco"));
        Assert.NotNull(WallSurfaceMaterialCatalog.FindOption("ceramica-bege"));
    }

    [Fact]
    public void TryAddDefaultTileRegion_DefineMaterialAzulejo()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _));
        Assert.NotNull(region);
        Assert.Equal("ceramica-bege", region!.MaterialId);
        Assert.Equal("Azulejo", region.Name);
    }

    [Fact]
    public void FaixaRegiaoMaterial_PersisteERestaura()
    {
        var project = new Project();
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        Assert.NotNull(band);
        band!.MaterialId = "mdf-madeirado";

        WallRegionService.TryAddDefaultTileRegion(wall, out _, out _);
        project.Room.SetWalls([wall]);

        var path = Path.Combine(Path.GetTempPath(), $"wall-mat-{Guid.NewGuid()}.tracos");
        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            var loaded = restored.Room.Walls.Single();
            Assert.Equal("mdf-madeirado", loaded.Bands.Single().MaterialId);
            Assert.Equal("ceramica-bege", loaded.Regions.Single().MaterialId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
