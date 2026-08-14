using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class WallAutomaticDimensionService
{
    public const float OffsetMm = 280f;
    public const float LabelHeightMm = 150f;

    public static IReadOnlyList<WallAutomaticDimension> BuildForWalls(IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return Array.Empty<WallAutomaticDimension>();

        var result = new List<WallAutomaticDimension>(walls.Count);

        foreach (var wall in walls)
        {
            var face = WallInnerFaceService.GetReferenceFace(wall, walls);
            if (face.Length < 1f)
                continue;

            Vector2 inward = face.InteriorNormal;
            if (inward.LengthSquared < 0.01f)
                inward = wall.LeftNormal;

            Vector2 offset = inward * OffsetMm;
            Vector2 dimStart = face.InnerStart + offset;
            Vector2 dimEnd = face.InnerEnd + offset;
            Vector2 labelFloor = (dimStart + dimEnd) * 0.5f;

            result.Add(new WallAutomaticDimension
            {
                FaceStart = face.InnerStart,
                FaceEnd = face.InnerEnd,
                DimStart = dimStart,
                DimEnd = dimEnd,
                LengthMm = WallInnerFaceService.GetDisplayReferenceLength(wall, walls),
                LabelWorldPosition = new Vector3(labelFloor.X, LabelHeightMm, labelFloor.Y)
            });
        }

        return result;
    }

    public static List<WallSegment> BuildDraftWallsIncludingPreview(
        WallDraft draft,
        Vector2 previewPoint,
        bool hasPreview)
    {
        var walls = draft.BuildWalls();

        if (!hasPreview || draft.Points.Count == 0)
            return walls;

        Vector2 refStart = draft.Points[^1];
        Vector2 refEnd = previewPoint;

        if ((refEnd - refStart).LengthSquared < 1f)
            return walls;

        var path = new List<Vector2>(draft.Points);
        if (!Geometry2D.AlmostEqual(refEnd, refStart, 1f))
            path.Add(refEnd);

        WallInnerFaceService.TryGetInteriorOnLeft(path, closed: false, out bool interiorOnLeft);

        var (axisStart, axisEnd) = WallInnerFaceService.ReferenceSegmentToAxis(
            refStart,
            refEnd,
            draft.Thickness,
            interiorOnLeft,
            draft.MeasureSide);

        var previewWall = new WallSegment(
            axisStart,
            axisEnd,
            draft.Thickness,
            draft.Height,
            draft.Orientation)
        {
            MeasureSide = draft.MeasureSide
        };

        var all = new List<WallSegment>(walls) { previewWall };
        return all;
    }
}
