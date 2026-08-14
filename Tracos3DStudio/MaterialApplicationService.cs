namespace Tracos3DStudio;

public enum MaterialListFilter
{
    All,
    Modules,
    Floors
}

/// <summary>Modo explícito de aplicação na janela Materiais (C.3). Automático segue o alvo sob o cursor ou selecionado.</summary>
public enum MaterialApplicationMode
{
    Auto,
    Module,
    WallFace,
    WallBand,
    WallRegion,
    Floor,
    FloorZone
}

public sealed class MaterialApplicationContext
{
    public Guid? ModuleId { get; init; }

    public Guid? WallId { get; init; }

    public Guid? WallBandId { get; init; }

    public Guid? WallRegionId { get; init; }

    public FaceType? WallFace { get; init; }

    public bool FloorSelected { get; init; }

    public Guid? FloorZoneId { get; init; }

    public bool HasApplyTarget =>
        ModuleId.HasValue ||
        FloorZoneId.HasValue ||
        (FloorSelected && !FloorZoneId.HasValue) ||
        (WallId.HasValue && (WallRegionId.HasValue || WallBandId.HasValue || WallFace.HasValue));
}

/// <summary>Material ativo na janela Exibir → Materiais (C.1). Aplicação por drag em C.2.</summary>
public static class MaterialApplicationService
{
    public static string ActiveMaterialId { get; set; } = MaterialCatalog.DefaultMaterialId;

    public static MaterialApplicationMode ApplicationMode { get; set; } = MaterialApplicationMode.Auto;

    public static IReadOnlyList<WallSurfaceMaterialOption> GetFilteredOptions(MaterialListFilter filter)
    {
        var all = WallSurfaceMaterialCatalog.All;

        return filter switch
        {
            MaterialListFilter.Modules => all
                .Where(o => MaterialCatalog.TryGet(o.Id, out _))
                .ToList(),
            MaterialListFilter.Floors => all
                .Where(o => FloorMaterialCatalog.TryGet(o.Id, out _))
                .ToList(),
            _ => all.ToList()
        };
    }

    public static MaterialListFilter GetGroupForMaterial(string materialId)
    {
        if (MaterialCatalog.TryGet(materialId, out _))
            return MaterialListFilter.Modules;

        if (FloorMaterialCatalog.TryGet(materialId, out _))
            return MaterialListFilter.Floors;

        return MaterialListFilter.All;
    }

