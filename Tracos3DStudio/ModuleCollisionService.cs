using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ModuleCollisionService
{
    /// <summary>
    /// Penetração mínima (mm) para considerar colisão. Encostar face a face (gap 0) não colide.
    /// </summary>
    public const float MinPenetrationMm = 1f;

    public static bool IntersectsBounds(
        (Vector3 Min, Vector3 Max) a,
        (Vector3 Min, Vector3 Max) b,
        float minPenetrationMm = MinPenetrationMm)
    {
        return a.Min.X < b.Max.X - minPenetrationMm && a.Max.X > b.Min.X + minPenetrationMm &&
               a.Min.Y < b.Max.Y - minPenetrationMm && a.Max.Y > b.Min.Y + minPenetrationMm &&
               a.Min.Z < b.Max.Z - minPenetrationMm && a.Max.Z > b.Min.Z + minPenetrationMm;
    }

    public static bool WouldCollide(
        Vector3 position,
        float width,
        float height,
        float depth,
        float rotationYDegrees,
        IReadOnlyList<ModuleInstance> modules,
        Guid? ignoreModuleId = null,
        Guid? candidateWallId = null,
        float distanceAlongWall = 0f,
        ModuleDefinition? candidateDefinition = null)
    {
        foreach (var module in modules)
        {
            if (ignoreModuleId.HasValue && ignoreModuleId.Value == module.Id)
                continue;

            if (!ShouldCollidePair(candidateWallId, module))
                continue;

            if (TryWallFaceCollision(
                    candidateWallId,
                    candidateDefinition,
                    distanceAlongWall,
                    position.Y,
                    width,
                    height,
                    module))
                return true;

            if (candidateWallId.HasValue && module.AttachedWallId.HasValue)
                continue;

            var candidateBounds = ModulePlacementService.ComputeBounds(
                position, width, height, depth, rotationYDegrees);

            if (IntersectsBounds(candidateBounds, module.GetBounds()))
                return true;
        }

        return false;
    }

    public static bool WouldCollide(ModuleInstance candidate, IReadOnlyList<ModuleInstance> modules)
    {
        var candidateDefinition = ModuleCatalog.GetRequired(candidate.DefinitionId);

        foreach (var module in modules)
        {
            if (!ShouldCollidePair(candidate, module))
                continue;

            if (TryWallFaceCollision(
                    candidate.AttachedWallId,
                    candidateDefinition,
                    candidate.DistanceAlongWall,
                    candidate.Position.Y,
                    candidate.Width,
                    candidate.Height,
                    module))
                return true;

            if (candidate.AttachedWallId.HasValue && module.AttachedWallId.HasValue)
                continue;

            if (IntersectsBounds(candidate.GetBounds(), module.GetBounds()))
                return true;
        }

        return false;
    }

    public static HashSet<Guid> FindCollidingModuleIds(IReadOnlyList<ModuleInstance> modules)
    {
        var result = new HashSet<Guid>();

        for (int i = 0; i < modules.Count; i++)
        {
            var definitionA = ModuleCatalog.GetRequired(modules[i].DefinitionId);

            for (int j = i + 1; j < modules.Count; j++)
            {
                if (!ShouldCollidePair(modules[i], modules[j]))
                    continue;

                var definitionB = ModuleCatalog.GetRequired(modules[j].DefinitionId);

                if (modules[i].AttachedWallId.HasValue &&
                    modules[j].AttachedWallId.HasValue &&
                    modules[i].AttachedWallId.Value == modules[j].AttachedWallId.Value &&
                    ModuleWallFaceService.SameMountBand(definitionA, definitionB))
                {
                    if (!ModuleWallFaceService.OverlapsOnWallFace(
                            ModuleWallFaceService.GetWallFaceRectangle(modules[i]),
                            ModuleWallFaceService.GetWallFaceRectangle(modules[j])))
                        continue;
                }
                else if (!IntersectsBounds(modules[i].GetBounds(), modules[j].GetBounds()))
                {
                    continue;
                }

                result.Add(modules[i].Id);
                result.Add(modules[j].Id);
            }
        }

        return result;
    }

    private static bool TryWallFaceCollision(
        Guid? candidateWallId,
        ModuleDefinition? candidateDefinition,
        float candidateAlong,
        float candidateMountY,
        float candidateWidth,
        float candidateHeight,
        ModuleInstance other)
    {
        if (!candidateWallId.HasValue || !other.AttachedWallId.HasValue ||
            candidateWallId.Value != other.AttachedWallId.Value ||
            candidateDefinition == null)
            return false;

        var otherDefinition = ModuleCatalog.GetRequired(other.DefinitionId);

        if (!ModuleWallFaceService.SameMountBand(candidateDefinition, otherDefinition))
            return false;

        var candidateRect = ModuleWallFaceService.GetWallFaceRectangle(
            candidateAlong,
            candidateWidth,
            candidateMountY,
            candidateHeight);

        return ModuleWallFaceService.OverlapsOnWallFace(
            candidateRect,
            ModuleWallFaceService.GetWallFaceRectangle(other));
    }

    private static bool ShouldCollidePair(ModuleInstance a, ModuleInstance b)
    {
        if (a.Id == b.Id)
            return false;

        if (a.AttachedWallId.HasValue && b.AttachedWallId.HasValue &&
            a.AttachedWallId.Value != b.AttachedWallId.Value)
            return false;

        return true;
    }

    private static bool ShouldCollidePair(Guid? candidateWallId, ModuleInstance other)
    {
        if (candidateWallId.HasValue && other.AttachedWallId.HasValue &&
            candidateWallId.Value != other.AttachedWallId.Value)
            return false;

        return true;
    }
}
