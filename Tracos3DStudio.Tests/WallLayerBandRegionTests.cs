using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class WallLayerBandRegionTests
{
    [Fact]
    public void TryAddDefaultUpperBand_AdicionaFaixaHorizontal()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallBandService.TryAddDefaultUpperBand(wall, out var band, out _));
        Assert.NotNull(band);
        Assert.Single(wall.Bands);
        Assert.Equal(2100f, band!.StartMm, precision: 1);
        Assert.Equal(2600f, band.EndMm, precision: 1);
    }

    [Fact]
    public void TryAddDefaultTileRegion_RegiaoNaFaceInterna()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _));
        Assert.NotNull(region);
        Assert.Equal(FaceType.Internal, region!.Face);
        Assert.Equal(1100f, region.BottomMm, precision: 1);
        Assert.Equal(2100f, region.TopMm, precision: 1);
    }

    [Fact]
    public void TryAddVerticalBandAtCenter_CriaFaixaVertical()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallBandService.TryAddVerticalBandAtCenter(wall, 2500f, out var band, out _));
        Assert.NotNull(band);
        Assert.False(band!.IsHorizontal);
        Assert.Equal(1900f, band.StartMm, precision: 1);
        Assert.Equal(3100f, band.EndMm, precision: 1);
    }

    [Fact]
    public void TryAddHorizontalBand_DoisCliquesDefineAltura()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallBandService.TryAddHorizontalBand(wall, 1100f, 2100f, out var band, out _));
        Assert.NotNull(band);
        Assert.True(band!.IsHorizontal);
        Assert.Equal(1100f, band.StartMm, precision: 1);
        Assert.Equal(2100f, band.EndMm, precision: 1);
    }

    [Fact]
    public void TryAddVerticalBand_DoisCliquesDefineLargura()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallBandService.TryAddVerticalBand(wall, 1500f, 3200f, out var band, out _));
        Assert.NotNull(band);
        Assert.False(band!.IsHorizontal);
        Assert.Equal(1500f, band.StartMm, precision: 1);
        Assert.Equal(3200f, band.EndMm, precision: 1);
    }

    [Fact]
    public void TryRemoveBand_RemoveFaixaExistente()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallBandService.TryAddHorizontalBand(wall, 1100f, 2100f, out var band, out _));
        Assert.True(WallBandService.TryRemoveBand(wall, band!.Id, out _));
        Assert.Empty(wall.Bands);
    }

    [Fact]
    public void FormatSummaryLine_IncluiMaterialQuandoDefinido()
    {
        var band = new WallBand
        {
            IsHorizontal = true,
            StartMm = 2100f,
            EndMm = 2600f,
            MaterialId = "mdf-madeirado"
        };

        string line = WallBandService.FormatSummaryLine(band);
        Assert.Contains("Horizontal", line);
        Assert.Contains("MDF Madeirado", line);
    }

    [Fact]
    public void TryAddCircleRegion_CriaRegiaoCircular()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddCircleRegion(
            wall,
            FaceType.Internal,
            2500f,
            1300f,
            600f,
            out var region,
            out _));

        Assert.NotNull(region);
        Assert.Equal(WallRegionShape.Circular, region!.Shape);
        Assert.Equal(2500f, region.CenterAlongMm, precision: 1);
        Assert.Equal(1300f, region.CenterHeightMm, precision: 1);
        Assert.Equal(600f, region.RadiusMm, precision: 1);
    }

    [Fact]
    public void TrySetRegionRadius_AjustaCirculo()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddCircleRegion(wall, FaceType.Internal, 2500f, 1300f, 600f, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TrySetRegionRadius(wall, region!.Id, 800f, out _));
        Assert.Equal(800f, region.RadiusMm, precision: 1);
    }

    [Fact]
    public void TrySetRegionOffset_ExpandeBorda()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TrySetRegionOffset(wall, region!.Id, 50f, out _));
        Assert.Equal(50f, region.OffsetMm, precision: 1);

        var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, wall.Length, 2600f);
        Assert.True(end - start > region.EndAlongMm - region.StartAlongMm);
        Assert.True(top - bottom > region.TopMm - region.BottomMm);
    }

    [Fact]
    public void TrySetRegionEdgeOffset_ExpandeArestaIndividual()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TrySetRegionEdgeOffset(
            wall, region!.Id, WallRegionEdgeKind.StartAlong, 40f, out _));
        Assert.Equal(40f, region.OffsetEdgeStartAlongMm, precision: 1);

        var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, wall.Length, 2600f);
        Assert.Equal(region.StartAlongMm - 40f, start, precision: 1);
        Assert.Equal(region.EndAlongMm, end, precision: 1);
        Assert.Equal(region.BottomMm, bottom, precision: 1);
        Assert.Equal(region.TopMm, top, precision: 1);
    }

    [Fact]
    public void TryAdjustRegionEdgeOffset_IncrementaOffsetAresta()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TryAdjustRegionEdgeOffset(
            wall, region!.Id, WallRegionEdgeKind.Top, 25f, out _));
        Assert.Equal(25f, region.OffsetEdgeTopMm, precision: 1);

        Assert.True(WallRegionService.TryAdjustRegionEdgeOffset(
            wall, region.Id, WallRegionEdgeKind.Top, 10f, out _));
        Assert.Equal(35f, region.OffsetEdgeTopMm, precision: 1);
    }

    [Fact]
    public void OffsetPorAresta_PersisteNoTracos()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        region!.OffsetEdgeStartAlongMm = 30f;
        region.OffsetEdgeEndAlongMm = 20f;
        region.OffsetEdgeBottomMm = 15f;
        region.OffsetEdgeTopMm = 25f;

        var project = new Project();
        project.Room.SetWalls([wall]);

        var path = Path.Combine(Path.GetTempPath(), $"offset-aresta-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path)).Room.Walls.Single();
            var loadedRegion = loaded.Regions.Single();

            Assert.Equal(30f, loadedRegion.OffsetEdgeStartAlongMm, precision: 1);
            Assert.Equal(20f, loadedRegion.OffsetEdgeEndAlongMm, precision: 1);
            Assert.Equal(15f, loadedRegion.OffsetEdgeBottomMm, precision: 1);
            Assert.Equal(25f, loadedRegion.OffsetEdgeTopMm, precision: 1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void RegiaoCircular_PersisteNoTracos()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddCircleRegion(wall, FaceType.External, 2000f, 1500f, 500f, out var region, out _);
        Assert.NotNull(region);
        region!.MaterialId = "ceramica-bege";
        region.OffsetMm = 30f;

        var project = new Project();
        project.Room.SetWalls([wall]);

        var path = Path.Combine(Path.GetTempPath(), $"circulo-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path)).Room.Walls.Single();
            var loadedRegion = loaded.Regions.Single();

            Assert.Equal(WallRegionShape.Circular, loadedRegion.Shape);
            Assert.Equal(2000f, loadedRegion.CenterAlongMm, precision: 1);
            Assert.Equal(1500f, loadedRegion.CenterHeightMm, precision: 1);
            Assert.Equal(500f, loadedRegion.RadiusMm, precision: 1);
            Assert.Equal(30f, loadedRegion.OffsetMm, precision: 1);
            Assert.Equal("ceramica-bege", loadedRegion.MaterialId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryAddPolygonRegion_CriaRegiaoEmL()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1000f, 2500f, 2500f, 1800f, 1800f, 1000f];
        float[] height = [1100f, 1100f, 1500f, 1500f, 2100f, 2100f];

        Assert.True(WallRegionService.TryAddPolygonRegion(
            wall,
            FaceType.Internal,
            along,
            height,
            out var region,
            out _));

        Assert.NotNull(region);
        Assert.Equal(WallRegionShape.Polygon, region!.Shape);
        Assert.Equal(6, region.PolygonAlongMm.Count);
        Assert.True(WallRegionGeometry.ContainsPoint(region, 1200f, 1200f));
        Assert.False(WallRegionGeometry.ContainsPoint(region, 2200f, 1700f));
    }

    [Fact]
    public void TryMoveRegion_Retangular_TransladaDentroDaParede()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1000f,
            2000f,
            1100f,
            2100f,
            out var region,
            out _));

        Assert.True(WallRegionService.TryMoveRegion(wall, region!.Id, 200f, 100f, out _));

        Assert.Equal(1200f, region.StartAlongMm, precision: 1);
        Assert.Equal(2200f, region.EndAlongMm, precision: 1);
        Assert.Equal(1200f, region.BottomMm, precision: 1);
        Assert.Equal(2200f, region.TopMm, precision: 1);
    }

    [Fact]
    public void TryMoveRegion_Polygon_TransladaVertices()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1200f, 2200f, 2200f, 1200f];
        float[] height = [1100f, 1100f, 1700f, 1700f];

        Assert.True(WallRegionService.TryAddPolygonRegion(wall, FaceType.Internal, along, height, out var region, out _));

        Assert.True(WallRegionService.TryMoveRegion(wall, region!.Id, 100f, 50f, out _));

        Assert.Equal(1300f, region.PolygonAlongMm[0], precision: 1);
        Assert.Equal(1150f, region.PolygonHeightMm[0], precision: 1);
        Assert.Equal(4, region.PolygonAlongMm.Count);
    }

    [Fact]
    public void TryMoveRegion_RejeitaSobreposicao()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1000f,
            2000f,
            1100f,
            2100f,
            out var first,
            out _));
        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            2500f,
            3500f,
            1100f,
            2100f,
            out _,
            out _));

        Assert.False(WallRegionService.TryMoveRegion(wall, first!.Id, 2000f, 0f, out string? error));

        Assert.Contains("sobrep", error!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1000f, first.StartAlongMm, precision: 1);
    }

    [Fact]
    public void TryInsertPolygonVertexAtPoint_AdicionaVerticeNaAresta()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1200f, 2200f, 2200f, 1200f];
        float[] height = [1100f, 1100f, 1700f, 1700f];

        Assert.True(WallRegionService.TryAddPolygonRegion(wall, FaceType.Internal, along, height, out var region, out _));
        Guid regionId = region!.Id;

        Assert.True(WallRegionService.TryInsertPolygonVertexAtPoint(
            wall,
            regionId,
            2200f,
            1400f,
            out _));

        Assert.Equal(5, region.PolygonAlongMm.Count);
        Assert.Contains(1400f, region.PolygonHeightMm);
    }

    [Fact]
    public void TryInsertPolygonVertexAtPoint_RejeitaLongeDaAresta()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1200f, 2200f, 2200f, 1200f];
        float[] height = [1100f, 1100f, 1700f, 1700f];

        Assert.True(WallRegionService.TryAddPolygonRegion(wall, FaceType.Internal, along, height, out var region, out _));

        Assert.False(WallRegionService.TryInsertPolygonVertexAtPoint(
            wall,
            region!.Id,
            1800f,
            1500f,
            out string? error));

        Assert.Contains("aresta", error!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, region.PolygonAlongMm.Count);
    }

    [Fact]
    public void TryAddPolygonRegion_PersisteVerticesNoTracos()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1200f, 2200f, 2200f, 1200f];
        float[] height = [1100f, 1100f, 1700f, 1700f];

        Assert.True(WallRegionService.TryAddPolygonRegion(wall, FaceType.Internal, along, height, out var region, out _));
        region!.MaterialId = "ceramica-bege";
        region.Name = "Azulejo L";

        var project = new Project();
        project.Room.SetWalls([wall]);
        var path = Path.Combine(Path.GetTempPath(), $"poligono-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path)).Room.Walls.Single();
            var loadedRegion = loaded.Regions.Single();

            Assert.Equal(WallRegionShape.Polygon, loadedRegion.Shape);
            Assert.Equal(4, loadedRegion.PolygonAlongMm.Count);
            Assert.Equal(1200f, loadedRegion.PolygonAlongMm[0], precision: 1);
            Assert.Equal(1100f, loadedRegion.PolygonHeightMm[0], precision: 1);
            Assert.Equal("Azulejo L", loadedRegion.Name);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryAddRectRegion_PorCantos_DefineRetangulo()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1000f,
            2000f,
            500f,
            1500f,
            out var region,
            out _));

        Assert.NotNull(region);
        Assert.Equal(1000f, region!.StartAlongMm, precision: 1);
        Assert.Equal(2000f, region.EndAlongMm, precision: 1);
        Assert.Equal(500f, region.BottomMm, precision: 1);
        Assert.Equal(1500f, region.TopMm, precision: 1);
    }

    [Fact]
    public void TrySetRegionEdge_AjustaTopo()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TrySetRegionEdge(
            wall, region!.Id, WallRegionEdgeKind.Top, 2000f, out _));
        Assert.Equal(2000f, region.TopMm, precision: 1);
    }

    [Fact]
    public void TrySetRegionEdge_AjustaLateral()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);
        Assert.NotNull(region);

        Assert.True(WallRegionService.TrySetRegionEdge(
            wall, region!.Id, WallRegionEdgeKind.EndAlong, 2200f, out _));
        Assert.Equal(2200f, region.EndAlongMm, precision: 1);
    }

    [Fact]
    public void TrySetBandEdge_Horizontal_AjustaBase()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        Assert.NotNull(band);

        Assert.True(WallBandService.TrySetBandEdge(wall, band!.Id, WallBandEdgeKind.Start, 2000f, out _));
        Assert.Equal(2000f, band.StartMm, precision: 1);
        Assert.Equal(2600f, band.EndMm, precision: 1);
    }

    [Fact]
    public void TrySetBandEdge_Vertical_AjustaLateral()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        WallBandService.TryAddVerticalBandAtCenter(wall, 2500f, out var band, out _);
        Assert.NotNull(band);

        Assert.True(WallBandService.TrySetBandEdge(wall, band!.Id, WallBandEdgeKind.End, 3200f, out _));
        Assert.Equal(3200f, band.EndMm, precision: 1);
    }

    [Fact]
    public void CamadasFaixasRegioes_ArrastePersisteNoTracos()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f,
            LayerId = "parede"
        };

        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        WallRegionService.TryAddDefaultTileRegion(wall, out var region, out _);

        Assert.NotNull(band);
        Assert.NotNull(region);

        Assert.True(WallBandService.TrySetBandEdge(wall, band!.Id, WallBandEdgeKind.Start, 2050f, out _));
        Assert.True(WallRegionService.TrySetRegionEdge(
            wall, region!.Id, WallRegionEdgeKind.Top, 2050f, out _));

        var project = new Project();
        project.Room.SetWalls([wall]);

        var path = Path.Combine(Path.GetTempPath(), $"arraste-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var loaded = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path)).Room.Walls.Single();

            Assert.Equal(2050f, loaded.Bands[0].StartMm, precision: 1);
            Assert.Equal(2050f, loaded.Regions[0].TopMm, precision: 1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CamadasFaixasRegioes_PersisteERestaura()
    {
        var project = new Project();
        project.Metadata.WallLayerVisibility = new Dictionary<string, bool> { ["divisoria"] = true };

        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f,
            LayerId = "divisoria"
        };

        WallBandService.TryAddDefaultUpperBand(wall, out _, out _);
        WallRegionService.TryAddDefaultTileRegion(wall, out _, out _);
        project.Room.SetWalls([wall]);

        var path = Path.Combine(Path.GetTempPath(), $"camadas-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            var loadedWall = restored.Room.Walls.Single();
            Assert.Equal("divisoria", loadedWall.LayerId);
            Assert.Single(loadedWall.Bands);
            Assert.Single(loadedWall.Regions);
            Assert.True(restored.Metadata.WallLayerVisibility!["divisoria"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void SetLayerVisible_OcultaCamadaNoMetadata()
    {
        var metadata = new ProjectMetadata();
        WallLayerCatalog.SetLayerVisible(metadata, "divisoria", false);

        Assert.False(WallLayerCatalog.IsLayerVisible(metadata, "divisoria"));
        Assert.True(WallLayerCatalog.IsLayerVisible(metadata, "parede"));
    }

    [Fact]
    public void CountWallsOnLayer_ContaPorCamada()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
            {
                LayerId = "parede"
            },
            new WallSegment(new OpenTK.Mathematics.Vector2(5000, 0), new OpenTK.Mathematics.Vector2(5000, 5000))
            {
                LayerId = "divisoria"
            }
        ]);

        Assert.Equal(1, WallLayerCatalog.CountWallsOnLayer(project.Room.Walls, "parede"));
        Assert.Equal(1, WallLayerCatalog.CountWallsOnLayer(project.Room.Walls, "divisoria"));
        Assert.Equal(0, WallLayerCatalog.CountWallsOnLayer(project.Room.Walls, "referencia"));
    }

    [Fact]
    public void ExportFixture_CamadasFaixasRegioes_ParaTesteVisual()
    {
        var project = new Project();
        project.Metadata.Name = "Quadrado camadas faixas";

        var wall = new WallSegment(
            new OpenTK.Mathematics.Vector2(-150, -150),
            new OpenTK.Mathematics.Vector2(5150, -150),
            150,
            2600,
            WallOrientation.Right)
        {
            MeasureSide = WallMeasureSide.Interior,
            LayerId = "parede"
        };

        WallBandService.TryAddDefaultUpperBand(wall, out var band, out _);
        if (band != null)
            band.MaterialId = "mdf-madeirado";

        WallBandService.TryAddVerticalBandAtCenter(wall, 2500f, out var verticalBand, out _);
        if (verticalBand != null)
            verticalBand.MaterialId = "mdf-branco";

        WallRegionService.TryAddDefaultTileRegion(wall, out _, out _);
        WallRegionService.TryAddCircleRegion(
            wall,
            FaceType.External,
            3800f,
            1600f,
            450f,
            out var circleRegion,
            out _);
        if (circleRegion != null)
        {
            circleRegion.MaterialId = "mdf-branco";
            circleRegion.Name = "Círculo";
        }

        WallRegionService.TryAddCircleRegion(
            wall,
            FaceType.Internal,
            1200f,
            1600f,
            350f,
            out var innerCircle,
            out _);
        if (innerCircle != null)
        {
            innerCircle.MaterialId = "mdf-branco";
            innerCircle.Name = "Círculo interno";
        }

        float[] polyAlong = [1000f, 2500f, 2500f, 1800f, 1800f, 1000f];
        float[] polyHeight = [1100f, 1100f, 1500f, 1500f, 2100f, 2100f];
        WallRegionService.TryAddPolygonRegion(
            wall,
            FaceType.Internal,
            polyAlong,
            polyHeight,
            out var polygonRegion,
            out _);
        if (polygonRegion != null)
        {
            polygonRegion.MaterialId = "ceramica-bege";
            polygonRegion.Name = "Azulejo L";
        }

        project.Room.SetWalls([
            wall,
            new WallSegment(
                new OpenTK.Mathematics.Vector2(5150, -150),
                new OpenTK.Mathematics.Vector2(5150, 5150),
                150,
                2600,
                WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior,
                LayerId = "divisoria"
            },
            new WallSegment(
                new OpenTK.Mathematics.Vector2(5150, 5150),
                new OpenTK.Mathematics.Vector2(-150, 5150),
                150,
                2600,
                WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            },
            new WallSegment(
                new OpenTK.Mathematics.Vector2(-150, 5150),
                new OpenTK.Mathematics.Vector2(-150, -150),
                150,
                2600,
                WallOrientation.Right)
            {
                MeasureSide = WallMeasureSide.Interior
            }
        ]);

        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "samples", "quadrado-5000-camadas-faixas.tracos"));

        ProjectPersistence.SaveToFile(ProjectPersistence.CreateFromProject(project), path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void TryRotateRegion_Retangular_Gira90Graus()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1500f,
            2500f,
            1100f,
            2100f,
            out var region,
            out _));

        Assert.True(WallRegionService.TryRotateRegionByDelta(wall, region!.Id, 90f, out _));
        Assert.Equal(90f, region.RotationDegrees, precision: 1);
    }

    [Fact]
    public void TryRotateRegion_Polygon_RotacionaVertices()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        float[] along = [1200f, 2200f, 2200f, 1200f];
        float[] height = [1100f, 1100f, 1700f, 1700f];

        Assert.True(WallRegionService.TryAddPolygonRegion(wall, FaceType.Internal, along, height, out var region, out _));

        float beforeAlong = region!.PolygonAlongMm[0];
        float beforeHeight = region.PolygonHeightMm[0];

        Assert.True(WallRegionService.TryRotateRegionByDelta(wall, region.Id, 90f, out _));

        Assert.NotEqual(beforeAlong, region.PolygonAlongMm[0]);
        Assert.NotEqual(beforeHeight, region.PolygonHeightMm[0]);
        Assert.Equal(0f, region.RotationDegrees, precision: 1);
    }

    [Fact]
    public void TryRotateRegion_RejeitaCircular()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddCircleRegion(
            wall,
            FaceType.Internal,
            2000f,
            1600f,
            400f,
            out var region,
            out _));

        Assert.False(WallRegionService.TryRotateRegionByDelta(wall, region!.Id, 90f, out string? error));

        Assert.Contains("circular", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryRotateRegion_RejeitaSobreposicao()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1900f,
            2100f,
            1200f,
            1800f,
            out var first,
            out _));
        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            2200f,
            2600f,
            1300f,
            1700f,
            out _,
            out _));

        Assert.False(WallRegionService.TryRotateRegionByDelta(wall, first!.Id, 45f, out string? error));

        Assert.Contains("sobrep", error!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0f, first.RotationDegrees, precision: 1);
    }

    [Fact]
    public void TryVerticalCutRegion_Retangular_DivideEmDuas()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1500f,
            3500f,
            1100f,
            2100f,
            out var region,
            out _));

        Assert.True(WallRegionService.TryVerticalCutRegion(wall, region!.Id, 2500f, out Guid leftId, out Guid rightId, out _));

        Assert.Equal(2, wall.Regions.Count);
        var left = wall.Regions.First(r => r.Id == leftId);
        var right = wall.Regions.First(r => r.Id == rightId);
        Assert.Equal(WallRegionShape.Rectangular, left.Shape);
        Assert.Equal(WallRegionShape.Rectangular, right.Shape);
        Assert.Equal(1500f, left.StartAlongMm, precision: 1);
        Assert.Equal(2500f, left.EndAlongMm, precision: 1);
        Assert.Equal(2500f, right.StartAlongMm, precision: 1);
        Assert.Equal(3500f, right.EndAlongMm, precision: 1);
        Assert.Equal("Face interna esq", left.Name);
        Assert.Equal("Face interna dir", right.Name);
    }

    [Fact]
    public void TryVerticalCutRegion_RejeitaCircular()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddCircleRegion(
            wall,
            FaceType.Internal,
            2000f,
            1600f,
            400f,
            out var region,
            out _));

        Assert.False(WallRegionService.TryVerticalCutRegion(wall, region!.Id, 2000f, out _, out _, out string? error));

        Assert.Contains("circular", error!, StringComparison.OrdinalIgnoreCase);
        Assert.Single(wall.Regions);
    }

    [Fact]
    public void TryVerticalCutRegion_RejeitaCortePertoDaBorda()
    {
        var wall = new WallSegment(new OpenTK.Mathematics.Vector2(0, 0), new OpenTK.Mathematics.Vector2(5000, 0))
        {
            Height = 2600f
        };

        Assert.True(WallRegionService.TryAddRectRegion(
            wall,
            FaceType.Internal,
            1500f,
            2500f,
            1100f,
            2100f,
            out var region,
            out _));

        Assert.False(WallRegionService.TryVerticalCutRegion(wall, region!.Id, 1550f, out _, out _, out string? error));

        Assert.Contains("50", error!);
        Assert.Single(wall.Regions);
    }
}
