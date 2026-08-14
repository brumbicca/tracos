using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class FloorZoneServiceTests
{
    private static readonly List<Vector2> Floor5000 =
    [
        new(150, 150),
        new(4850, 150),
        new(4850, 4850),
        new(150, 4850)
    ];

    [Fact]
    public void TryCreateRectZone_DentroDoPiso_RetornaZonaValida()
    {
        var zone = FloorZoneService.TryCreateRectZone(
            new Vector2(500, 500),
            new Vector2(2000, 2000),
            Floor5000,
            FloorMaterialCatalog.DefaultMaterialId,
            1);

        Assert.NotNull(zone);
        Assert.Equal(1500f, zone!.Width);
        Assert.Equal(1500f, zone.Depth);
    }

    [Fact]
    public void TryCreateRectZone_ForaDoPiso_RetornaNull()
    {
        var zone = FloorZoneService.TryCreateRectZone(
            new Vector2(0, 0),
            new Vector2(1000, 1000),
            Floor5000,
            FloorMaterialCatalog.DefaultMaterialId,
            1);

        Assert.Null(zone);
    }

    [Fact]
    public void TryPickZone_UltimaZonaSobrepoe()
    {
        var zones = new List<FloorZone>
        {
            FloorZone.FromCorners(new Vector2(500, 500), new Vector2(2500, 2500)),
            FloorZone.FromCorners(new Vector2(1000, 1000), new Vector2(1500, 1500))
        };

        zones[0].MaterialId = "porcelanato-branco";
        zones[1].MaterialId = "laminado-madeira";

        Assert.True(FloorZoneService.TryPickZone(zones, new Vector2(1200, 1200), out var picked));
        Assert.Equal("laminado-madeira", picked!.MaterialId);
    }

    [Fact]
    public void TryAddCircleZone_CriaRegiaoCircularNoPiso()
    {
        var floor = new FloorSurface(Floor5000);

        Assert.True(FloorZoneService.TryAddCircleZone(
            floor, 2000f, 2000f, 500f, out var zone, out _));

        Assert.NotNull(zone);
        Assert.Equal(WallRegionShape.Circular, zone!.Shape);
        Assert.Equal(2000f, zone.CenterX, precision: 1);
        Assert.Equal(2000f, zone.CenterY, precision: 1);
        Assert.Equal(500f, zone.RadiusMm, precision: 1);
    }

    [Fact]
    public void TryAddPolygonZone_CriaRegiaoPoligonal()
    {
        var floor = new FloorSurface(Floor5000);
        float[] xs = [1000f, 2000f, 2000f, 1000f];
        float[] ys = [1000f, 1000f, 2000f, 2000f];

        Assert.True(FloorZoneService.TryAddPolygonZone(floor, xs, ys, out var zone, out _));
        Assert.NotNull(zone);
        Assert.Equal(WallRegionShape.Polygon, zone!.Shape);
        Assert.Equal(4, zone.PolygonAlongMm.Count);
    }

    [Fact]
    public void TrySetZoneEdgeOffset_ExpandeAresta()
    {
        var floor = new FloorSurface(Floor5000);
        FloorZoneService.TryAddRectZone(floor, 1000f, 2000f, 1000f, 2000f, out var zone, out _);
        Assert.NotNull(zone);

        Assert.True(FloorZoneService.TrySetZoneEdgeOffset(
            floor, zone!.Id, WallRegionEdgeKind.StartAlong, 40f, out _));

        var (minX, _, _, _) = FloorZoneGeometry.GetEffectiveBounds(
            zone, 0f, 0f, 5000f, 5000f);

        Assert.Equal(zone.MinX - 40f, minX, precision: 1);
    }

    [Fact]
    public void RegiaoPiso_PersisteFormasNoTracos()
    {
        var walls = new List<WallSegment>
        {
            new(new Vector2(0, 0), new Vector2(5000, 0)),
            new(new Vector2(5000, 0), new Vector2(5000, 5000)),
            new(new Vector2(5000, 5000), new Vector2(0, 5000)),
            new(new Vector2(0, 5000), new Vector2(0, 0))
        };

        var project = new Project();
        project.Room.SetWalls(walls);
        var floor = project.Room.Floor!;
        FloorZoneService.TryAddCircleZone(floor, 1500f, 1500f, 400f, out var circle, out _);
        Assert.NotNull(circle);
        circle!.OffsetMm = 20f;

        var path = Path.Combine(Path.GetTempPath(), $"piso-reg-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path)).Room.Floor!;
            var loadedZone = loaded.Zones.Single();

            Assert.Equal(WallRegionShape.Circular, loadedZone.Shape);
            Assert.Equal(1500f, loadedZone.CenterX, precision: 1);
            Assert.Equal(400f, loadedZone.RadiusMm, precision: 1);
            Assert.Equal(20f, loadedZone.OffsetMm, precision: 1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

public class FloorMaterialCatalogTests
{
    [Fact]
    public void GetDefault_RetornaPorcelanatoBranco()
    {
        var material = FloorMaterialCatalog.GetDefault();
        Assert.Equal(FloorMaterialCatalog.DefaultMaterialId, material.Id);
        Assert.Equal(FloorMaterialPattern.Tile, material.Pattern);
    }
}

public class FloorPersistenceTests
{
    [Fact]
    public void SaveLoad_PreservaMaterialEZonaDoPiso()
    {
        var walls = new List<WallSegment>
        {
            new(new Vector2(0, 0), new Vector2(5000, 0)),
            new(new Vector2(5000, 0), new Vector2(5000, 5000)),
            new(new Vector2(5000, 5000), new Vector2(0, 5000)),
            new(new Vector2(0, 5000), new Vector2(0, 0))
        };

        var project = new Project();
        project.Room.SetWalls(walls);
        Assert.NotNull(project.Room.Floor);

        project.Room.Floor!.DefaultMaterialId = "laminado-madeira";
        project.Room.Floor.Zones.Add(new FloorZone
        {
            MaterialId = "porcelanato-cinza",
            MinX = 500,
            MinY = 500,
            MaxX = 2000,
            MaxY = 2000,
            Name = "Entrada"
        });

        var document = ProjectPersistence.CreateFromProject(project);
        var loaded = ProjectPersistence.LoadProject(document);

        Assert.Equal("laminado-madeira", loaded.Room.Floor!.DefaultMaterialId);
        Assert.Single(loaded.Room.Floor.Zones);
        Assert.Equal("porcelanato-cinza", loaded.Room.Floor.Zones[0].MaterialId);
        Assert.Equal("Entrada", loaded.Room.Floor.Zones[0].Name);
    }
}
