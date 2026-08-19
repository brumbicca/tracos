using OpenTK.Mathematics;

namespace Tracos3DStudio;

public readonly struct ModulePlacementResult
{
    public Vector3 Position { get; init; }

    public float RotationYDegrees { get; init; }

    public bool SnappedToWall { get; init; }

    public Guid? WallId { get; init; }

    public float DistanceAlongWall { get; init; }
}

public enum ModuleCotaAxis
{
    Anterior,
    Posterior,
    Inferior,
    Superior
}

public static class ModulePlacementService
{
    public const float GridStep = 100f;
    public const float WallEdgeMargin = 0f;

    public static ModulePlacementResult? TryComputeFromScreenRay(
        double mouseX,
        double mouseY,
        double viewportWidth,
        double viewportHeight,
        Matrix4 view,
        Matrix4 projection,
        IReadOnlyList<WallSegment> walls,
        IReadOnlyList<WallPickTarget> pickTargets,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleDepth,
        float moduleHeight = 0f,
        Guid? restrictToWallId = null,
        bool snapMountY = true,
        bool snapAlongWall = true)
    {
        if (walls.Count == 0 || pickTargets.Count == 0)
            return null;

        if (!Geometry3D.TryCreateWorldRay(
                mouseX, mouseY, viewportWidth, viewportHeight, view, projection,
                out Vector3 origin, out Vector3 direction))
            return null;

        if (!WallPickService.TryPickModuleInsertionFace(
                origin, direction, pickTargets,
                out Guid wallId, out float distanceAlong, out _, out Vector3 hitPoint))
            return null;

        if (restrictToWallId.HasValue && wallId != restrictToWallId.Value)
            return null;

        var wall = FindWall(walls, wallId);

        if (wall == null)
            return null;

        Vector2 hitFloor = Geometry3D.HitPointToFloor(hitPoint);
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float distanceAlongInner = innerFace.DistanceFromInnerStart(hitFloor);

        distanceAlongInner = snapAlongWall
            ? SnapAlongWall(distanceAlongInner, GridStep)
            : distanceAlongInner;

        float heightForClamp = moduleHeight > 0f ? moduleHeight : definition.DefaultHeight;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        float relativeY = hitPoint.Y - wall.FloorOffset;
        float mountY = snapMountY
            ? wall.FloorOffset + SnapAlongWall(relativeY, GridStep)
            : hitPoint.Y;
        mountY = Math.Clamp(mountY, wall.FloorOffset, MathF.Max(wall.FloorOffset, wallTop - heightForClamp));

        return PlaceOnInsertionFace(
            wall,
            walls,
            definition,
            moduleWidth,
            moduleDepth,
            distanceAlongInner,
            innerFace.InteriorNormal,
            mountYOverride: mountY,
            moduleHeight: moduleHeight);
    }

    public static ModulePlacementResult? TryRepositionAttachedModuleFromScreenRay(
        ModuleInstance module,
        ModuleDefinition definition,
        double mouseX,
        double mouseY,
        double viewportWidth,
        double viewportHeight,
        Matrix4 view,
        Matrix4 projection,
        IReadOnlyList<WallSegment> walls,
        IReadOnlyList<WallPickTarget> pickTargets)
    {
        if (!module.AttachedWallId.HasValue)
            return null;

        return TryComputeFromScreenRay(
            mouseX,
            mouseY,
            viewportWidth,
            viewportHeight,
            view,
            projection,
            walls,
            pickTargets,
            definition,
            module.Width,
            module.Depth,
            module.Height,
            module.AttachedWallId);
    }

    public static ModulePlacementResult Compute(
        Vector2 floorPoint,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleDepth)
    {
        Vector2 snapped = SnapToGrid(floorPoint, GridStep);

        if (walls.Count == 0)
            return PlaceFree(snapped, definition, moduleWidth, moduleDepth);

        if (!WallPickService.TryPickFloor(snapped, walls, out Guid wallId, out float distanceAlong))
            return PlaceFree(snapped, definition, moduleWidth, moduleDepth);

        var wall = FindWall(walls, wallId);

        if (wall == null)
            return PlaceFree(snapped, definition, moduleWidth, moduleDepth);

        return PlaceAgainstWall(snapped, wall, walls, definition, moduleWidth, moduleDepth, distanceAlong);
    }

