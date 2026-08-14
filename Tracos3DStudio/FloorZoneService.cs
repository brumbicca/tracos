using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class FloorZoneService
{
    public const float MinSpanMm = 50f;
    public const float DefaultCircleRadiusMm = 600f;
    public const float MinCircleRadiusMm = 25f;
    public const int MinPolygonVertices = 3;
    public const float MinPolygonAreaMm2 = 2500f;

    public static bool IsZoneInsideFloor(FloorZone zone, IReadOnlyList<Vector2> floorPoints)
    {
        if (!zone.IsValid || floorPoints.Count < 3)
            return false;

        foreach (var point in FloorZoneGeometry.GetOutlinePoints(zone))
        {
            if (!Geometry2D.ContainsPoint(floorPoints, point))
                return false;
        }

        if (zone.Shape == WallRegionShape.Polygon)
        {
            foreach (var point in FloorZoneGeometry.GetBaseOutlinePoints(zone))
            {
                if (!Geometry2D.ContainsPoint(floorPoints, point))
                    return false;
            }
        }

        return true;
    }

    public static bool TryPickZone(
        IReadOnlyList<FloorZone> zones,
        Vector2 point,
        out FloorZone? zone)
    {
        zone = null;

        for (int i = zones.Count - 1; i >= 0; i--)
        {
            if (!zones[i].ContainsPoint(point))
                continue;

            zone = zones[i];
            return true;
        }

        return false;
    }

    public static FloorZone? TryCreateRectZone(
        Vector2 cornerA,
        Vector2 cornerB,
        IReadOnlyList<Vector2> floorPoints,
        string materialId,
        int zoneIndex)
    {
        var zone = FloorZone.FromCorners(cornerA, cornerB);

        if (!IsZoneInsideFloor(zone, floorPoints))
            return null;

        zone.MaterialId = materialId;
        zone.Name = $"Região {zoneIndex}";
        return zone;
    }

    public static bool TryAddRectZone(
        FloorSurface floor,
        float minX,
        float maxX,
        float minY,
        float maxY,
        out FloorZone? zone,
        out string? error)
    {
        zone = null;
        error = null;

        if (floor.Points.Count < 3)
        {
            error = "Piso inválido.";
            return false;
        }

        if (!floor.TryGetBounds(out Vector2 gmin, out Vector2 gmax))
        {
            error = "Piso sem limites.";
            return false;
        }

        minX = Math.Clamp(minX, gmin.X, gmax.X - MinSpanMm);
        maxX = Math.Clamp(maxX, minX + MinSpanMm, gmax.X);
        minY = Math.Clamp(minY, gmin.Y, gmax.Y - MinSpanMm);
        maxY = Math.Clamp(maxY, minY + MinSpanMm, gmax.Y);

        zone = new FloorZone
        {
            Shape = WallRegionShape.Rectangular,
            MinX = minX,
            MinY = minY,
            MaxX = maxX,
            MaxY = maxY,
            Name = "Região retangular"
        };

        return TryAddZone(floor, zone, out error);
    }

    public static bool TryAddDefaultRectZone(FloorSurface floor, out FloorZone? zone, out string? error)
    {
        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
        {
            zone = null;
            error = "Piso sem limites.";
            return false;
        }

        float width = MathF.Min(2000f, max.X - min.X);
        float depth = MathF.Min(2000f, max.Y - min.Y);
        float minX = min.X + (max.X - min.X - width) * 0.5f;
        float minY = min.Y + (max.Y - min.Y - depth) * 0.5f;

        if (!TryAddRectZone(floor, minX, minX + width, minY, minY + depth, out zone, out error))
            return false;

        if (zone != null)
        {
            zone.MaterialId = "porcelanato-cinza";
            zone.Name = "Região padrão";
        }

        return true;
    }

    public static bool TryAddCircleZone(
        FloorSurface floor,
        float centerX,
        float centerY,
        float radiusMm,
        out FloorZone? zone,
        out string? error)
    {
        zone = null;
        error = null;

        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
        {
            error = "Piso sem limites.";
            return false;
        }

        radiusMm = MathF.Max(MinCircleRadiusMm, radiusMm);
        centerX = Math.Clamp(centerX, min.X + radiusMm, max.X - radiusMm);
        centerY = Math.Clamp(centerY, min.Y + radiusMm, max.Y - radiusMm);

        zone = new FloorZone
        {
            Shape = WallRegionShape.Circular,
            CenterX = centerX,
            CenterY = centerY,
            RadiusMm = radiusMm,
            Name = "Círculo"
        };

        FloorZoneGeometry.SyncBoundingBox(zone);
        return TryAddZone(floor, zone, out error);
    }

    public static bool TryAddPolygonZone(
        FloorSurface floor,
        IReadOnlyList<float> xVertices,
        IReadOnlyList<float> yVertices,
        out FloorZone? zone,
        out string? error)
    {
        zone = null;
        error = null;

        if (xVertices.Count != yVertices.Count || xVertices.Count < MinPolygonVertices)
        {
            error = $"Polígono precisa de pelo menos {MinPolygonVertices} vértices.";
            return false;
        }

        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
        {
            error = "Piso sem limites.";
            return false;
        }

        var xs = xVertices.ToArray();
        var ys = yVertices.ToArray();

        for (int i = 0; i < xs.Length; i++)
        {
            xs[i] = Math.Clamp(xs[i], min.X, max.X);
            ys[i] = Math.Clamp(ys[i], min.Y, max.Y);
        }

        float area = MathF.Abs(WallRegionGeometry.ComputeSignedArea(xs, ys));
        if (area < MinPolygonAreaMm2)
        {
            error = $"Polígono muito pequeno (mín. ~{MinPolygonAreaMm2:0} mm²).";
            return false;
        }

        zone = new FloorZone
        {
            Shape = WallRegionShape.Polygon,
            Name = "Polígono"
        };

        zone.PolygonAlongMm.AddRange(xs);
        zone.PolygonHeightMm.AddRange(ys);
        FloorZoneGeometry.SyncBoundingBox(zone);

        return TryAddZone(floor, zone, out error);
    }

    public static bool TrySetZoneEdge(
        FloorSurface floor,
        Guid zoneId,
        WallRegionEdgeKind edge,
        float newValueMm,
        out string? error)
    {
        error = null;
        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
        {
            error = "Piso sem limites.";
            return false;
        }

        if (zone.Shape == WallRegionShape.Circular)
        {
            if (edge == WallRegionEdgeKind.Radius)
                return TrySetZoneRadius(floor, zoneId, newValueMm, out error);

            error = "Use arraste na borda do círculo para ajustar o raio.";
            return false;
        }

        if (zone.Shape == WallRegionShape.Polygon)
        {
            error = "Região poligonal: use offset ou redesenhe.";
            return false;
        }

        float minX = zone.MinX;
        float maxX = zone.MaxX;
        float minY = zone.MinY;
        float maxY = zone.MaxY;

        switch (edge)
        {
            case WallRegionEdgeKind.StartAlong:
                minX = Math.Clamp(newValueMm, min.X, maxX - MinSpanMm);
                break;
            case WallRegionEdgeKind.EndAlong:
                maxX = Math.Clamp(newValueMm, minX + MinSpanMm, max.X);
                break;
            case WallRegionEdgeKind.Bottom:
                minY = Math.Clamp(newValueMm, min.Y, maxY - MinSpanMm);
                break;
            case WallRegionEdgeKind.Top:
                maxY = Math.Clamp(newValueMm, minY + MinSpanMm, max.Y);
                break;
            default:
                error = "Borda inválida.";
                return false;
        }

        zone.MinX = minX;
        zone.MaxX = maxX;
        zone.MinY = minY;
        zone.MaxY = maxY;

        return ValidateNoOverlap(floor, zone, out error);
    }

    public static bool TrySetZoneRadius(
        FloorSurface floor,
        Guid zoneId,
        float radiusMm,
        out string? error)
    {
        error = null;
        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null || zone.Shape != WallRegionShape.Circular)
        {
            error = "Região circular não encontrada.";
            return false;
        }

        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
        {
            error = "Piso sem limites.";
            return false;
        }

        radiusMm = MathF.Max(MinCircleRadiusMm, radiusMm);
        float maxRadius = MathF.Min(
            MathF.Min(zone.CenterX - min.X, max.X - zone.CenterX),
            MathF.Min(zone.CenterY - min.Y, max.Y - zone.CenterY));

        zone.RadiusMm = MathF.Min(radiusMm, maxRadius);
        FloorZoneGeometry.SyncBoundingBox(zone);

        return ValidateNoOverlap(floor, zone, out error);
    }

    public static bool TrySetZoneOffset(
        FloorSurface floor,
        Guid zoneId,
        float offsetMm,
        out string? error)
    {
        error = null;
        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        zone.OffsetMm = offsetMm;

        if (zone.Shape == WallRegionShape.Circular || zone.Shape == WallRegionShape.Polygon)
            FloorZoneGeometry.SyncBoundingBox(zone);

        return ValidateNoOverlap(floor, zone, out error);
    }

    public static bool TrySetZoneEdgeOffset(
        FloorSurface floor,
        Guid zoneId,
        WallRegionEdgeKind edge,
        float offsetMm,
        out string? error)
    {
        error = null;
        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (zone.Shape != WallRegionShape.Rectangular)
        {
            error = "Offset por aresta só em região retangular. Use offset forma.";
            return false;
        }

        switch (edge)
        {
            case WallRegionEdgeKind.StartAlong:
                zone.OffsetEdgeStartAlongMm = offsetMm;
                break;
            case WallRegionEdgeKind.EndAlong:
                zone.OffsetEdgeEndAlongMm = offsetMm;
                break;
            case WallRegionEdgeKind.Bottom:
                zone.OffsetEdgeBottomMm = offsetMm;
                break;
            case WallRegionEdgeKind.Top:
                zone.OffsetEdgeTopMm = offsetMm;
                break;
            default:
                error = "Borda inválida para offset.";
                return false;
        }

        return ValidateNoOverlap(floor, zone, out error);
    }

    public static bool TryAdjustZoneEdgeOffset(
        FloorSurface floor,
        Guid zoneId,
        WallRegionEdgeKind edge,
        float deltaMm,
        out string? error)
    {
        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        float current = edge switch
        {
            WallRegionEdgeKind.StartAlong => zone.OffsetEdgeStartAlongMm,
            WallRegionEdgeKind.EndAlong => zone.OffsetEdgeEndAlongMm,
            WallRegionEdgeKind.Bottom => zone.OffsetEdgeBottomMm,
            WallRegionEdgeKind.Top => zone.OffsetEdgeTopMm,
            _ => 0f
        };

        return TrySetZoneEdgeOffset(floor, zoneId, edge, current + deltaMm, out error);
    }

    public static float AreaSquareMeters(FloorZone zone)
    {
        if (zone.Shape == WallRegionShape.Circular)
            return MathF.PI * zone.RadiusMm * zone.RadiusMm / 1_000_000f;

        if (zone.Shape == WallRegionShape.Polygon && zone.PolygonAlongMm.Count >= 3)
            return MathF.Abs(WallRegionGeometry.ComputeSignedArea(
                zone.PolygonAlongMm.ToArray(),
                zone.PolygonHeightMm.ToArray())) / 1_000_000f;

        return zone.Width * zone.Depth / 1_000_000f;
    }

    private static bool TryAddZone(FloorSurface floor, FloorZone zone, out string? error)
    {
        if (!IsZoneInsideFloor(zone, floor.Points))
        {
            error = "Região fora do piso.";
            return false;
        }

        if (!ValidateNoOverlap(floor, zone, out error))
            return false;

        floor.Zones.Add(zone);
        return true;
    }

    private static bool ValidateNoOverlap(FloorSurface floor, FloorZone candidate, out string? error)
    {
        foreach (var other in floor.Zones)
        {
            if (other.Id == candidate.Id)
                continue;

            if (FloorZoneGeometry.ZonesOverlap(candidate, other))
            {
                error = "Regiões do piso não podem se sobrepor.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
