using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ProjectPersistence
{
    public const string FileExtension = ".tracos";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static ProjectDocument CreateFromRoom(Room room, ProjectMetadata? metadata = null)
    {
        var project = new Project { Metadata = metadata ?? new ProjectMetadata() };
        project.Room.SetWalls(room.Walls);
        return CreateFromProject(project, metadata);
    }

    public static ProjectDocument CreateFromProject(Project project, ProjectMetadata? metadata = null)
    {
        var document = new ProjectDocument
        {
            Metadata = metadata ?? project.Metadata
        };

        document.Metadata.ModifiedUtc = DateTime.UtcNow;

        foreach (var compartment in project.Room.Compartments)
        {
            document.Compartments.Add(new RoomCompartmentData
            {
                Id = compartment.Id,
                DisplayName = compartment.DisplayName
            });
        }

        foreach (var wall in project.Room.Walls)
        {
            var wallData = new WallSegmentData
            {
                Id = wall.Id,
                StartX = wall.Start.X,
                StartZ = wall.Start.Y,
                EndX = wall.End.X,
                EndZ = wall.End.Y,
                Height = wall.HeightStart,
                HeightStart = wall.HeightStart,
                HeightEnd = wall.HeightEnd,
                Thickness = wall.Thickness,
                Orientation = wall.Orientation,
                MeasureSide = wall.MeasureSide,
                FloorOffset = wall.FloorOffset,
                CotaAnterior = wall.CotaAnterior,
                CotaPosterior = wall.CotaPosterior,
                CotaInferior = wall.CotaInferior,
                CotaSuperior = wall.CotaSuperior,
                DrawBottomFace = wall.DrawBottomFace,
                IsMovable = wall.IsMovable,
                IsVisible = wall.IsVisible,
                ChamferStartMm = wall.ChamferStartMm,
                ChamferEndMm = wall.ChamferEndMm,
                FlechaMm = wall.FlechaMm,
                ConstructionType = wall.ConstructionType,
                LayerId = wall.LayerId,
                InternalFaceMaterialId = wall.InternalFaceMaterialId,
                ExternalFaceMaterialId = wall.ExternalFaceMaterialId,
                CompartmentId = wall.CompartmentId
            };

            foreach (var band in wall.Bands)
            {
                wallData.Bands.Add(new WallBandData
                {
                    Id = band.Id,
                    IsHorizontal = band.IsHorizontal,
                    StartMm = band.StartMm,
                    EndMm = band.EndMm,
                    MaterialId = band.MaterialId
                });
            }

            foreach (var region in wall.Regions)
            {
                wallData.Regions.Add(new WallRegionData
                {
                    Id = region.Id,
                    Name = region.Name,
                    Shape = region.Shape,
                    Face = region.Face,
                    StartAlongMm = region.StartAlongMm,
                    EndAlongMm = region.EndAlongMm,
                    BottomMm = region.BottomMm,
                    TopMm = region.TopMm,
                    CenterAlongMm = region.CenterAlongMm,
                    CenterHeightMm = region.CenterHeightMm,
                    RadiusMm = region.RadiusMm,
                    OffsetMm = region.OffsetMm,
                    OffsetEdgeStartAlongMm = region.OffsetEdgeStartAlongMm,
                    OffsetEdgeEndAlongMm = region.OffsetEdgeEndAlongMm,
                    OffsetEdgeBottomMm = region.OffsetEdgeBottomMm,
                    OffsetEdgeTopMm = region.OffsetEdgeTopMm,
                    MaterialId = region.MaterialId,
                    RotationDegrees = region.RotationDegrees,
                    PolygonAlongMm = region.PolygonAlongMm.ToList(),
                    PolygonHeightMm = region.PolygonHeightMm.ToList()
                });
            }

            foreach (var opening in wall.Openings)
            {
                wallData.Openings.Add(new WallOpeningData
                {
                    Id = opening.Id,
                    Type = opening.Type,
                    DistanceFromStart = opening.DistanceFromStart,
                    Width = opening.Width,
                    Height = opening.Height,
                    SillHeight = opening.SillHeight
                });
            }

            document.Walls.Add(wallData);
        }

        foreach (var module in project.Modules)
        {
            document.Modules.Add(new ModuleInstanceData
            {
                Id = module.Id,
                DefinitionId = module.DefinitionId,
                Width = module.Width,
                Height = module.Height,
                Depth = module.Depth,
                PositionX = module.Position.X,
                PositionY = module.Position.Y,
                PositionZ = module.Position.Z,
                RotationYDegrees = module.RotationYDegrees,
                MaterialId = module.MaterialId,
                AttachedWallId = module.AttachedWallId,
                DistanceAlongWall = module.DistanceAlongWall,
                LayerId = module.LayerId,
                InstanceDisplayName = module.InstanceDisplayName,
                IsVisible = module.IsVisible,
                IsLocked = module.IsLocked,
                CornerMedidaA = module.CornerL?.ProfundidadeDireita,
                CornerMedidaB = module.CornerL?.ProfundidadeEsquerda,
                CornerLarguraA = module.CornerL?.ComprimentoDireito,
                CornerLarguraB = module.CornerL?.ComprimentoEsquerdo,
                BlindCornerUseSpacer = module.BlindCorner?.UseSpacer,
                PartOverrides = module.PartOverrides.Count == 0
                    ? null
                    : module.PartOverrides.ToDictionary(
                        kv => kv.Key,
                        kv => new PartDimensionOverrideData
                        {
                            Width = kv.Value.Width,
                            Height = kv.Value.Height,
                            Depth = kv.Value.Depth,
                            MinXOffset = kv.Value.MinXOffset,
                            MaxXOffset = kv.Value.MaxXOffset,
                            MinYOffset = kv.Value.MinYOffset,
                            MaxYOffset = kv.Value.MaxYOffset,
                            MinZOffset = kv.Value.MinZOffset,
                            MaxZOffset = kv.Value.MaxZOffset
                        })
            });
        }

        if (project.Room.Floor != null)
        {
            var floor = project.Room.Floor;
            var floorData = new FloorSurfaceData
            {
                DefaultMaterialId = floor.DefaultMaterialId,
                ShowGrid = project.Room.ShowFloorGrid
            };

            foreach (var zone in floor.Zones)
            {
                floorData.Zones.Add(new FloorZoneData
                {
                    Id = zone.Id,
                    MaterialId = zone.MaterialId,
                    Name = zone.Name,
                    Shape = zone.Shape,
                    MinX = zone.MinX,
                    MinZ = zone.MinY,
                    MaxX = zone.MaxX,
                    MaxZ = zone.MaxY,
                    CenterX = zone.CenterX,
                    CenterZ = zone.CenterY,
                    RadiusMm = zone.RadiusMm,
                    OffsetMm = zone.OffsetMm,
                    OffsetEdgeStartAlongMm = zone.OffsetEdgeStartAlongMm,
                    OffsetEdgeEndAlongMm = zone.OffsetEdgeEndAlongMm,
                    OffsetEdgeBottomMm = zone.OffsetEdgeBottomMm,
                    OffsetEdgeTopMm = zone.OffsetEdgeTopMm,
                    PolygonXMm = zone.PolygonAlongMm.ToList(),
                    PolygonZMm = zone.PolygonHeightMm.ToList()
                });
            }

            document.Floor = floorData;
        }

        foreach (var dim in project.ManualWallDimensions)
        {
            document.ManualDimensions.Add(new WallManualDimensionData
            {
                Id = dim.Id,
                Kind = dim.Kind,
                PointAX = dim.PointA.X,
                PointAY = dim.PointA.Y,
                PointBX = dim.PointB.X,
                PointBY = dim.PointB.Y,
                PointCX = dim.PointC.X,
                PointCY = dim.PointC.Y,
                DimStartX = dim.DimStart.X,
                DimStartY = dim.DimStart.Y,
                DimEndX = dim.DimEnd.X,
                DimEndY = dim.DimEnd.Y,
                ArcRadius = dim.ArcRadius,
                DisplayValue = dim.DisplayValue
            });
        }

        return document;
    }

    public static Room LoadRoom(ProjectDocument document) => LoadProject(document).Room;

    public static Project LoadProject(ProjectDocument document)
    {
        if (document.SchemaVersion > ProjectDocument.CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Arquivo .tracos versão {document.SchemaVersion} não é suportado. " +
                $"Atualize o Traços 3D Studio.");

        var walls = new List<WallSegment>();

        foreach (var wallData in document.Walls)
        {
            float heightStart = wallData.HeightStart ?? wallData.Height;
            float heightEnd = wallData.HeightEnd ?? heightStart;

            var wall = new WallSegment(
                new Vector2(wallData.StartX, wallData.StartZ),
                new Vector2(wallData.EndX, wallData.EndZ),
                wallData.Thickness,
                heightStart,
                wallData.Orientation)
            {
                Id = wallData.Id == Guid.Empty ? Guid.NewGuid() : wallData.Id,
                HeightEnd = heightEnd,
                FloorOffset = wallData.FloorOffset,
                MeasureSide = wallData.MeasureSide,
                CotaAnterior = wallData.CotaAnterior,
                CotaPosterior = wallData.CotaPosterior,
                CotaInferior = wallData.CotaInferior,
                CotaSuperior = wallData.CotaSuperior,
                DrawBottomFace = wallData.DrawBottomFace,
                IsMovable = wallData.IsMovable,
                IsVisible = wallData.IsVisible,
                ChamferStartMm = wallData.ChamferStartMm,
                ChamferEndMm = wallData.ChamferEndMm,
                FlechaMm = wallData.FlechaMm,
                ConstructionType = wallData.ConstructionType,
                LayerId = WallLayerCatalog.NormalizeLayerId(wallData.LayerId),
                InternalFaceMaterialId = wallData.InternalFaceMaterialId,
                ExternalFaceMaterialId = wallData.ExternalFaceMaterialId,
                CompartmentId = wallData.CompartmentId
            };

            foreach (var bandData in wallData.Bands)
            {
                wall.Bands.Add(new WallBand
                {
                    Id = bandData.Id == Guid.Empty ? Guid.NewGuid() : bandData.Id,
                    IsHorizontal = bandData.IsHorizontal,
                    StartMm = bandData.StartMm,
                    EndMm = bandData.EndMm,
                    MaterialId = bandData.MaterialId
                });
            }

            foreach (var regionData in wallData.Regions)
            {
                var region = new WallRegion
                {
                    Id = regionData.Id == Guid.Empty ? Guid.NewGuid() : regionData.Id,
                    Name = regionData.Name,
                    Shape = regionData.Shape,
                    Face = regionData.Face,
                    StartAlongMm = regionData.StartAlongMm,
                    EndAlongMm = regionData.EndAlongMm,
                    BottomMm = regionData.BottomMm,
                    TopMm = regionData.TopMm,
                    CenterAlongMm = regionData.CenterAlongMm,
                    CenterHeightMm = regionData.CenterHeightMm,
                    RadiusMm = regionData.RadiusMm,
                    OffsetMm = regionData.OffsetMm,
                    OffsetEdgeStartAlongMm = regionData.OffsetEdgeStartAlongMm,
                    OffsetEdgeEndAlongMm = regionData.OffsetEdgeEndAlongMm,
                    OffsetEdgeBottomMm = regionData.OffsetEdgeBottomMm,
                    OffsetEdgeTopMm = regionData.OffsetEdgeTopMm,
                    MaterialId = regionData.MaterialId,
                    RotationDegrees = regionData.RotationDegrees
                };

                if (regionData.PolygonAlongMm.Count > 0 &&
                    regionData.PolygonHeightMm.Count == regionData.PolygonAlongMm.Count)
                {
                    region.PolygonAlongMm.AddRange(regionData.PolygonAlongMm);
                    region.PolygonHeightMm.AddRange(regionData.PolygonHeightMm);
                }

                wall.Regions.Add(region);
            }

            foreach (var openingData in wallData.Openings)
            {
                wall.Openings.Add(new WallOpening
                {
                    Id = openingData.Id == Guid.Empty ? Guid.NewGuid() : openingData.Id,
                    Type = openingData.Type,
                    DistanceFromStart = openingData.DistanceFromStart,
                    Width = openingData.Width,
                    Height = openingData.Height,
                    SillHeight = openingData.SillHeight,
                    AutoCutWall = true
                });
            }

            walls.Add(wall);
        }

        var project = new Project { Metadata = document.Metadata };
        project.Room.ShowAutomaticCeiling = document.Metadata.ShowAutomaticCeiling;

        var partitions = walls.Where(w => w.IsMovable).ToList();
        var envelope = walls.Where(w => !w.IsMovable).ToList();

        if (partitions.Count > 0)
        {
            project.Room.SetWalls(envelope);

            if (project.Room.IsClosed && project.Room.Floor == null)
                project.Room.RebuildAutomaticFloor();

            project.Room.AppendPartitionWalls(partitions);
        }
        else
        {
            project.Room.SetWalls(walls);
        }

        project.Room.ApplyFloorDocument(document.Floor);

        foreach (var compartmentData in document.Compartments)
        {
            project.Room.Compartments.Add(new RoomCompartment
            {
                Id = compartmentData.Id == Guid.Empty ? Guid.NewGuid() : compartmentData.Id,
                DisplayName = string.IsNullOrWhiteSpace(compartmentData.DisplayName)
                    ? RoomCompartmentService.DefaultDisplayName
                    : compartmentData.DisplayName.Trim()
            });
        }

        RoomCompartmentService.EnsureInitialized(project.Room, project.Metadata);

        foreach (var moduleData in document.Modules)
        {
            if (!ModuleCatalog.TryGet(moduleData.DefinitionId, out var definition) || definition == null)
                throw new InvalidDataException($"Módulo '{moduleData.DefinitionId}' não existe na biblioteca.");

            var instance = new ModuleInstance
            {
                Id = moduleData.Id == Guid.Empty ? Guid.NewGuid() : moduleData.Id,
                DefinitionId = moduleData.DefinitionId,
                Position = new Vector3(moduleData.PositionX, moduleData.PositionY, moduleData.PositionZ),
                RotationYDegrees = moduleData.RotationYDegrees,
                MaterialId = string.IsNullOrWhiteSpace(moduleData.MaterialId)
                    ? MaterialCatalog.DefaultMaterialId
                    : moduleData.MaterialId,
                AttachedWallId = moduleData.AttachedWallId,
                DistanceAlongWall = moduleData.DistanceAlongWall,
                LayerId = string.IsNullOrWhiteSpace(moduleData.LayerId)
                    ? WallLayerCatalog.DefaultModuleLayerId
                    : moduleData.LayerId,
                InstanceDisplayName = string.IsNullOrWhiteSpace(moduleData.InstanceDisplayName)
                    ? null
                    : moduleData.InstanceDisplayName.Trim(),
                IsVisible = moduleData.IsVisible,
                IsLocked = moduleData.IsLocked
            };

            if (moduleData.PartOverrides != null)
            {
                foreach (var kv in moduleData.PartOverrides)
                {
                    if (kv.Value == null)
                        continue;

                    instance.PartOverrides[kv.Key] = new PartDimensionOverride
                    {
                        Width = kv.Value.Width,
                        Height = kv.Value.Height,
                        Depth = kv.Value.Depth,
                        MinXOffset = kv.Value.MinXOffset,
                        MaxXOffset = kv.Value.MaxXOffset,
                        MinYOffset = kv.Value.MinYOffset,
                        MaxYOffset = kv.Value.MaxYOffset,
                        MinZOffset = kv.Value.MinZOffset,
                        MaxZOffset = kv.Value.MaxZOffset
                    };
                }
            }

            var dimSettings = DimensionConfiguratorService.GetSettings(project);
            bool isCornerL = definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight;
            // Envelope no arquivo; Medida A/B separadas (não usar Depth do envelope como Pe/Pd).
            instance.SetDimensions(
                moduleData.Width,
                moduleData.Height,
                moduleData.Depth,
                definition,
                dimSettings,
                respectCatalogLimits: false,
                syncCornerArmDepthFromDepth: !isCornerL);

            if (isCornerL && instance.CornerL != null)
            {
                if (moduleData.CornerLarguraA is > 0f && moduleData.CornerLarguraB is > 0f)
                {
                    instance.CornerL.ApplyEnvelopeLengths(
                        moduleData.CornerLarguraA.Value,
                        moduleData.CornerLarguraB.Value,
                        instance.Height);
                }

                float medidaA = moduleData.CornerMedidaA is > 0f
                    ? moduleData.CornerMedidaA.Value
                    : DimensionConfiguratorService.ResolveInsertionDimensions(definition, dimSettings).Depth;
                float medidaB = moduleData.CornerMedidaB is > 0f
                    ? moduleData.CornerMedidaB.Value
                    : medidaA;
                instance.CornerL.ApplyArmDepths(medidaA, medidaB);
                instance.RebuildMesh(definition, dimSettings);
            }

            if (definition.ShapeKind is ModuleShapeKind.BlindCornerLeft or ModuleShapeKind.BlindCornerRight)
            {
                instance.BlindCorner ??= BlindCornerParams.FromConfigurator(dimSettings);
                if (moduleData.BlindCornerUseSpacer.HasValue)
                    instance.BlindCorner.UseSpacer = moduleData.BlindCornerUseSpacer.Value;
                instance.RebuildMesh(definition, dimSettings);
            }

            project.Modules.Add(instance);
        }

        foreach (var dimData in document.ManualDimensions)
        {
            project.ManualWallDimensions.Add(new WallManualDimension
            {
                Id = dimData.Id == Guid.Empty ? Guid.NewGuid() : dimData.Id,
                Kind = dimData.Kind,
                PointA = new Vector2(dimData.PointAX, dimData.PointAY),
                PointB = new Vector2(dimData.PointBX, dimData.PointBY),
                PointC = new Vector2(dimData.PointCX, dimData.PointCY),
                DimStart = new Vector2(dimData.DimStartX, dimData.DimStartY),
                DimEnd = new Vector2(dimData.DimEndX, dimData.DimEndY),
                ArcRadius = dimData.ArcRadius,
                DisplayValue = dimData.DisplayValue
            });
        }

        DimensionConfiguratorService.EnsureProjectSettings(project);
        return project;
    }

    public static void SaveToFile(ProjectDocument document, string filePath)
    {
        document.Metadata.ModifiedUtc = DateTime.UtcNow;
        document.SchemaVersion = ProjectDocument.CurrentSchemaVersion;

        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static ProjectDocument LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var document = JsonSerializer.Deserialize<ProjectDocument>(json, JsonOptions);

        if (document == null)
            throw new InvalidDataException("Arquivo .tracos inválido ou vazio.");

        if (document.SchemaVersion < 1)
            throw new InvalidDataException("Versão do schema não reconhecida.");

        if (document.SchemaVersion > ProjectDocument.CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Arquivo .tracos versão {document.SchemaVersion} não é suportado. " +
                $"Atualize o Traços 3D Studio.");

        return document;
    }
}