    public static ModulePlacementResult PlaceFree(
        Vector2 floorPoint,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleDepth)
    {
        float mountY = definition.IsDecorativePanel || !definition.IsWallMounted ? 0f : 1400f;

        return new ModulePlacementResult
        {
            Position = new Vector3(
                floorPoint.X - moduleWidth * 0.5f,
                mountY,
                floorPoint.Y - moduleDepth * 0.5f),
            RotationYDegrees = 0f,
            SnappedToWall = false
        };
    }

    public static ModulePlacementResult PlaceAgainstWall(
        Vector2 floorPoint,
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleDepth,
        float distanceAlongAxis)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float distanceAlongInner = innerFace.DistanceFromInnerStart(
            wall.Start + wall.Direction * Math.Clamp(distanceAlongAxis, 0f, wall.Length));

        Vector2 interiorNormal = innerFace.InteriorNormal;
        return PlaceOnInsertionFace(
            wall,
            walls,
            definition,
            moduleWidth,
            moduleDepth,
            distanceAlongInner,
            interiorNormal);
    }

    public static ModulePlacementResult PlaceOnInsertionFace(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleDepth,
        float distanceAlongInner,
        Vector2 interiorNormal,
        float? mountYOverride = null,
        float moduleHeight = 0f,
        bool clampToWallFace = true)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float halfWidth = moduleWidth * 0.5f;
        float minCenter = WallEdgeMargin + halfWidth;
        float maxCenter = Math.Max(minCenter, innerFace.Length - WallEdgeMargin - halfWidth);
        float centerAlong = clampToWallFace
            ? Math.Clamp(distanceAlongInner, minCenter, maxCenter)
            : distanceAlongInner;

        // A frente do módulo (eixo local Z) deve ser PERPENDICULAR à parede e apontar
        // para o interior. A interiorNormal (direção ao centro do ambiente) pode ser
        // diagonal em ambientes em L; usá-la direto deixaria o módulo fora de esquadro.
        // Por isso derivamos a perpendicular exata da face e escolhemos o lado interior.
        Vector2 faceDir = innerFace.Direction.LengthSquared > 1e-6f
            ? Vector2.Normalize(innerFace.Direction)
            : wall.Direction;
        Vector2 perp = new(-faceDir.Y, faceDir.X);
        Vector2 interiorHint = interiorNormal.LengthSquared > 1e-6f
            ? interiorNormal
            : innerFace.InteriorNormal;

        if (Vector2.Dot(perp, interiorHint) < 0f)
            perp = -perp;

        Vector2 front = perp;

        // Eixo de largura do módulo (local X). No mesh, local Z = local X girado +90°,
        // então widthAxis = (front.Y, -front.X) fica paralelo à face da parede.
        Vector2 widthAxis = new(front.Y, -front.X);

        // Centro do módulo sobre a face interna, no ponto do clique; recuar meia largura
        // pelo eixo de largura fornece o canto traseiro-esquerdo (origem local do mesh).
        Vector2 faceCenter = clampToWallFace
            ? innerFace.PointAtDistance(centerAlong)
            : innerFace.InnerStart + innerFace.Direction * centerAlong;
        Vector2 backLeft = faceCenter - widthAxis * halfWidth;

        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        float defaultMountY = definition.IsDecorativePanel
            ? wall.FloorOffset
            : definition.IsWallMounted ? 1400f : wall.FloorOffset;
        float heightForClamp = moduleHeight > 0f ? moduleHeight : definition.DefaultHeight;
        float mountY = mountYOverride ?? defaultMountY;
        if (clampToWallFace)
            mountY = Math.Clamp(mountY, wall.FloorOffset, MathF.Max(wall.FloorOffset, wallTop - heightForClamp));
        float rotationY = MathHelper.RadiansToDegrees(MathF.Atan2(front.X, front.Y));

        return new ModulePlacementResult
        {
            Position = new Vector3(backLeft.X, mountY, backLeft.Y),
            RotationYDegrees = rotationY,
            SnappedToWall = true,
            WallId = wall.Id,
            DistanceAlongWall = centerAlong
        };
    }

    public static Vector2 InteriorNormalFromRotation(float rotationYDegrees)
    {
        float radians = MathHelper.DegreesToRadians(rotationYDegrees);
        return new Vector2(MathF.Sin(radians), MathF.Cos(radians));
    }

    public static bool TryApplyWallCota(
        ModuleInstance module,
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition,
        ModuleCotaAxis axis,
        float value,
        out string? error)
    {
        error = null;

        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float halfWidth = module.Width * 0.5f;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);

        float centerAlong = module.DistanceAlongWall;
        // Preserva a altura atual por padrão: editar uma cota horizontal (Anterior/Posterior)
        // NÃO deve derrubar o módulo para o piso, e editar uma vertical não muda a horizontal.
        float mountYOverride = module.Position.Y;

        switch (axis)
        {
            case ModuleCotaAxis.Anterior:
                centerAlong = value + halfWidth;
                break;
            case ModuleCotaAxis.Posterior:
                centerAlong = innerFace.Length - value - halfWidth;
                break;
            case ModuleCotaAxis.Inferior:
                mountYOverride = wall.FloorOffset + value;
                break;
            case ModuleCotaAxis.Superior:
                mountYOverride = wallTop - value - module.Height;
                break;
        }

        Vector2 interiorNormal = InteriorNormalFromRotation(module.RotationYDegrees);
        var placement = PlaceOnInsertionFace(
            wall,
            walls,
            definition,
            module.Width,
            module.Depth,
            centerAlong,
            interiorNormal,
            mountYOverride,
            module.Height,
            clampToWallFace: false);

        module.ApplyPlacement(
            placement.Position,
            placement.RotationYDegrees,
            definition,
            placement.WallId,
            placement.DistanceAlongWall);

        return true;
    }

    public static Vector2 ComputeBackCornerOnInnerFace(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        float leftEdgeAlongInner,
        Vector2 interiorNormal)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        return innerFace.PointAtDistance(leftEdgeAlongInner);
    }

    public readonly struct ModuleWallCotas
    {
        public float Anterior { get; init; }

        public float Posterior { get; init; }

        public float Inferior { get; init; }

        public float Superior { get; init; }
    }

    public static ModuleWallCotas? TryComputeWallCotas(
        ModuleInstance module,
        WallSegment wall,
        IReadOnlyList<WallSegment> walls)
    {
        if (!module.AttachedWallId.HasValue || module.AttachedWallId.Value != wall.Id)
            return null;

        return ComputeDisplayWallCotas(module, wall, walls);
    }

    /// <summary>
    /// Cotas do módulo em relação a uma parede, para exibição/edição. Funciona mesmo
    /// quando o módulo ainda não está vinculado (AttachedWallId ausente/órfão): nesse
    /// caso a posição ao longo da face é projetada a partir do centro do módulo.
    /// </summary>
    public static ModuleWallCotas ComputeDisplayWallCotas(
        ModuleInstance module,
        WallSegment wall,
        IReadOnlyList<WallSegment> walls)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float centerAlong = module.AttachedWallId == wall.Id
            ? module.DistanceAlongWall
            : innerFace.DistanceFromInnerStart(ModuleCenterXZ(module));
        float halfWidth = module.Width * 0.5f;
        float leftAlong = centerAlong - halfWidth;
        float rightAlong = centerAlong + halfWidth;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        float moduleBottom = module.Position.Y;
        float moduleTop = moduleBottom + module.Height;

        return new ModuleWallCotas
        {
            Anterior = leftAlong,
            Posterior = innerFace.Length - rightAlong,
            Inferior = moduleBottom - wall.FloorOffset,
            Superior = wallTop - moduleTop
        };
    }

    /// <summary>
    /// Parede em que o módulo está "de costas" — referência para as cotas. Usa a parede
    /// vinculada quando válida; senão, a de face interna mais próxima do centro do módulo.
    /// Retorna null apenas quando não há paredes no ambiente.
    /// </summary>
    public static WallSegment? FindBackingWall(
        ModuleInstance module,
        IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return null;

        if (module.AttachedWallId.HasValue)
        {
            foreach (var wall in walls)
            {
                if (wall.Id == module.AttachedWallId.Value)
                    return wall;
            }
        }

        Vector2 center = ModuleCenterXZ(module);
        WallSegment? best = null;
        float bestDist = float.MaxValue;

        foreach (var wall in walls)
        {
            var face = WallInnerFaceService.GetInnerFace(wall, walls);
            float along = face.DistanceFromInnerStart(center);
            float dist = (center - face.PointAtDistance(along)).LengthSquared;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall;
            }
        }

        return best;
    }

    /// <summary>
    /// Vincula o módulo à parede preservando o ponto ao longo da face e a altura atual,
    /// deixando-o em esquadro. Necessário antes de editar cotas de um módulo sem vínculo.
    /// </summary>
    public static void AttachModuleToWall(
        ModuleInstance module,
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float centerAlong = innerFace.DistanceFromInnerStart(ModuleCenterXZ(module));

        var placement = PlaceOnInsertionFace(
            wall,
            walls,
            definition,
            module.Width,
            module.Depth,
            centerAlong,
            innerFace.InteriorNormal,
            module.Position.Y,
            module.Height);

        module.ApplyPlacement(
            placement.Position,
            placement.RotationYDegrees,
            definition,
            placement.WallId,
            placement.DistanceAlongWall);
    }

    private static Vector2 ModuleCenterXZ(ModuleInstance module)
    {
        var (min, max) = module.GetBounds();
        return new Vector2((min.X + max.X) * 0.5f, (min.Z + max.Z) * 0.5f);
    }

    public static ModuleWallCotas ComputeWallCotasFromPlacement(
        WallSegment wall,
        IReadOnlyList<WallSegment> walls,
        ModuleDefinition definition,
        float moduleWidth,
        float moduleHeight,
        ModulePlacementResult placement)
    {
        var innerFace = WallInnerFaceService.GetInnerFace(wall, walls);
        float halfWidth = moduleWidth * 0.5f;
        float leftAlong = placement.DistanceAlongWall - halfWidth;
        float rightAlong = placement.DistanceAlongWall + halfWidth;
        float wallTop = wall.FloorOffset + MathF.Max(wall.HeightStart, wall.HeightEnd);
        float moduleBottom = placement.Position.Y;
        float moduleTop = moduleBottom + moduleHeight;

        return new ModuleWallCotas
        {
            Anterior = leftAlong,
            Posterior = innerFace.Length - rightAlong,
            Inferior = moduleBottom - wall.FloorOffset,
            Superior = wallTop - moduleTop
        };
    }

    private static float SnapAlongWall(float distanceAlong, float step)
    {
        if (step <= 0f)
            return distanceAlong;

        return MathF.Round(distanceAlong / step) * step;
    }

    public static Vector2 ComputeInteriorNormal(WallSegment wall, Vector2 floorPoint, float distanceAlong)
    {
        Vector2 wallDir = wall.Direction;
        Vector2 centerPoint = wall.GetPointAtDistance(distanceAlong);
        Vector2 toClick = floorPoint - centerPoint;

        float along = Vector2.Dot(toClick, wallDir);
        Vector2 perpendicular = toClick - wallDir * along;

        if (perpendicular.LengthSquared > 1f)
            return Vector2.Normalize(perpendicular);

        return wall.LeftNormal;
    }

    public static Vector3 TransformLocalPoint(Vector3 local, Vector3 position, float rotationYDegrees)
    {
        float radians = MathHelper.DegreesToRadians(rotationYDegrees);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        float rotatedX = local.X * cos + local.Z * sin;
        float rotatedZ = -local.X * sin + local.Z * cos;

        return new Vector3(
            position.X + rotatedX,
            position.Y + local.Y,
            position.Z + rotatedZ);
    }

    /// <summary>Converte um ponto do mundo de volta ao espaço local do módulo (inverso de <see cref="TransformLocalPoint"/>).</summary>
    public static Vector3 InverseTransformPoint(Vector3 world, Vector3 position, float rotationYDegrees)
    {
        float radians = MathHelper.DegreesToRadians(rotationYDegrees);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        float dx = world.X - position.X;
        float dz = world.Z - position.Z;

        // Inverso da rotação aplicada em TransformLocalPoint.
        float localX = dx * cos - dz * sin;
        float localZ = dx * sin + dz * cos;

        return new Vector3(localX, world.Y - position.Y, localZ);
    }

    public static (Vector3 Min, Vector3 Max) ComputeBounds(
        Vector3 position,
        float width,
        float height,
        float depth,
        float rotationYDegrees)
    {
        ReadOnlySpan<Vector3> corners =
        [
            Vector3.Zero,
            new(width, 0, 0),
            new(width, 0, depth),
            new(0, 0, depth),
            new(0, height, 0),
            new(width, height, 0),
            new(width, height, depth),
            new(0, height, depth)
        ];

        Vector3 min = TransformLocalPoint(corners[0], position, rotationYDegrees);
        Vector3 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 world = TransformLocalPoint(corners[i], position, rotationYDegrees);
            min = Vector3.ComponentMin(min, world);
            max = Vector3.ComponentMax(max, world);
        }

        return (min, max);
    }

    private static Vector2 SnapToGrid(Vector2 point, float step)
    {
        if (step <= 0f)
            return point;

        return new Vector2(
            MathF.Round(point.X / step) * step,
            MathF.Round(point.Y / step) * step);
    }

    private static WallSegment? FindWall(IReadOnlyList<WallSegment> walls, Guid wallId)
    {
        foreach (var wall in walls)
        {
            if (wall.Id == wallId)
                return wall;
        }

        return null;
    }
}
