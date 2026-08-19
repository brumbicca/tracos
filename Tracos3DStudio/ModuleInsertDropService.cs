using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ModuleInsertDropService
{
    public static bool TryInsertFromScreen(
        Project project,
        string definitionId,
        double mouseX,
        double mouseY,
        double viewportWidth,
        double viewportHeight,
        Matrix4 view,
        Matrix4 projection,
        IReadOnlyList<WallPickTarget> pickTargets,
        bool collisionEnabled,
        bool ignoreCollision,
        DimensionConfiguratorSettings dimensionSettings,
        out ModuleInstance? instance,
        out string? error)
    {
        instance = null;
        error = null;

        if (!ModuleCatalog.TryGet(definitionId, out ModuleDefinition? definition) || definition == null)
        {
            error = "Módulo inválido.";
            return false;
        }

        if (project.Room.Walls.Count == 0)
        {
            error = "Desenhe paredes antes de inserir módulos.";
            return false;
        }

        if (pickTargets.Count == 0)
        {
            error = "Solte na face interna da parede.";
            return false;
        }

        var (insertWidth, insertHeight, insertDepth) = DimensionConfiguratorService.ResolveInsertionDimensions(
            definition,
            dimensionSettings);

        ModulePlacementResult? placement = ModulePlacementService.TryComputeFromScreenRay(
            mouseX,
            mouseY,
            viewportWidth,
            viewportHeight,
            view,
            projection,
            project.Room.Walls,
            pickTargets,
            definition,
            insertWidth,
            insertDepth,
            insertHeight);

        if (placement == null || !placement.Value.SnappedToWall)
        {
            error = "Solte na face interna da parede.";
            return false;
        }

        ModulePlacementResult resolved = placement.Value;

        if (!ignoreCollision &&
            collisionEnabled &&
            ModuleCollisionService.WouldCollide(
                resolved.Position,
                insertWidth,
                insertHeight,
                insertDepth,
                resolved.RotationYDegrees,
                project.Modules,
                candidateWallId: resolved.WallId,
                distanceAlongWall: resolved.DistanceAlongWall,
                candidateDefinition: definition,
                dimensionSettings: dimensionSettings))
        {
            error = "Colisão com outro módulo.";
            return false;
        }

        instance = project.AddModule(definitionId, resolved.Position);
        instance.SetDimensions(insertWidth, insertHeight, insertDepth, definition,
            dimensionSettings, respectCatalogLimits: false);

        instance.ApplyPlacement(
            resolved.Position,
            resolved.RotationYDegrees,
            definition,
            resolved.WallId,
            resolved.DistanceAlongWall,
            dimensionSettings);

        DimensionConfiguratorService.EnsureProjectSettings(project);
        if (project.Metadata.DimensionSettings == null)
            project.Metadata.DimensionSettings = dimensionSettings.Clone();

        return true;
    }
}
