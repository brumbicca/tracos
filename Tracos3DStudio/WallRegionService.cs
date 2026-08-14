namespace Tracos3DStudio;

public enum WallRegionEdgeKind
{
    StartAlong,
    EndAlong,
    Bottom,
    Top,
    Radius
}

public static class WallRegionService
{
    public const float MinSpanMm = 50f;
    public const float DefaultCircleRadiusMm = 600f;
    public const float MinCircleRadiusMm = 25f;
    public const int MinPolygonVertices = 3;
    public const float MinPolygonAreaMm2 = 2500f;
    public const float PolygonVertexInsertToleranceMm = 120f;
    public const float MinPolygonVertexSpacingMm = 80f;
    public const float RegionBodyDragEdgeToleranceMm = 120f;

    public static bool TryMoveRegion(
        WallSegment wall,
        Guid regionId,
        float deltaAlong,
        float deltaHeight,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        (deltaAlong, deltaHeight) = WallRegionGeometry.ClampMoveDelta(
            region,
            wall.Length,
            wallTop,
            deltaAlong,
            deltaHeight);

        if (MathF.Abs(deltaAlong) < 0.01f && MathF.Abs(deltaHeight) < 0.01f)
            return true;

        var snapshot = WallRegionMoveSnapshot.From(region);
        WallRegionGeometry.ApplyMoveDelta(region, deltaAlong, deltaHeight);

        if (!ValidateNoOverlap(wall, region, out error))
        {
            snapshot.RestoreTo(region);
            return false;
        }

        return true;
    }

    public static bool TryRotateRegion(
        WallSegment wall,
        Guid regionId,
        float rotationDegrees,
        out string? error)
    {
        error = null;
        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            error = "Região circular não pode ser rotacionada.";
            return false;
        }

        var snapshot = WallRegionMoveSnapshot.From(region);
        WallRegionGeometry.ApplyRotationDegrees(region, rotationDegrees);

        if (!ValidateNoOverlap(wall, region, out error))
        {
            snapshot.RestoreTo(region);
            return false;
        }