    public static bool TryApplyMaterial(
        Project project,
        MaterialApplicationContext context,
        string materialId,
        out MaterialApplicationTarget appliedTarget,
        out string? error)
    {
        appliedTarget = MaterialApplicationTarget.None;
        error = null;

        if (string.IsNullOrWhiteSpace(materialId))
        {
            error = "Material inválido.";
            return false;
        }

        if (WallSurfaceMaterialCatalog.FindOption(materialId) == null)
        {
            error = $"Material '{materialId}' não encontrado.";
            return false;
        }

        ActiveMaterialId = materialId;

        if (!TryResolveEffectiveContext(context, out var effective, out error))
            return false;

        if (effective.ModuleId is Guid moduleId)
        {
            if (!TryApplyToModule(project, moduleId, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.Module;
            return true;
        }

        if (effective.FloorZoneId is Guid floorZoneId)
        {
            if (!TryApplyToFloorZone(project, floorZoneId, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.FloorZone;
            return true;
        }

        if (effective.WallId is Guid wallId && effective.WallRegionId is Guid regionId)
        {
            if (!TryApplyToWallRegion(project, wallId, regionId, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.WallRegion;
            return true;
        }

        if (effective.WallId is Guid bandWallId && effective.WallBandId is Guid bandId)
        {
            if (!TryApplyToWallBand(project, bandWallId, bandId, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.WallBand;
            return true;
        }

        if (effective.WallId is Guid faceWallId && effective.WallFace is FaceType face)
        {
            if (!TryApplyToWallFace(project, faceWallId, face, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.WallFace;
            return true;
        }

        if (effective.FloorSelected && project.Room.Floor != null)
        {
            if (!TryApplyToFloorBase(project, materialId, out error))
                return false;

            appliedTarget = MaterialApplicationTarget.FloorBase;
            return true;
        }

        return true;
    }

    public static bool TryResolveEffectiveContext(
        MaterialApplicationContext context,
        out MaterialApplicationContext effective,
        out string? error)
    {
        effective = context;
        error = null;

        if (ApplicationMode == MaterialApplicationMode.Auto)
            return true;

        effective = ApplicationMode switch
        {
            MaterialApplicationMode.Module => new MaterialApplicationContext { ModuleId = context.ModuleId },
            MaterialApplicationMode.WallFace => new MaterialApplicationContext
            {
                WallId = context.WallId,
                WallFace = context.WallFace ?? FaceType.Internal
            },
            MaterialApplicationMode.WallBand => new MaterialApplicationContext
            {
                WallId = context.WallId,
                WallBandId = context.WallBandId
            },
            MaterialApplicationMode.WallRegion => new MaterialApplicationContext
            {
                WallId = context.WallId,
                WallRegionId = context.WallRegionId
            },
            MaterialApplicationMode.Floor => new MaterialApplicationContext { FloorSelected = true },
            MaterialApplicationMode.FloorZone => new MaterialApplicationContext { FloorZoneId = context.FloorZoneId },
            _ => context
        };

        if (effective.HasApplyTarget)
            return true;

        error = ApplicationMode switch
        {
            MaterialApplicationMode.Module => "Selecione um módulo no projeto.",
            MaterialApplicationMode.WallFace => "Selecione uma parede e a face (interna/externa).",
            MaterialApplicationMode.WallBand => "Selecione uma parede e uma faixa.",
            MaterialApplicationMode.WallRegion => "Selecione uma parede e uma região.",
            MaterialApplicationMode.Floor => "Selecione o piso.",
            MaterialApplicationMode.FloorZone => "Selecione uma região do piso.",
            _ => "Nenhum alvo compatível com o modo selecionado."
        };

        return false;
    }

    public static bool TryApplyToModule(Project project, Guid moduleId, string materialId, out string? error)
    {
        error = null;

        if (!MaterialCatalog.TryGet(materialId, out _))
        {
            error = "Este acabamento não está disponível para módulos.";
            return false;
        }

        var module = project.FindModule(moduleId);

        if (module == null)
        {
            error = "Módulo não encontrado.";
            return false;
        }

        module.MaterialId = materialId;
        ActiveMaterialId = materialId;
        return true;
    }

    public static bool TryApplyToWallBand(
        Project project,
        Guid wallId,
        Guid bandId,
        string materialId,
        out string? error)
    {
        error = null;
        var wall = FindWall(project, wallId);

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

        band.MaterialId = materialId;
        ActiveMaterialId = materialId;
        return true;
    }

    public static bool TryApplyToWallRegion(
        Project project,
        Guid wallId,
        Guid regionId,
        string materialId,
        out string? error)
    {
        error = null;
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

        region.MaterialId = materialId;
        ActiveMaterialId = materialId;
        return true;
    }

    public static bool TryApplyToWallFace(
        Project project,
        Guid wallId,
        FaceType face,
        string materialId,
        out string? error)
    {
        error = null;
        var wall = FindWall(project, wallId);

        if (wall == null)
        {
            error = "Parede não encontrada.";
            return false;
        }

        wall.SetFaceMaterialId(face, materialId);
        ActiveMaterialId = materialId;
        return true;
    }

    public static bool TryApplyToFloorZone(Project project, Guid zoneId, string materialId, out string? error)
    {
        error = null;

        if (!FloorMaterialCatalog.TryGet(materialId, out _))
        {
            error = "Este acabamento não está disponível para pisos.";
            return false;
        }

        var floor = project.Room.Floor;

        if (floor == null)
        {
            error = "Piso não encontrado.";
            return false;
        }

        var zone = floor.Zones.FirstOrDefault(z => z.Id == zoneId);

        if (zone == null)
        {
            error = "Região do piso não encontrada.";
            return false;
        }

        zone.MaterialId = materialId;
        ActiveMaterialId = materialId;
        return true;
    }

    public static bool TryApplyToFloorBase(Project project, string materialId, out string? error)
    {
        error = null;

        if (!FloorMaterialCatalog.TryGet(materialId, out _))
        {
            error = "Este acabamento não está disponível para pisos.";
            return false;
        }

        var floor = project.Room.Floor;

        if (floor == null)
        {
            error = "Piso não encontrado.";
            return false;
        }

        floor.DefaultMaterialId = materialId;
        ActiveMaterialId = materialId;
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

public enum MaterialApplicationTarget
{
    None,
    Module,
    WallBand,
    WallRegion,
    WallFace,
    FloorZone,
    FloorBase
}
