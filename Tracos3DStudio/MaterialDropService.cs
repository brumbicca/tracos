using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>Resultado de raycast no viewport para soltar material (C.2).</summary>
public sealed class MaterialDropRayHit
{
    public float ModuleDistance { get; init; } = float.MaxValue;

    public ModuleInstance? Module { get; init; }

    public float WallDistance { get; init; } = float.MaxValue;

    public WallSegment? Wall { get; init; }

    public float Along { get; init; }

    public float Height { get; init; }

    public FaceType Face { get; init; }

    public bool WallHitTop { get; init; }

    public float FloorDistance { get; init; } = float.MaxValue;

    public Vector2 FloorPoint { get; init; }

    public bool HasFloorHit { get; init; }
}

public static class MaterialDropService
{
    public static WallRegion? PickRegionAt(WallSegment wall, FaceType face, float along, float height)
    {
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        WallRegion? best = null;
        float bestArea = float.MaxValue;

        foreach (var region in wall.Regions)
        {
            if (region.Face != face)
                continue;

            if (!WallRegionGeometry.ContainsPoint(region, along, height))
                continue;

            float area = EstimateRegionArea(region, wall.Length, wallTop);

            if (area < bestArea)
            {
                bestArea = area;
                best = region;
            }
        }

        return best;
    }

    public static WallBand? PickBandAt(WallSegment wall, float along, float height)
    {
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        WallBand? best = null;
        float bestArea = float.MaxValue;

        foreach (var band in wall.Bands)
        {
            if (!WallBandGeometry.ContainsPoint(band, along, height, wall.Length, wallTop))
                continue;

            float area = WallBandGeometry.EstimateArea(band, wall.Length, wallTop);

            if (area < bestArea)
            {
                bestArea = area;
                best = band;
            }
        }

        return best;
    }

    public static FloorZone? PickFloorZoneAt(FloorSurface floor, Vector2 point)
    {
        if (!floor.TryGetBounds(out Vector2 min, out Vector2 max))
            return null;

        FloorZone? best = null;
        float bestArea = float.MaxValue;

        foreach (var zone in floor.Zones)
        {
            if (!zone.ContainsPoint(point))
                continue;

            var (minX, maxX, minY, maxY) = FloorZoneGeometry.GetEffectiveBounds(
                zone, min.X, min.Y, max.X, max.Y);

            float area = (maxX - minX) * (maxY - minY);

            if (area < bestArea)
            {
                bestArea = area;
                best = zone;
            }
        }

        return best;
    }

    public static bool TryResolveTarget(
        Project project,
        MaterialDropRayHit hit,
        out MaterialApplicationContext context,
        out MaterialApplicationTarget targetKind) =>
        TryResolveTarget(
            project,
            hit,
            MaterialApplicationService.ApplicationMode,
            out context,
            out targetKind);

    public static bool TryResolveTarget(
        Project project,
        MaterialDropRayHit hit,
        MaterialApplicationMode mode,
        out MaterialApplicationContext context,
        out MaterialApplicationTarget targetKind)
    {
        if (mode == MaterialApplicationMode.Auto)
            return TryResolveTargetAuto(project, hit, out context, out targetKind);

        return TryResolveTargetForced(project, hit, mode, out context, out targetKind);
    }

    private static bool TryResolveTargetAuto(
        Project project,
        MaterialDropRayHit hit,
        out MaterialApplicationContext context,
        out MaterialApplicationTarget targetKind)
    {
        context = new MaterialApplicationContext();
        targetKind = MaterialApplicationTarget.None;

        float moduleDist = hit.Module != null ? hit.ModuleDistance : float.MaxValue;
        float wallDist = hit.Wall != null && !hit.WallHitTop ? hit.WallDistance : float.MaxValue;
        float floorDist = hit.HasFloorHit ? hit.FloorDistance : float.MaxValue;

        if (hit.Module != null && moduleDist <= wallDist && moduleDist <= floorDist)
        {
            context = new MaterialApplicationContext { ModuleId = hit.Module.Id };
            targetKind = MaterialApplicationTarget.Module;
            return true;
        }

        if (hit.Wall != null && !hit.WallHitTop && wallDist <= floorDist)
        {
            var region = PickRegionAt(hit.Wall, hit.Face, hit.Along, hit.Height);

            if (region != null)
            {
                context = new MaterialApplicationContext
                {
                    WallId = hit.Wall.Id,
                    WallRegionId = region.Id
                };
                targetKind = MaterialApplicationTarget.WallRegion;
                return true;
            }

            var band = PickBandAt(hit.Wall, hit.Along, hit.Height);

            if (band != null)
            {
                context = new MaterialApplicationContext
                {
                    WallId = hit.Wall.Id,
                    WallBandId = band.Id
                };
                targetKind = MaterialApplicationTarget.WallBand;
                return true;
            }

            context = new MaterialApplicationContext
            {
                WallId = hit.Wall.Id,
                WallFace = hit.Face
            };
            targetKind = MaterialApplicationTarget.WallFace;
            return true;
        }

        if (hit.HasFloorHit && project.Room.Floor != null)
        {
            var zone = PickFloorZoneAt(project.Room.Floor, hit.FloorPoint);

            if (zone != null)
            {
                context = new MaterialApplicationContext { FloorZoneId = zone.Id };
                targetKind = MaterialApplicationTarget.FloorZone;
                return true;
            }

            context = new MaterialApplicationContext { FloorSelected = true };
            targetKind = MaterialApplicationTarget.FloorBase;
            return true;
        }

        return false;
    }