        return true;
    }

    public static bool TryRotateRegionByDelta(
        WallSegment wall,
        Guid regionId,
        float deltaDegrees,
        out string? error)
    {
        error = null;
        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            error = "Região circular não pode ser rotacionada.";
            return false;
        }

        if (MathF.Abs(deltaDegrees) < 0.01f)
            return true;

        var snapshot = WallRegionMoveSnapshot.From(region);
        WallRegionGeometry.ApplyRotationDelta(region, deltaDegrees);

        if (!ValidateNoOverlap(wall, region, out error))
        {
            snapshot.RestoreTo(region);
            return false;
        }

        return true;
    }

    public static bool TryVerticalCutRegion(
        WallSegment wall,
        Guid regionId,
        float cutAlongMm,
        out Guid leftRegionId,
        out Guid rightRegionId,
        out string? error)
    {
        leftRegionId = rightRegionId = Guid.Empty;
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            error = "Região circular não pode ser cortada.";
            return false;
        }

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);

        if (!WallRegionGeometry.TryBuildVerticalCutParts(
                region,
                wall.Length,
                wallTop,
                cutAlongMm,
                out WallRegion leftPart,
                out WallRegion rightPart,
                out error))
            return false;

        string baseName = region.Name ?? "Região";
        leftPart.Name = $"{baseName} esq";
        rightPart.Name = $"{baseName} dir";

        wall.Regions.Remove(region);

        if (!TryAddRegion(wall, leftPart, out error))
        {
            wall.Regions.Add(region);
            return false;
        }

        if (!TryAddRegion(wall, rightPart, out error))
        {
            wall.Regions.Remove(leftPart);
            wall.Regions.Add(region);
            return false;
        }

        leftRegionId = leftPart.Id;
        rightRegionId = rightPart.Id;
        return true;
    }

    public static bool TryInsertPolygonVertexAtPoint(
        WallSegment wall,
        Guid regionId,
        float along,
        float height,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null || region.Shape != WallRegionShape.Polygon)
        {
            error = "Selecione uma região poligonal.";
            return false;
        }

        if (!WallRegionGeometry.TryFindPolygonEdgeForVertexInsert(
                region,
                along,
                height,
                PolygonVertexInsertToleranceMm,
                MinPolygonVertexSpacingMm,
                out int edgeStartIndex,
                out float insertAlong,
                out float insertHeight))
        {
            error = "Clique mais perto da aresta do polígono (longe dos vértices existentes).";
            return false;
        }

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        insertAlong = Math.Clamp(insertAlong, 0f, wall.Length);
        insertHeight = Math.Clamp(insertHeight, 0f, wallTop);

        int insertAt = edgeStartIndex + 1;
        region.PolygonAlongMm.Insert(insertAt, insertAlong);
        region.PolygonHeightMm.Insert(insertAt, insertHeight);
        WallRegionGeometry.SyncBoundingBox(region);

        float area = MathF.Abs(WallRegionGeometry.ComputeSignedArea(
            region.PolygonAlongMm.ToArray(),
            region.PolygonHeightMm.ToArray()));

        if (area < MinPolygonAreaMm2)
        {
            region.PolygonAlongMm.RemoveAt(insertAt);
            region.PolygonHeightMm.RemoveAt(insertAt);
            WallRegionGeometry.SyncBoundingBox(region);
            error = $"Polígono muito pequeno (mín. ~{MinPolygonAreaMm2:0} mm²).";
            return false;
        }

        if (!ValidateNoOverlap(wall, region, out error))
        {
            region.PolygonAlongMm.RemoveAt(insertAt);
            region.PolygonHeightMm.RemoveAt(insertAt);
            WallRegionGeometry.SyncBoundingBox(region);
            return false;
        }

        return true;
    }

    public static bool TryAddPolygonRegion(
        WallSegment wall,
        FaceType face,
        IReadOnlyList<float> alongVertices,
        IReadOnlyList<float> heightVertices,
        out WallRegion? region,
        out string? error)
    {
        region = null;
        error = null;

        if (alongVertices.Count != heightVertices.Count ||
            alongVertices.Count < MinPolygonVertices)
        {
            error = $"Polígono precisa de pelo menos {MinPolygonVertices} vértices.";
            return false;
        }

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float length = wall.Length;

        if (length < MinSpanMm || wallTop < MinSpanMm)
        {
            error = "Parede muito curta para região.";
            return false;
        }

        var along = alongVertices.ToArray();
        var height = heightVertices.ToArray();

        for (int i = 0; i < along.Length; i++)
        {
            along[i] = Math.Clamp(along[i], 0f, length);
            height[i] = Math.Clamp(height[i], 0f, wallTop);
        }

        float area = MathF.Abs(WallRegionGeometry.ComputeSignedArea(along, height));
        if (area < MinPolygonAreaMm2)
        {
            error = $"Polígono muito pequeno (mín. ~{MinPolygonAreaMm2:0} mm²).";
            return false;
        }

        region = new WallRegion
        {
            Shape = WallRegionShape.Polygon,
            Face = face,
            Name = "Polígono"
        };

        region.PolygonAlongMm.AddRange(along);
        region.PolygonHeightMm.AddRange(height);
        WallRegionGeometry.SyncBoundingBox(region);

        if (!TryAddRegion(wall, region, out error))
        {
            region = null;
            return false;
        }

        return true;
    }

    public static bool TryAddRectRegion(
        WallSegment wall,
        FaceType face,
        float startAlongMm,
        float endAlongMm,
        float bottomMm,
        float topMm,
        out WallRegion? region,
        out string? error)
    {
        region = null;
        error = null;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float length = wall.Length;

        if (length < MinSpanMm || wallTop < MinSpanMm)
        {
            error = "Parede muito curta para região.";
            return false;
        }

        startAlongMm = Math.Clamp(startAlongMm, 0f, length - MinSpanMm);
        endAlongMm = Math.Clamp(endAlongMm, startAlongMm + MinSpanMm, length);
        bottomMm = Math.Clamp(bottomMm, 0f, wallTop - MinSpanMm);
        topMm = Math.Clamp(topMm, bottomMm + MinSpanMm, wallTop);

        region = new WallRegion
        {
            Shape = WallRegionShape.Rectangular,
            Face = face,
            StartAlongMm = startAlongMm,
            EndAlongMm = endAlongMm,
            BottomMm = bottomMm,
            TopMm = topMm,
            Name = face == FaceType.Internal ? "Face interna" : "Face externa"
        };

        if (!TryAddRegion(wall, region, out error))
        {
            region = null;
            return false;
        }

        return true;
    }

    public static bool TryAddCircleRegion(
        WallSegment wall,
        FaceType face,
        float centerAlongMm,
        float centerHeightMm,
        float radiusMm,
        out WallRegion? region,
        out string? error)
    {
        region = null;
        error = null;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float length = wall.Length;
        radiusMm = MathF.Max(MinCircleRadiusMm, radiusMm);

        if (length < MinSpanMm || wallTop < MinSpanMm)
        {
            error = "Parede muito curta para região.";
            return false;
        }

        centerAlongMm = Math.Clamp(centerAlongMm, radiusMm, length - radiusMm);
        centerHeightMm = Math.Clamp(centerHeightMm, radiusMm, wallTop - radiusMm);

        region = new WallRegion
        {
            Shape = WallRegionShape.Circular,
            Face = face,
            CenterAlongMm = centerAlongMm,
            CenterHeightMm = centerHeightMm,
            RadiusMm = radiusMm,
            Name = "Círculo"
        };

        WallRegionGeometry.SyncBoundingBox(region);

        if (!TryAddRegion(wall, region, out error))
        {
            region = null;
            return false;
        }

        return true;
    }

    public static bool TryAddDefaultTileRegion(WallSegment wall, out WallRegion? region, out string? error)
    {
        float length = wall.Length;
        float width = MathF.Min(1200f, length);
        float start = MathF.Max(0f, (length - width) * 0.5f);
        float end = start + width;

        if (!TryAddRectRegion(
            wall,
            FaceType.Internal,
            start,
            end,
            1100f,
            2100f,
            out region,
            out error))
            return false;

        if (region != null)
        {
            region.MaterialId = "ceramica-bege";
            region.Name = "Azulejo";
        }

        return true;
    }

    public static bool TrySetRegionEdge(
        WallSegment wall,
        Guid regionId,
        WallRegionEdgeKind edge,
        float newValueMm,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape == WallRegionShape.Circular)
        {
            if (edge == WallRegionEdgeKind.Radius)
                return TrySetRegionRadius(wall, regionId, newValueMm, out error);

            error = "Use arraste na borda do círculo para ajustar o raio.";
            return false;
        }

        if (region.Shape == WallRegionShape.Polygon)
        {
            error = "Região poligonal: use offset ou redesenhe a região.";
            return false;
        }

        float startAlong = region.StartAlongMm;
        float endAlong = region.EndAlongMm;
        float bottom = region.BottomMm;
        float top = region.TopMm;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float length = wall.Length;

        switch (edge)
        {
            case WallRegionEdgeKind.StartAlong:
                startAlong = Math.Clamp(newValueMm, 0f, endAlong - MinSpanMm);
                break;
            case WallRegionEdgeKind.EndAlong:
                endAlong = Math.Clamp(newValueMm, startAlong + MinSpanMm, length);
                break;
            case WallRegionEdgeKind.Bottom:
                bottom = Math.Clamp(newValueMm, 0f, top - MinSpanMm);
                break;
            case WallRegionEdgeKind.Top:
                top = Math.Clamp(newValueMm, bottom + MinSpanMm, wallTop);
                break;
            default:
                error = "Borda inválida para região retangular.";
                return false;
        }

        if (endAlong - startAlong < MinSpanMm)
        {
            error = $"Região precisa de largura mínima de {MinSpanMm:0} mm.";
            return false;
        }

        if (top - bottom < MinSpanMm)
        {
            error = $"Região precisa de altura mínima de {MinSpanMm:0} mm.";
            return false;
        }

        region.StartAlongMm = startAlong;
        region.EndAlongMm = endAlong;
        region.BottomMm = bottom;
        region.TopMm = top;

        return ValidateNoOverlap(wall, region, out error);
    }

    public static bool TrySetRegionRadius(
        WallSegment wall,
        Guid regionId,
        float radiusMm,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null || region.Shape != WallRegionShape.Circular)
        {
            error = "Região circular não encontrada.";
            return false;
        }

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float length = wall.Length;
        radiusMm = MathF.Max(MinCircleRadiusMm, radiusMm);

        float maxRadius = MathF.Min(
            MathF.Min(region.CenterAlongMm, length - region.CenterAlongMm),
            MathF.Min(region.CenterHeightMm, wallTop - region.CenterHeightMm));

        region.RadiusMm = MathF.Min(radiusMm, maxRadius);
        WallRegionGeometry.SyncBoundingBox(region);

        return ValidateNoOverlap(wall, region, out error);
    }

    public static bool TrySetRegionOffset(
        WallSegment wall,
        Guid regionId,
        float offsetMm,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        region.OffsetMm = offsetMm;

        if (region.Shape == WallRegionShape.Circular || region.Shape == WallRegionShape.Polygon)
            WallRegionGeometry.SyncBoundingBox(region);

        return ValidateNoOverlap(wall, region, out error);
    }

    public static bool TrySetRegionEdgeOffset(
        WallSegment wall,
        Guid regionId,
        WallRegionEdgeKind edge,
        float offsetMm,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape != WallRegionShape.Rectangular)
        {
            error = "Offset por aresta só em região retangular. Use offset forma.";
            return false;
        }

        switch (edge)
        {
            case WallRegionEdgeKind.StartAlong:
                region.OffsetEdgeStartAlongMm = offsetMm;
                break;
            case WallRegionEdgeKind.EndAlong:
                region.OffsetEdgeEndAlongMm = offsetMm;
                break;
            case WallRegionEdgeKind.Bottom:
                region.OffsetEdgeBottomMm = offsetMm;
                break;
            case WallRegionEdgeKind.Top:
                region.OffsetEdgeTopMm = offsetMm;
                break;
            default:
                error = "Borda inválida para offset por aresta.";
                return false;
        }

        return ValidateNoOverlap(wall, region, out error);
    }

    public static bool TryAdjustRegionEdgeOffset(
        WallSegment wall,
        Guid regionId,
        WallRegionEdgeKind edge,
        float deltaMm,
        out string? error)
    {
        error = null;

        var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

        if (region == null)
        {
            error = "Região não encontrada.";
            return false;
        }

        if (region.Shape != WallRegionShape.Rectangular)
        {
            error = "Offset por aresta só em região retangular.";
            return false;
        }

        float current = edge switch
        {
            WallRegionEdgeKind.StartAlong => region.OffsetEdgeStartAlongMm,
            WallRegionEdgeKind.EndAlong => region.OffsetEdgeEndAlongMm,
            WallRegionEdgeKind.Bottom => region.OffsetEdgeBottomMm,
            WallRegionEdgeKind.Top => region.OffsetEdgeTopMm,
            _ => 0f
        };

        return TrySetRegionEdgeOffset(wall, regionId, edge, current + deltaMm, out error);
    }

    private static bool TryAddRegion(WallSegment wall, WallRegion region, out string? error)
    {
        if (!ValidateNoOverlap(wall, region, out error))
            return false;

        wall.Regions.Add(region);
        return true;
    }

    private static bool ValidateNoOverlap(WallSegment wall, WallRegion candidate, out string? error)
    {
        error = null;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);

        foreach (var existing in wall.Regions)
        {
            if (existing.Id == candidate.Id || existing.Face != candidate.Face)
                continue;

            if (WallRegionGeometry.RegionsOverlap(candidate, existing, wall.Length, wallTop))
            {
                error = "Região sobrepõe outra na mesma face.";
                return false;
            }
        }

        return true;
    }
}
