using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Desenho de paredes em modo draft (fantasma confirmado + segmento em preview).
/// </summary>
public static class WallDraftViewportRenderer
{
    public static void DrawConfirmedGhosts(IReadOnlyList<WallSegment> walls)
    {
        if (walls.Count == 0)
            return;

        var visuals = WallVisualBuilder.BuildWithCorners(walls);

        foreach (var visual in visuals)
        {
            var wall = visual.Wall;
            var inner = WallInnerFaceService.GetInnerFace(wall, walls);

            RenderEngine.BeginLineBatch();
            RenderEngine.Color3(0.75f, 0.75f, 0.75f);
            RenderEngine.Line(wall.Start, wall.End, 88f);
            RenderEngine.EndLineBatch(2f, depthTest: false);

            RenderEngine.Color3(0f, 0f, 0f);
            DrawDashedLine(inner.InnerStart, inner.InnerEnd, 95f, 120f, 70f);

            RenderEngine.BeginLineBatch();
            RenderEngine.Color3(0.35f, 0.35f, 0.35f);
            RenderEngine.Line(inner.InnerStart, wall.Start, 94f);
            RenderEngine.Line(inner.InnerEnd, wall.End, 94f);
            RenderEngine.EndLineBatch(1f, depthTest: false);

            WallViewportRenderer.DrawSegmentSolid(
                visual,
                faceSelected: false,
                groupSelected: false,
                xRay: true,
                insertionHighlight: false);
        }
    }

    public static void DrawPreviewSegment(
        WallDraft draft,
        Vector2 refStart,
        Vector2 refEnd,
        WallInnerFaceGeometry innerFace,
        bool showCloseMarker)
    {
        if ((refEnd - refStart).LengthSquared < 1f)
            return;

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

        RenderEngine.BeginLineBatch();
        RenderEngine.Color3(1f, 1f, 1f);
        RenderEngine.Line(axisStart, axisEnd, 90f);
        RenderEngine.EndLineBatch(3f, depthTest: false);

        if (innerFace.Length > 0.5f)
        {
            RenderEngine.Color3(0f, 0f, 0f);
            DrawDashedLine(innerFace.InnerStart, innerFace.InnerEnd, 95f, 120f, 70f);
        }

        RenderEngine.BeginLineBatch();
        RenderEngine.Color3(0.35f, 0.35f, 0.35f);
        RenderEngine.Line(refStart, axisStart, 94f);
        RenderEngine.Line(refEnd, axisEnd, 94f);
        RenderEngine.EndLineBatch(1f, depthTest: false);

        if (showCloseMarker)
        {
            Vector2 firstPoint = draft.Points[0];
            RenderEngine.Color3(0f, 0.75f, 0f);
            RenderEngine.BeginTriangleBatch();
            RenderEngine.PointMarker(new Vector3(firstPoint.X, 130f, firstPoint.Y), 24f);
            RenderEngine.EndTriangleBatch(depthTest: false);
        }
    }

    public static bool TryGetPreviewReferenceFace(
        WallDraft draft,
        Vector2 refStart,
        Vector2 refEnd,
        out WallInnerFaceGeometry referenceFace)
    {
        referenceFace = default;

        if (draft.Points.Count == 0)
            return false;

        if (!TryBuildPreviewWall(draft, refStart, refEnd, out var previewWall, out var allWalls))
            return false;

        referenceFace = WallInnerFaceService.GetReferenceFace(previewWall, allWalls);
        return referenceFace.Length > 0.5f;
    }

    public static bool TryGetPreviewInnerFace(
        WallDraft draft,
        Vector2 refStart,
        Vector2 refEnd,
        out WallInnerFaceGeometry innerFace)
    {
        innerFace = default;

        if (draft.Points.Count == 0)
            return false;

        if (!TryBuildPreviewWall(draft, refStart, refEnd, out var previewWall, out var allWalls))
            return false;

        innerFace = WallInnerFaceService.GetInnerFace(previewWall, allWalls);
        return innerFace.Length > 0.5f;
    }

    public static void DrawDashedLine(Vector2 start, Vector2 end, float y, float dashLength, float gapLength)
    {
        Vector2 delta = end - start;
        float totalLength = delta.Length;

        if (totalLength < 0.001f)
            return;

        Vector2 direction = Vector2.Normalize(delta);
        float cursor = 0f;

        RenderEngine.BeginLineBatch();

        while (cursor < totalLength)
        {
            float a = cursor;
            float b = MathF.Min(cursor + dashLength, totalLength);

            Vector2 p1 = start + direction * a;
            Vector2 p2 = start + direction * b;

            RenderEngine.Line(p1, p2, y);

            cursor += dashLength + gapLength;
        }

        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    private static bool TryBuildPreviewWall(
        WallDraft draft,
        Vector2 refStart,
        Vector2 refEnd,
        out WallSegment previewWall,
        out List<WallSegment> allWalls)
    {
        previewWall = null!;
        allWalls = new List<WallSegment>();

        var confirmed = draft.BuildWalls();
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

        previewWall = new WallSegment(
            axisStart,
            axisEnd,
            draft.Thickness,
            draft.Height,
            draft.Orientation)
        {
            MeasureSide = draft.MeasureSide
        };

        allWalls = new List<WallSegment>(confirmed) { previewWall };
        return true;
    }
}