    private static bool TryResolveTargetForced(
        Project project,
        MaterialDropRayHit hit,
        MaterialApplicationMode mode,
        out MaterialApplicationContext context,
        out MaterialApplicationTarget targetKind)
    {
        context = new MaterialApplicationContext();
        targetKind = MaterialApplicationTarget.None;

        switch (mode)
        {
            case MaterialApplicationMode.Module:
                if (hit.Module == null)
                    return false;

                context = new MaterialApplicationContext { ModuleId = hit.Module.Id };
                targetKind = MaterialApplicationTarget.Module;
                return true;

            case MaterialApplicationMode.WallFace:
                if (hit.Wall == null || hit.WallHitTop)
                    return false;

                context = new MaterialApplicationContext
                {
                    WallId = hit.Wall.Id,
                    WallFace = hit.Face
                };
                targetKind = MaterialApplicationTarget.WallFace;
                return true;

            case MaterialApplicationMode.WallBand:
                if (hit.Wall == null || hit.WallHitTop)
                    return false;

                var band = PickBandAt(hit.Wall, hit.Along, hit.Height);

                if (band == null)
                    return false;

                context = new MaterialApplicationContext
                {
                    WallId = hit.Wall.Id,
                    WallBandId = band.Id
                };
                targetKind = MaterialApplicationTarget.WallBand;
                return true;

            case MaterialApplicationMode.WallRegion:
                if (hit.Wall == null || hit.WallHitTop)
                    return false;

                var region = PickRegionAt(hit.Wall, hit.Face, hit.Along, hit.Height);

                if (region == null)
                    return false;

                context = new MaterialApplicationContext
                {
                    WallId = hit.Wall.Id,
                    WallRegionId = region.Id
                };
                targetKind = MaterialApplicationTarget.WallRegion;
                return true;

            case MaterialApplicationMode.FloorZone:
                if (!hit.HasFloorHit || project.Room.Floor == null)
                    return false;

                var zone = PickFloorZoneAt(project.Room.Floor, hit.FloorPoint);

                if (zone == null)
                    return false;

                context = new MaterialApplicationContext { FloorZoneId = zone.Id };
                targetKind = MaterialApplicationTarget.FloorZone;
                return true;

            case MaterialApplicationMode.Floor:
                if (!hit.HasFloorHit || project.Room.Floor == null)
                    return false;

                context = new MaterialApplicationContext { FloorSelected = true };
                targetKind = MaterialApplicationTarget.FloorBase;
                return true;

            default:
                return TryResolveTargetAuto(project, hit, out context, out targetKind);
        }
    }

    private static float EstimateRegionArea(WallRegion region, float wallLength, float wallTop)
    {
        if (region.Shape == WallRegionShape.Circular)
        {
            float r = WallRegionGeometry.GetEffectiveRadius(region);
            return MathF.PI * r * r;
        }

        if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            return MathF.Abs(WallRegionGeometry.ComputeSignedArea(
                region.PolygonAlongMm.ToArray(),
                region.PolygonHeightMm.ToArray()));
        }

        if (region.Shape == WallRegionShape.Rectangular && MathF.Abs(region.RotationDegrees) > 0.01f)
        {
            return (region.EndAlongMm - region.StartAlongMm) * (region.TopMm - region.BottomMm);
        }

        var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, wallLength, wallTop);
        return (end - start) * (top - bottom);
    }
}
