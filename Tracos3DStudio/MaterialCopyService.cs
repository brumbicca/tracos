namespace Tracos3DStudio;

/// <summary>Lê material de um alvo para copiar entre objetos (M3).</summary>
public static class MaterialCopyService
{
    public static bool TryReadMaterial(
        Project project,
        MaterialApplicationContext context,
        out string? materialId,
        out MaterialApplicationTarget sourceTarget,
        out string? error)
    {
        materialId = null;
        sourceTarget = MaterialApplicationTarget.None;
        error = null;

        if (!context.HasApplyTarget)
        {
            error = "Selecione o item de origem.";
            return false;
        }

        if (context.ModuleId is Guid moduleId)
        {
            var module = project.FindModule(moduleId);

            if (module == null)
            {
                error = "Módulo não encontrado.";
                return false;
            }

            materialId = module.MaterialId;
            sourceTarget = MaterialApplicationTarget.Module;
            return ValidateMaterialId(materialId, out error);
        }

        if (context.FloorZoneId is Guid floorZoneId)
        {
            var floor = project.Room.Floor;

            if (floor == null)
            {
                error = "Piso não encontrado.";
                return false;
            }

            var zone = floor.Zones.FirstOrDefault(z => z.Id == floorZoneId);

            if (zone == null)
            {
                error = "Região do piso não encontrada.";
                return false;
            }

            materialId = zone.MaterialId;
            sourceTarget = MaterialApplicationTarget.FloorZone;
            return ValidateMaterialId(materialId, out error);
        }

        if (context.WallId is Guid wallId && context.WallRegionId is Guid regionId)
        {
            var wall = FindWall(project, wallId);

            if (wall == null)
            {
                error = "Parede não encontrada.";
                return false;
            }

            var region = wall.Regions.FirstOrDefault(r => r.Id == regionId);

            if (region == null)
            {
                error = "Região não encontrada.";
                return false;
            }

            materialId = region.MaterialId;
            sourceTarget = MaterialApplicationTarget.WallRegion;
            return ValidateMaterialId(materialId, out error);
        }

        if (context.WallId is Guid bandWallId && context.WallBandId is Guid bandId)
        {
            var wall = FindWall(project, bandWallId);

            if (wall == null)
            {
                error = "Parede não encontrada.";
                return false;
            }

            var band = wall.Bands.FirstOrDefault(b => b.Id == bandId);

            if (band == null)
            {
                error = "Faixa não encontrada.";
                return false;
            }

            materialId = band.MaterialId;
            sourceTarget = MaterialApplicationTarget.WallBand;
            return ValidateMaterialId(materialId, out error);
        }

        if (context.WallId is Guid faceWallId && context.WallFace is FaceType face)
        {
            var wall = FindWall(project, faceWallId);

            if (wall == null)
            {
                error = "Parede não encontrada.";
                return false;
            }

            materialId = wall.GetFaceMaterialId(face);
            sourceTarget = MaterialApplicationTarget.WallFace;
            return ValidateMaterialId(materialId, out error);
        }

        if (context.FloorSelected && project.Room.Floor != null)
        {
            materialId = project.Room.Floor.DefaultMaterialId;
            sourceTarget = MaterialApplicationTarget.FloorBase;
            return ValidateMaterialId(materialId, out error);
        }

        error = "Nenhum material neste item.";
        return false;
    }

    public static bool TryReadMaterialFromRayHit(
        Project project,
        MaterialDropRayHit hit,
        out string? materialId,
        out MaterialApplicationContext context,
        out MaterialApplicationTarget sourceTarget,
        out string? error)
    {
        materialId = null;
        context = new MaterialApplicationContext();
        sourceTarget = MaterialApplicationTarget.None;
        error = null;

        if (!MaterialDropService.TryResolveTarget(
                project,
                hit,
                MaterialApplicationMode.Auto,
                out context,
                out sourceTarget))
        {
            error = "Nenhum item com material neste ponto.";
            return false;
        }

        return TryReadMaterial(project, context, out materialId, out sourceTarget, out error);
    }

    public static bool TryCaptureToActive(
        Project project,
        MaterialApplicationContext context,
        out MaterialApplicationTarget sourceTarget,
        out string? error)
    {
        if (!TryReadMaterial(project, context, out string? materialId, out sourceTarget, out error))
            return false;

        MaterialApplicationService.ActiveMaterialId = materialId!;
        return true;
    }

    private static bool ValidateMaterialId(string? materialId, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(materialId))
        {
            error = "Este item não tem material definido.";
            return false;
        }

        if (WallSurfaceMaterialCatalog.FindOption(materialId) == null)
        {
            error = "Material não reconhecido.";
            return false;
        }

        return true;
    }

    private static WallSegment? FindWall(Project project, Guid wallId)
    {
        foreach (var wall in project.Room.Walls)
        {
            if (wall.Id == wallId)
                return wall;
        }

        return null;
    }
}
