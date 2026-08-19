using OpenTK.Mathematics;

namespace Tracos3DStudio;

public class Room
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public List<WallSegment> Walls { get; } = new();

    public List<WallFace> Faces { get; } = new();

    public List<RoomCompartment> Compartments { get; } = new();

    public FloorSurface? Floor { get; set; }

    public FloorSurface? Ceiling { get; set; }

    public bool ShowAutomaticCeiling { get; set; } = false;

    /// <summary>Grade sobre o piso. Desligada por padrão para o ambiente iniciar limpo.</summary>
    public bool ShowFloorGrid { get; set; } = false;

    public bool IsClosed { get; private set; }

    public void Clear()
    {
        Walls.Clear();
        Faces.Clear();
        Compartments.Clear();
        Floor = null;
        Ceiling = null;
        IsClosed = false;
    }

    public void AddWall(WallSegment wall)
    {
        Walls.Add(wall);
        RecalculateClosedState();
    }

    /// <summary>Adiciona paredes internas sem invalidar envelope fechado (piso/teto) já existente.</summary>
    public void AppendPartitionWalls(IEnumerable<WallSegment> partitions)
    {
        bool envelopeClosed = IsClosed && Floor != null;

        foreach (var wall in partitions)
            Walls.Add(wall);

        if (envelopeClosed)
            return;

        RecalculateClosedState();
    }

    public bool TryReplaceWallWithSegments(Guid wallId, IReadOnlyList<WallSegment> replacements)
    {
        if (replacements.Count == 0)
            return false;

        int index = Walls.FindIndex(w => w.Id == wallId);

        if (index < 0)
            return false;

        Walls.RemoveAt(index);
        Walls.InsertRange(index, replacements);
        RecalculateClosedState();

        if (IsClosed)
            RebuildAutomaticFloor();

        return true;
    }

    public void SetWalls(IEnumerable<WallSegment> walls)
    {
        Walls.Clear();
        Walls.AddRange(walls);

        RecalculateClosedState();

        if (IsClosed)
        {
            RebuildAutomaticFloor();
            RebuildAutomaticCeiling();
        }
        else
        {
            Floor = null;
            Ceiling = null;
        }
    }

    public void RecalculateClosedState(float tolerance = 20f)
    {
        if (Walls.Count < 3)
        {
            IsClosed = false;
            return;
        }

        var first = Walls[0].Start;
        var last = Walls[^1].End;

        if (Geometry2D.AlmostEqual(first, last, tolerance))
        {
            IsClosed = true;
            return;
        }

        IsClosed = InnerFacePathIsClosed(tolerance);
    }

    private bool InnerFacePathIsClosed(float tolerance)
    {
        var visuals = WallVisualBuilder.BuildWithCorners(Walls);

        if (visuals.Count < 3)
            return false;

        for (int i = 0; i < visuals.Count; i++)
        {
            int next = (i + 1) % visuals.Count;
            var current = visuals[i];
            var nextSeg = visuals[next];

            bool currInnerA = WallInnerFaceService.UsesInnerFaceA(current, Walls);
            bool nextInnerA = WallInnerFaceService.UsesInnerFaceA(nextSeg, Walls);

            Vector2 currEnd = currInnerA ? current.A2 : current.B2;
            Vector2 nextStart = nextInnerA ? nextSeg.A1 : nextSeg.B1;

            if (!Geometry2D.AlmostEqual(currEnd, nextStart, tolerance))
                return false;
        }

        return true;
    }

    public void RebuildAutomaticFloor()
    {
        string previousMaterial = Floor?.DefaultMaterialId ?? FloorMaterialCatalog.DefaultMaterialId;
        var previousZones = Floor?.Zones.Select(CloneZone).ToList() ?? [];

        Floor = null;

        if (Walls.Count == 0)
            return;

        RecalculateClosedState();

        var cleanPoints = BuildAutomaticFloorPoints(Walls);

        if (cleanPoints.Count < 3)
            return;

        Floor = new FloorSurface(cleanPoints)
        {
            DefaultMaterialId = previousMaterial
        };

        foreach (var zone in previousZones)
        {
            if (FloorZoneService.IsZoneInsideFloor(zone, Floor.Points))
                Floor.Zones.Add(CloneZone(zone));
        }

        BuildFloorMesh(Floor);
        RebuildAutomaticCeiling();
    }

    /// <summary>
    /// Cria piso retangular a partir do bounding-box das paredes.
    /// Usado em ambientes abertos (3 paredes em U, paridade Promob).
    /// </summary>
    public void SeedFloorFromBounds()
    {
        if (Walls.Count < 2)
            return;

        string mat = Floor?.DefaultMaterialId ?? FloorMaterialCatalog.DefaultMaterialId;
        Floor = null;

        // Face interna das paredes (paridade Promob 5000×5000), não o eixo/exteriores.
        var pts = BuildAutomaticFloorPoints(Walls);
        if (pts.Count < 3)
            return;

        Floor = new FloorSurface(pts) { DefaultMaterialId = mat };
        BuildFloorMesh(Floor);
    }

    private static FloorZone CloneZone(FloorZone zone)
    {
        var clone = new FloorZone
        {
            Id = zone.Id,
            MaterialId = zone.MaterialId,
            Name = zone.Name,
            Shape = zone.Shape,
            MinX = zone.MinX,
            MinY = zone.MinY,
            MaxX = zone.MaxX,
            MaxY = zone.MaxY,
            CenterX = zone.CenterX,
            CenterY = zone.CenterY,
            RadiusMm = zone.RadiusMm,
            OffsetMm = zone.OffsetMm,
            OffsetEdgeStartAlongMm = zone.OffsetEdgeStartAlongMm,
            OffsetEdgeEndAlongMm = zone.OffsetEdgeEndAlongMm,
            OffsetEdgeBottomMm = zone.OffsetEdgeBottomMm,
            OffsetEdgeTopMm = zone.OffsetEdgeTopMm
        };

        clone.PolygonAlongMm.AddRange(zone.PolygonAlongMm);
        clone.PolygonHeightMm.AddRange(zone.PolygonHeightMm);
        return clone;
    }

    public void ApplyFloorDocument(FloorSurfaceData? data)
    {
        if (data == null || Floor == null)
            return;

        Floor.DefaultMaterialId = string.IsNullOrWhiteSpace(data.DefaultMaterialId)
            ? FloorMaterialCatalog.DefaultMaterialId
            : data.DefaultMaterialId;

        ShowFloorGrid = data.ShowGrid;

        Floor.Zones.Clear();

        foreach (var zoneData in data.Zones)
        {
            var zone = new FloorZone
            {
                Id = zoneData.Id == Guid.Empty ? Guid.NewGuid() : zoneData.Id,
                MaterialId = string.IsNullOrWhiteSpace(zoneData.MaterialId)
                    ? FloorMaterialCatalog.DefaultMaterialId
                    : zoneData.MaterialId,
                Name = string.IsNullOrWhiteSpace(zoneData.Name) ? "Região" : zoneData.Name,
                Shape = zoneData.Shape,
                MinX = zoneData.MinX,
                MinY = zoneData.MinZ,
                MaxX = zoneData.MaxX,
                MaxY = zoneData.MaxZ,
                CenterX = zoneData.CenterX,
                CenterY = zoneData.CenterZ,
                RadiusMm = zoneData.RadiusMm,
                OffsetMm = zoneData.OffsetMm,
                OffsetEdgeStartAlongMm = zoneData.OffsetEdgeStartAlongMm,
                OffsetEdgeEndAlongMm = zoneData.OffsetEdgeEndAlongMm,
                OffsetEdgeBottomMm = zoneData.OffsetEdgeBottomMm,
                OffsetEdgeTopMm = zoneData.OffsetEdgeTopMm
            };

            if (zoneData.PolygonXMm.Count > 0 &&
                zoneData.PolygonZMm.Count == zoneData.PolygonXMm.Count)
            {
                zone.PolygonAlongMm.AddRange(zoneData.PolygonXMm);
                zone.PolygonHeightMm.AddRange(zoneData.PolygonZMm);
            }

            if (FloorZoneService.IsZoneInsideFloor(zone, Floor.Points))
                Floor.Zones.Add(zone);
        }

        BuildFloorMesh(Floor);
    }

    /// <summary>Limites exatos do piso (sem margem) — usados pela grade.</summary>
    public bool TryGetFloorBounds(out Vector2 min, out Vector2 max)
    {
        min = Vector2.Zero;
        max = Vector2.Zero;

        if (Floor == null || Floor.Points.Count < 3)
            return false;

        min = new Vector2(Floor.Points.Min(p => p.X), Floor.Points.Min(p => p.Y));
        max = new Vector2(Floor.Points.Max(p => p.X), Floor.Points.Max(p => p.Y));
        return min.X < max.X && min.Y < max.Y;
    }

    private static List<Vector2> BuildAutomaticFloorPoints(IReadOnlyList<WallSegment> walls)
    {
        var visuals = WallVisualBuilder.BuildWithCorners(walls);

        if (visuals.Count == 0)
            return [];

        bool closed = walls.Count >= 3 &&
                      Geometry2D.AlmostEqual(walls[0].Start, walls[^1].End, 20f);

        if (closed && TryBuildInnerFacePolygon(visuals, walls, out List<Vector2> innerPolygon))
            return Geometry2D.RemoveDuplicates(innerPolygon, 2f);

        return BuildInnerBoundingRect(visuals, walls);
    }

    /// <summary>Vértices da face interna (tracejada) — mesmo critério de winding que cotas e medidas Promob.</summary>
    private static bool TryBuildInnerFacePolygon(
        IReadOnlyList<VisualWallSegment> visuals,
        IReadOnlyList<WallSegment> walls,
        out List<Vector2> points)
    {
        points = new List<Vector2>(visuals.Count);

        foreach (var visual in visuals)
        {
            bool useFaceA = WallInnerFaceService.UsesInnerFaceA(visual, walls);
            points.Add(useFaceA ? visual.A1 : visual.B1);
        }

        return points.Count >= 3;
    }

    private static List<Vector2> BuildInnerBoundingRect(
        IReadOnlyList<VisualWallSegment> visuals,
        IReadOnlyList<WallSegment> walls)
    {
        var inner = new List<Vector2>(visuals.Count * 2);

        foreach (var v in visuals)
        {
            bool useFaceA = WallInnerFaceService.UsesInnerFaceA(v, walls);
            inner.Add(useFaceA ? v.A1 : v.B1);
            inner.Add(useFaceA ? v.A2 : v.B2);
        }

        if (inner.Count == 0)
            return [];

        float minX = inner.Min(p => p.X);
        float minY = inner.Min(p => p.Y);
        float maxX = inner.Max(p => p.X);
        float maxY = inner.Max(p => p.Y);

        if (maxX - minX < 1f || maxY - minY < 1f)
            return [];

        return
        [
            new Vector2(minX, minY),
            new Vector2(maxX, minY),
            new Vector2(maxX, maxY),
            new Vector2(minX, maxY)
        ];
    }

    public void RebuildAutomaticCeiling()
    {
        Ceiling = null;

        if (!IsClosed || Walls.Count < 3 || !ShowAutomaticCeiling)
            return;

        var cleanPoints = BuildAutomaticFloorPoints(Walls);

        if (cleanPoints.Count < 3)
            return;

        float ceilingHeight = Walls.Count == 0 ? 2600f : Walls.Max(w => w.Height);

        Ceiling = new FloorSurface(cleanPoints) { Height = ceilingHeight };
        BuildCeilingMesh(Ceiling);
    }

    private void BuildCeilingMesh(FloorSurface ceiling)
    {
        ceiling.Mesh.Clear();

        if (!ceiling.Visible || ceiling.Points.Count < 3)
            return;

        var points = Geometry2D.IsClockwise(ceiling.Points)
            ? ceiling.Points.ToList()
            : ceiling.Points.AsEnumerable().Reverse().ToList();

        var origin = points[0];
        var ownerId = ceiling.Id;
        float y = ceiling.Height;

        for (var i = 1; i < points.Count - 1; i++)
        {
            var a = new Vector3(origin.X, y, origin.Y);
            var b = new Vector3(points[i].X, y, points[i].Y);
            var c = new Vector3(points[i + 1].X, y, points[i + 1].Y);

            ceiling.Mesh.AddTriangle(a, c, b, FaceKind.Ceiling, ownerId, "Teto automático");
        }
    }

    private void BuildFloorMesh(FloorSurface floor)
    {
        floor.Mesh.Clear();

        if (!floor.Visible)
            return;

        if (floor.Points.Count < 3)
            return;

        var points = Geometry2D.IsClockwise(floor.Points)
            ? floor.Points.AsEnumerable().Reverse().ToList()
            : floor.Points.ToList();

        var origin = points[0];
        var ownerId = floor.Id;
        var y = floor.Height;

        for (var i = 1; i < points.Count - 1; i++)
        {
            var a = new Vector3(origin.X, y, origin.Y);
            var b = new Vector3(points[i].X, y, points[i].Y);
            var c = new Vector3(points[i + 1].X, y, points[i + 1].Y);

            floor.Mesh.AddTriangle(a, b, c, FaceKind.Floor, ownerId, "Piso automático");
        }
    }
}
