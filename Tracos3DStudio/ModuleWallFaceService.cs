namespace Tracos3DStudio;

public readonly struct WallFaceRectangle
{
    public float AlongStart { get; init; }

    public float AlongEnd { get; init; }

    public float BottomY { get; init; }

    public float TopY { get; init; }
}

public static class ModuleWallFaceService
{
    /// <summary>Encaixe lateral (borda com borda ao longo da parede).</summary>
    public const float HorizontalEdgeSnapDistanceMm = 80f;

    /// <summary>Faixa magnética para alinhar topo com topo.</summary>
    public const float VerticalTopSnapDistanceMm = 350f;

    /// <summary>Faixa magnética para alinhar base com base (maior — piso/vizinho).</summary>
    public const float VerticalBottomSnapDistanceMm = 480f;

    /// <summary>Distância máxima entre módulos na horizontal para ainda atrair na vertical.</summary>
    public const float VerticalSnapMaxAlongGapMm = 120f;

    private const float VerticalMoveThresholdMm = 0.5f;
    private const float LateralTouchToleranceMm = 2f;

    public static WallFaceRectangle GetWallFaceRectangle(ModuleInstance module)
    {
        float halfWidth = module.Width * 0.5f;

        return new WallFaceRectangle
        {
            AlongStart = module.DistanceAlongWall - halfWidth,
            AlongEnd = module.DistanceAlongWall + halfWidth,
            BottomY = module.Position.Y,
            TopY = module.Position.Y + module.Height
        };
    }

    public static WallFaceRectangle GetWallFaceRectangle(
        float distanceAlongCenter,
        float width,
        float mountY,
        float height)
    {
        float halfWidth = width * 0.5f;

        return new WallFaceRectangle
        {
            AlongStart = distanceAlongCenter - halfWidth,
            AlongEnd = distanceAlongCenter + halfWidth,
            BottomY = mountY,
            TopY = mountY + height
        };
    }

    public static bool SameMountBand(ModuleDefinition a, ModuleDefinition b) =>
        a.IsWallMounted == b.IsWallMounted;

    public static bool OverlapsOnWallFace(
        WallFaceRectangle a,
        WallFaceRectangle b,
        float minPenetrationMm = ModuleCollisionService.MinPenetrationMm)
    {
        return a.AlongStart < b.AlongEnd - minPenetrationMm && a.AlongEnd > b.AlongStart + minPenetrationMm &&
               a.BottomY < b.TopY - minPenetrationMm && a.TopY > b.BottomY + minPenetrationMm;
    }

    public static (float AlongCenter, float MountY) ApplyEdgeSnaps(
        float alongCenter,
        float mountY,
        float width,
        float height,
        Guid wallId,
        bool isWallMounted,
        Guid movingModuleId,
        IReadOnlyList<ModuleInstance> modules,
        float verticalMoveDelta,
        float verticalDirectionHint = 0f,
        bool lockHorizontal = false,
        float? wallFloorY = null)
    {
        float halfWidth = width * 0.5f;
        float movingLeft = alongCenter - halfWidth;
        float movingRight = alongCenter + halfWidth;
        float movingTop = mountY + height;
        float movingBottom = mountY;

        float bestAlong = alongCenter;
        float bestMountY = mountY;
        float bestAlongSnap = float.MaxValue;
        float bestVerticalSnap = float.MaxValue;

        float direction = ResolveVerticalDirection(verticalMoveDelta, verticalDirectionHint);

        bool movingUp = direction > VerticalMoveThresholdMm;
        bool movingDown = direction < -VerticalMoveThresholdMm;

        foreach (var other in modules)
        {
            if (other.Id == movingModuleId)
                continue;

            if (!other.AttachedWallId.HasValue || other.AttachedWallId.Value != wallId)
                continue;

            var otherDefinition = ModuleCatalog.GetRequired(other.DefinitionId);

            if (otherDefinition.IsWallMounted != isWallMounted)
                continue;

            var otherRect = GetWallFaceRectangle(other);

            if (!lockHorizontal && HasHorizontalProximity(movingLeft, movingRight, otherRect))
            {
                TrySnapAlongEdge(movingLeft, otherRect.AlongEnd, otherRect.AlongEnd + halfWidth, ref bestAlong, ref bestAlongSnap);
                TrySnapAlongEdge(movingRight, otherRect.AlongStart, otherRect.AlongStart - halfWidth, ref bestAlong, ref bestAlongSnap);
                TrySnapAlongEdge(movingLeft, otherRect.AlongStart, otherRect.AlongStart + halfWidth, ref bestAlong, ref bestAlongSnap);
                TrySnapAlongEdge(movingRight, otherRect.AlongEnd, otherRect.AlongEnd - halfWidth, ref bestAlong, ref bestAlongSnap);
            }

            if (!HasVerticalSnapProximity(movingLeft, movingRight, otherRect))
                continue;

            bool laterallyTouching = IsLaterallyTouching(movingLeft, movingRight, otherRect);

            if (movingUp || (!movingDown && !laterallyTouching))
            {
                TrySnapVerticalEdge(
                    movingTop,
                    otherRect.TopY,
                    otherRect.TopY - height,
                    mountY,
                    direction,
                    VerticalTopSnapDistanceMm,
                    snapKind: VerticalSnapKind.Top,
                    ref bestMountY,
                    ref bestVerticalSnap);
            }

            if (movingDown || (!movingUp && !laterallyTouching))
            {
                TrySnapVerticalEdge(
                    movingBottom,
                    otherRect.BottomY,
                    otherRect.BottomY,
                    mountY,
                    direction,
                    VerticalBottomSnapDistanceMm,
                    snapKind: VerticalSnapKind.Bottom,
                    ref bestMountY,
                    ref bestVerticalSnap);
            }

            if (laterallyTouching && !movingUp && !movingDown)
            {
                float distTop = MathF.Abs(movingTop - otherRect.TopY);
                float distBottom = MathF.Abs(movingBottom - otherRect.BottomY);

                if (distBottom <= distTop)
                {
                    TrySnapVerticalEdge(
                        movingBottom,
                        otherRect.BottomY,
                        otherRect.BottomY,
                        mountY,
                        direction,
                        VerticalBottomSnapDistanceMm,
                        VerticalSnapKind.Bottom,
                        ref bestMountY,
                        ref bestVerticalSnap);
                }
                else
                {
                    TrySnapVerticalEdge(
                        movingTop,
                        otherRect.TopY,
                        otherRect.TopY - height,
                        mountY,
                        direction,
                        VerticalTopSnapDistanceMm,
                        VerticalSnapKind.Top,
                        ref bestMountY,
                        ref bestVerticalSnap);
                }
            }
        }

        if (wallFloorY.HasValue && (movingDown || !movingUp))
        {
            TrySnapVerticalEdge(
                movingBottom,
                wallFloorY.Value,
                wallFloorY.Value,
                mountY,
                direction,
                VerticalBottomSnapDistanceMm,
                VerticalSnapKind.Bottom,
                ref bestMountY,
                ref bestVerticalSnap);
        }

        return (bestAlong, bestMountY);
    }

