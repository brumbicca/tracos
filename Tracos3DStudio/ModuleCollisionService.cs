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
        ModuleDefinition? candidateDefinition = null,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        foreach (var module in modules)
        {
            if (ignoreModuleId.HasValue && ignoreModuleId.Value == module.Id)
                continue;

            bool useGeometryEnvelope = UsesCornerGeometryEnvelope(candidateDefinition) || UsesCornerGeometryEnvelope(module);

            if (!ShouldCollidePair(candidateWallId, candidateDefinition, module))
                continue;

            if (!useGeometryEnvelope && TryWallFaceCollision(
                    candidateWallId,
                    candidateDefinition,
                    distanceAlongWall,
                    position.Y,
                    width,
                    height,
                    module))
                return true;

            if (!useGeometryEnvelope && candidateWallId.HasValue && module.AttachedWallId.HasValue)
                continue;

            var candidateBounds = ComputeCandidateBounds(
                position, width, height, depth, rotationYDegrees,
                candidateDefinition, dimensionSettings);

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
            bool useGeometryEnvelope = UsesCornerGeometryEnvelope(candidate) || UsesCornerGeometryEnvelope(module);

            if (!ShouldCollidePair(candidate, module))
                continue;

            if (!useGeometryEnvelope && TryWallFaceCollision(
                    candidate.AttachedWallId,
                    candidateDefinition,
                    candidate.DistanceAlongWall,
                    candidate.Position.Y,
                    candidate.Width,
                    candidate.Height,
                    module))
                return true;

            if (!useGeometryEnvelope && candidate.AttachedWallId.HasValue && module.AttachedWallId.HasValue)
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
                bool useGeometryEnvelope = UsesCornerGeometryEnvelope(definitionA) || UsesCornerGeometryEnvelope(definitionB);

                if (modules[i].AttachedWallId is Guid wallA &&
                    modules[j].AttachedWallId is Guid wallB &&
                    wallA == wallB &&
                    ModuleWallFaceService.SameMountBand(definitionA, definitionB) &&
                    !useGeometryEnvelope)
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
            return UsesCornerGeometryEnvelope(a) || UsesCornerGeometryEnvelope(b);

        return true;
    }

    private static bool ShouldCollidePair(
        Guid? candidateWallId,
        ModuleDefinition? candidateDefinition,
        ModuleInstance other)
    {
        if (candidateWallId.HasValue && other.AttachedWallId.HasValue &&
            candidateWallId.Value != other.AttachedWallId.Value)
            return UsesCornerGeometryEnvelope(candidateDefinition) || UsesCornerGeometryEnvelope(other);

        return true;
    }

    private static bool IsBlindCorner(ModuleInstance module) =>
        IsBlindCorner(ModuleCatalog.GetRequired(module.DefinitionId));

    private static bool IsBlindCorner(ModuleDefinition? definition) =>
        definition?.ShapeKind is ModuleShapeKind.BlindCornerLeft or ModuleShapeKind.BlindCornerRight;

    private static bool UsesCornerGeometryEnvelope(ModuleInstance module) =>
        UsesCornerGeometryEnvelope(ModuleCatalog.GetRequired(module.DefinitionId));

    private static bool UsesCornerGeometryEnvelope(ModuleDefinition? definition) =>
        IsBlindCorner(definition) || definition?.ShapeKind == ModuleShapeKind.Oblique;

    private static (Vector3 Min, Vector3 Max) ComputeCandidateBounds(
        Vector3 position,
        float width,
        float height,
        float depth,
        float rotationYDegrees,
        ModuleDefinition? definition,
        DimensionConfiguratorSettings? settings)
    {
        var nominal = ModulePlacementService.ComputeBounds(
            position, width, height, depth, rotationYDegrees);
        if (!UsesCornerGeometryEnvelope(definition))
            return nominal;

        settings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var numeric = settings.CozinhaInferiorBox.InferiorNumeric;
        string sideKey = definition!.ShapeKind == ModuleShapeKind.Oblique ? "cl-afa-lat" : "cr-afa-lat";
        string backKey = definition.ShapeKind == ModuleShapeKind.Oblique ? "cl-afa-tra" : "cr-afa-tra";
        float side = numeric.TryGetValue(sideKey, out float sideValue) && float.IsFinite(sideValue)
            ? sideValue
            : 0f;
        float back = numeric.TryGetValue(backKey, out float backValue) && float.IsFinite(backValue)
            ? backValue
            : 0f;
        float envelopeX = definition.ShapeKind switch
        {
            ModuleShapeKind.BlindCornerLeft => MathF.Min(0f, side),
            ModuleShapeKind.BlindCornerRight => MathF.Max(0f, -side),
            _ => MathF.Min(0f, side)
        };
        float envelopeZ = MathF.Min(0f, back);

        if (MathF.Abs(envelopeX) < 0.001f && MathF.Abs(envelopeZ) < 0.001f)
            return nominal;

        Vector3 shiftedOrigin = ModulePlacementService.TransformLocalPoint(
            new Vector3(envelopeX, 0f, envelopeZ), position, rotationYDegrees);
        var shifted = ModulePlacementService.ComputeBounds(
            shiftedOrigin, width, height, depth, rotationYDegrees);
        return (
            Vector3.ComponentMin(nominal.Min, shifted.Min),
            Vector3.ComponentMax(nominal.Max, shifted.Max));
    }
}
