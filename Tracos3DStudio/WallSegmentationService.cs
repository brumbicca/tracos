using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class WallSegmentationService
{
    public const float MinSegmentLengthMm = 300f;

    public static bool CanSplit(WallSegment wall, float splitDistanceAlong) =>
        wall.Length >= MinSegmentLengthMm * 2f &&
        splitDistanceAlong >= MinSegmentLengthMm &&
        splitDistanceAlong <= wall.Length - MinSegmentLengthMm &&
        !WouldSplitThroughOpening(wall, splitDistanceAlong);

    public static bool TrySplit(WallSegment wall, float splitDistanceAlong, out List<WallSegment> segments)
    {
        segments = new List<WallSegment>(2);

        if (!CanSplit(wall, splitDistanceAlong))
            return false;

        Vector2 splitPoint = wall.GetPointAtDistance(splitDistanceAlong);
        float heightAtSplit = wall.HeightAtDistance(splitDistanceAlong);

        var first = CreateSegment(wall.Start, splitPoint, wall, wall.HeightStart, heightAtSplit);
        var second = CreateSegment(splitPoint, wall.End, wall, heightAtSplit, wall.HeightEnd);

        DistributeOpenings(wall, splitDistanceAlong, first, second);

        segments.Add(first);
        segments.Add(second);
        return true;
    }

    public static void ReassignModulesAfterSplit(
        IReadOnlyList<ModuleInstance> modules,
        Guid originalWallId,
        Guid firstSegmentId,
        Guid secondSegmentId,
        float splitDistanceAlong)
    {
        foreach (var module in modules)
        {
            if (module.AttachedWallId != originalWallId)
                continue;

            if (module.DistanceAlongWall < splitDistanceAlong)
            {
                module.AttachedWallId = firstSegmentId;
                continue;
            }

            module.AttachedWallId = secondSegmentId;
            module.DistanceAlongWall -= splitDistanceAlong;
        }
    }

    private static bool WouldSplitThroughOpening(WallSegment wall, float splitDistanceAlong)
    {
        foreach (var opening in wall.Openings)
        {
            float half = opening.Width * 0.5f;
            float start = opening.DistanceFromStart - half;
            float end = opening.DistanceFromStart + half;

            if (splitDistanceAlong > start && splitDistanceAlong < end)
                return true;
        }

        return false;
    }

    private static void DistributeOpenings(
        WallSegment source,
        float splitDistanceAlong,
        WallSegment first,
        WallSegment second)
    {
        foreach (var opening in source.Openings)
        {
            if (opening.DistanceFromStart <= splitDistanceAlong)
            {
                first.Openings.Add(CloneOpening(opening));
                continue;
            }

            var moved = CloneOpening(opening);
            moved.DistanceFromStart -= splitDistanceAlong;
            second.Openings.Add(moved);
        }
    }

    private static WallOpening CloneOpening(WallOpening opening) => new()
    {
        Id = Guid.NewGuid(),
        Type = opening.Type,
        DistanceFromStart = opening.DistanceFromStart,
        Width = opening.Width,
        Height = opening.Height,
        SillHeight = opening.SillHeight,
        AutoCutWall = opening.AutoCutWall
    };

    private static WallSegment CreateSegment(
        Vector2 start,
        Vector2 end,
        WallSegment source,
        float heightStart,
        float heightEnd)
    {
        var segment = new WallSegment(start, end, source.Thickness, heightStart, source.Orientation)
        {
            HeightEnd = heightEnd,
            MeasureSide = source.MeasureSide,
            FloorOffset = source.FloorOffset,
            CotaAnterior = source.CotaAnterior,
            CotaPosterior = source.CotaPosterior,
            CotaInferior = source.CotaInferior,
            CotaSuperior = source.CotaSuperior,
            DrawBottomFace = source.DrawBottomFace,
            IsMovable = source.IsMovable,
            IsVisible = source.IsVisible,
            ChamferStartMm = start == source.Start ? source.ChamferStartMm : 0f,
            ChamferEndMm = end == source.End ? source.ChamferEndMm : 0f,
            FlechaMm = start == source.Start && end == source.End ? source.FlechaMm : 0f,
            ConstructionType = source.ConstructionType,
            LayerId = source.LayerId,
            CompartmentId = source.CompartmentId
        };

        return segment;
    }
}