    private enum VerticalSnapKind
    {
        Top,
        Bottom
    }

    /// <summary>
    /// Prioriza mouse (Y invertido) quando o gesto é vertical; evita jitter do raio cancelar snap inferior.
    /// </summary>
    public static float ResolveVerticalDirection(float verticalMoveDelta, float verticalDirectionHint)
    {
        float screenDirection = -verticalDirectionHint;

        if (MathF.Abs(screenDirection) > 1f)
        {
            if (MathF.Abs(verticalMoveDelta) <= VerticalMoveThresholdMm)
                return screenDirection;

            if (verticalMoveDelta * screenDirection < 0f)
                return screenDirection;
        }

        if (MathF.Abs(verticalMoveDelta) > VerticalMoveThresholdMm)
            return verticalMoveDelta;

        return screenDirection;
    }

    private static bool IsLaterallyTouching(float movingLeft, float movingRight, WallFaceRectangle other)
    {
        float gap = movingLeft > other.AlongEnd
            ? movingLeft - other.AlongEnd
            : other.AlongStart - movingRight;

        return gap <= LateralTouchToleranceMm;
    }

    private static bool HasHorizontalProximity(float movingLeft, float movingRight, WallFaceRectangle other)
    {
        return movingRight >= other.AlongStart - HorizontalEdgeSnapDistanceMm &&
               movingLeft <= other.AlongEnd + HorizontalEdgeSnapDistanceMm;
    }

    private static bool HasVerticalSnapProximity(float movingLeft, float movingRight, WallFaceRectangle other)
    {
        if (movingRight >= other.AlongStart && movingLeft <= other.AlongEnd)
            return true;

        float gap = movingLeft > other.AlongEnd
            ? movingLeft - other.AlongEnd
            : other.AlongStart - movingRight;

        return gap <= VerticalSnapMaxAlongGapMm;
    }

    private static void TrySnapAlongEdge(
        float movingEdge,
        float targetEdge,
        float resultingCenter,
        ref float bestCenter,
        ref float bestDistance)
    {
        float distance = MathF.Abs(movingEdge - targetEdge);

        if (distance > HorizontalEdgeSnapDistanceMm || distance >= bestDistance)
            return;

        bestDistance = distance;
        bestCenter = resultingCenter;
    }

    private static void TrySnapVerticalEdge(
        float movingEdge,
        float targetEdge,
        float resultingMountY,
        float rawMountY,
        float verticalMoveDelta,
        float maxDistance,
        VerticalSnapKind snapKind,
        ref float bestMountY,
        ref float bestDistance)
    {
        float distance = MathF.Abs(movingEdge - targetEdge);

        if (distance > maxDistance || distance >= bestDistance)
            return;

        if (snapKind == VerticalSnapKind.Bottom &&
            verticalMoveDelta < -VerticalMoveThresholdMm &&
            resultingMountY > rawMountY + 0.5f)
            return;

        bestDistance = distance;
        bestMountY = resultingMountY;
    }
}
