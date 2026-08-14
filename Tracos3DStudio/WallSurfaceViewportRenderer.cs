using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Desenho de faixas, regiões e overlays de edição na superfície das paredes.
/// </summary>
public static class WallSurfaceViewportRenderer
{
    private static readonly Vector4 BandLineColor = new(0.95f, 0.55f, 0.1f, 0.95f);
    private static readonly Vector4 RegionLineColor = new(0.2f, 0.75f, 0.95f, 0.95f);
    private static readonly Vector4 VerticalBandPreviewColor = new(0.95f, 0.55f, 0.1f, 0.95f);
    private static readonly Vector4 RegionPickPreviewColor = new(0.2f, 0.75f, 0.95f, 0.95f);
    private static readonly Vector4 BandDragPreviewColor = new(1f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 RegionDragPreviewColor = new(0.35f, 0.9f, 1f, 1f);
    private static readonly Vector4 RegionBaseLineColor = new(0.15f, 0.45f, 0.65f, 0.7f);
    private static readonly Vector4 RegionOffsetArrowColor = new(0.95f, 0.88f, 0.15f, 1f);
    private static readonly Vector4 RegionRotationHandleColor = new(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Vector4 RegionVerticalCutPreviewColor = new(0.95f, 0.1f, 0.1f, 1f);

    public const float RegionOffsetArrowSpacingMm = 80f;
    public const float RegionOffsetArrowPickToleranceMm = 100f;
    public const float RegionOffsetArrowSizeMm = 45f;

    public static VisualWallSegment? FindSegment(IReadOnlyList<WallSegment> walls, Guid wallId)
    {
        return WallVisualBuilder.BuildWithCorners(walls).FirstOrDefault(s => s.Wall.Id == wallId);
    }

    public static void DrawBandsAndRegions(VisualWallSegment segment, LayerFillMode fillMode = LayerFillMode.Default)
    {
        var wall = segment.Wall;
        bool drawMaterialFills = LayerFillModeCatalog.ShouldDrawSurfaceMaterials(fillMode);
        float floor = wall.FloorOffset;
        float length = wall.Length;

        RenderEngine.BeginTriangleBatch();

        float faceTopY = floor + MathF.Max(wall.HeightStart, wall.HeightEnd);

        if (drawMaterialFills)
        {
            if (!string.IsNullOrWhiteSpace(wall.InternalFaceMaterialId))
            {
                var internalFill = WallSurfaceMaterialCatalog.GetPreviewColor(wall.InternalFaceMaterialId);
                DrawFaceVerticalStrip(segment, true, 0f, length, floor, faceTopY, internalFill);
            }

            if (!string.IsNullOrWhiteSpace(wall.ExternalFaceMaterialId))
            {
                var externalFill = WallSurfaceMaterialCatalog.GetPreviewColor(wall.ExternalFaceMaterialId);
                DrawFaceVerticalStrip(segment, false, 0f, length, floor, faceTopY, externalFill);
            }

            foreach (var band in wall.Bands)
            {
                if (band.IsHorizontal)
                {
                    if (string.IsNullOrWhiteSpace(band.MaterialId))
                        continue;

                    float yBottom = floor + band.StartMm;
                    float yTop = floor + band.EndMm;
                    var fill = WallSurfaceMaterialCatalog.GetPreviewColor(band.MaterialId);
                    DrawFaceVerticalStrip(segment, true, 0f, length, yBottom, yTop, fill);
                }
                else if (!string.IsNullOrWhiteSpace(band.MaterialId))
                {
                    float yBottom = floor;
                    float yTop = floor + MathF.Max(wall.HeightStart, wall.HeightEnd);
                    var fill = WallSurfaceMaterialCatalog.GetPreviewColor(band.MaterialId);
                    DrawFaceVerticalStrip(segment, true, band.StartMm, band.EndMm, yBottom, yTop, fill);
                }
            }

            foreach (var region in wall.Regions)
            {
                if (string.IsNullOrWhiteSpace(region.MaterialId))
                    continue;

                var fill = WallSurfaceMaterialCatalog.GetPreviewColor(region.MaterialId);
                bool faceA = region.Face == FaceType.Internal;

                if (region.Shape == WallRegionShape.Circular)
                {
                    DrawCircleRegionFill(segment, region, faceA, floor, fill);
                }
                else if (region.Shape == WallRegionShape.Polygon)
                {
                    DrawPolygonRegionFill(segment, region, faceA, floor, fill);
                }
                else if (MathF.Abs(region.RotationDegrees) > 0.01f)
                {
                    DrawRotatedRectRegionFill(segment, region, faceA, floor, fill);
                }
                else
                {
                    var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, length, MathF.Max(wall.HeightStart, wall.HeightEnd));
                    DrawFaceVerticalStrip(segment, faceA, start, end, floor + bottom, floor + top, fill);
                }
            }
        }

        RenderEngine.EndTriangleBatch(blend: drawMaterialFills);

        RenderEngine.BeginLineBatch();

        foreach (var band in wall.Bands)
        {
            if (band.IsHorizontal)
            {
                float yBottom = floor + band.StartMm;
                float yTop = floor + band.EndMm;
                DrawHorizontalBandLine(segment.A1, segment.A2, length, yBottom, BandLineColor);
                DrawHorizontalBandLine(segment.A1, segment.A2, length, yTop, BandLineColor);
            }
            else
            {
                float yBottom = floor;
                float yTop = floor + MathF.Max(wall.HeightStart, wall.HeightEnd);
                DrawVerticalBandLine(segment.A1, segment.A2, length, band.StartMm, yBottom, yTop, BandLineColor);
                DrawVerticalBandLine(segment.A1, segment.A2, length, band.EndMm, yBottom, yTop, BandLineColor);
            }
        }

        foreach (var region in wall.Regions)
        {
            if (region.Shape == WallRegionShape.Circular)
            {
                DrawCircleRegionOutline(segment, region, floor, RegionLineColor);
            }
            else if (region.Shape == WallRegionShape.Polygon)
            {
                DrawPolygonRegionOutline(segment, region, floor, RegionLineColor);
            }
            else if (MathF.Abs(region.RotationDegrees) > 0.01f)
            {
                DrawRotatedRectRegionOutline(segment, region, floor, RegionLineColor);
            }
            else
            {
                float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
                bool hasEdgeOffset =
                    MathF.Abs(region.OffsetEdgeStartAlongMm) > 0.01f ||
                    MathF.Abs(region.OffsetEdgeEndAlongMm) > 0.01f ||
                    MathF.Abs(region.OffsetEdgeBottomMm) > 0.01f ||
                    MathF.Abs(region.OffsetEdgeTopMm) > 0.01f;

                if (hasEdgeOffset || MathF.Abs(region.OffsetMm) > 0.01f)
                {
                    var (bs, be, bb, bt) = WallRegionGeometry.GetBaseBounds(region);
                    DrawRegionOutline(segment, region.Face, bs, be, floor + bb, floor + bt, RegionBaseLineColor);
                }

                var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, length, wallTop);
                DrawRegionOutline(segment, region.Face, start, end, floor + bottom, floor + top, RegionLineColor);
            }
        }

        RenderEngine.EndLineBatch(2f, depthTest: true);
    }

    public static void DrawVerticalBandPickPreview(VisualWallSegment visual, float along1, float along2)
    {
        var wall = visual.Wall;
        float length = wall.Length;
        float start = MathF.Min(along1, along2);
        float end = MathF.Max(along1, along2);
        float floor = wall.FloorOffset;
        float yTop = floor + MathF.Max(wall.HeightStart, wall.HeightEnd);

        RenderEngine.BeginLineBatch();
        DrawVerticalBandLine(visual.A1, visual.A2, length, start, floor, yTop, VerticalBandPreviewColor);
        DrawVerticalBandLine(visual.A1, visual.A2, length, end, floor, yTop, VerticalBandPreviewColor);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawHorizontalBandPickPreview(VisualWallSegment visual, float height1, float height2)
    {
        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float length = wall.Length;
        float y1 = floor + height1;
        float y2 = floor + height2;

        RenderEngine.BeginLineBatch();
        DrawHorizontalBandLine(visual.A1, visual.A2, length, y1, VerticalBandPreviewColor);
        DrawHorizontalBandLine(visual.A1, visual.A2, length, y2, VerticalBandPreviewColor);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawPolygonRegionPickPreview(
        VisualWallSegment visual,
        FaceType face,
        IReadOnlyList<float> alongVertices,
        IReadOnlyList<float> heightVertices,
        float previewAlong,
        float previewHeight)
    {
        float floor = visual.Wall.FloorOffset;
        int n = alongVertices.Count;

        RenderEngine.BeginLineBatch();

        for (int i = 0; i < n - 1; i++)
        {
            DrawPolygonEdge(
                visual, face, floor,
                alongVertices[i], heightVertices[i],
                alongVertices[i + 1], heightVertices[i + 1],
                RegionPickPreviewColor);
        }

        if (n > 0)
        {
            DrawPolygonEdge(
                visual, face, floor,
                alongVertices[^1], heightVertices[^1],
                previewAlong, previewHeight,
                RegionPickPreviewColor);
        }

        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawRegionPickPreview(
        VisualWallSegment visual,
        FaceType face,
        float along1,
        float along2,
        float height1,
        float height2)
    {
        float startAlong = MathF.Min(along1, along2);
        float endAlong = MathF.Max(along1, along2);
        float bottom = MathF.Min(height1, height2);
        float top = MathF.Max(height1, height2);
        float floor = visual.Wall.FloorOffset;

        RenderEngine.BeginLineBatch();
        DrawRegionOutline(
            visual,
            face,
            startAlong,
            endAlong,
            floor + bottom,
            floor + top,
            RegionPickPreviewColor);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawBandDragPreview(
        VisualWallSegment visual,
        WallBand band,
        WallBandEdgeKind edge,
        float previewValue)
    {
        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float length = wall.Length;
        float yTop = floor + MathF.Max(wall.HeightStart, wall.HeightEnd);

        RenderEngine.BeginLineBatch();

        if (band.IsHorizontal)
        {
            float yBottom = edge == WallBandEdgeKind.Start
                ? floor + previewValue
                : floor + band.StartMm;
            float yTopBand = edge == WallBandEdgeKind.End
                ? floor + previewValue
                : floor + band.EndMm;
            DrawHorizontalBandLine(visual.A1, visual.A2, length, yBottom, BandDragPreviewColor);
            DrawHorizontalBandLine(visual.A1, visual.A2, length, yTopBand, BandDragPreviewColor);
        }
        else
        {
            float start = edge == WallBandEdgeKind.Start ? previewValue : band.StartMm;
            float end = edge == WallBandEdgeKind.End ? previewValue : band.EndMm;
            DrawVerticalBandLine(visual.A1, visual.A2, length, start, floor, yTop, BandDragPreviewColor);
            DrawVerticalBandLine(visual.A1, visual.A2, length, end, floor, yTop, BandDragPreviewColor);
        }

        RenderEngine.EndLineBatch(3f, depthTest: false);
    }

    public static void DrawCircleRegionPreview(
        VisualWallSegment visual,
        FaceType face,
        float centerAlong,
        float centerHeight,
        float radius)
    {
        float floor = visual.Wall.FloorOffset;
        var preview = new WallRegion
        {
            Shape = WallRegionShape.Circular,
            Face = face,
            CenterAlongMm = centerAlong,
            CenterHeightMm = centerHeight,
            RadiusMm = radius
        };

        RenderEngine.BeginLineBatch();
        DrawCircleRegionOutline(visual, preview, floor, RegionPickPreviewColor);
        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    public static void DrawRegionDragPreview(
        VisualWallSegment visual,
        WallRegion region,
        WallRegionEdgeKind edge,
        float previewValue)
    {
        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);

        RenderEngine.BeginLineBatch();

        if (region.Shape == WallRegionShape.Circular && edge == WallRegionEdgeKind.Radius)
        {
            var preview = new WallRegion
            {
                Shape = WallRegionShape.Circular,
                Face = region.Face,
                CenterAlongMm = region.CenterAlongMm,
                CenterHeightMm = region.CenterHeightMm,
                RadiusMm = previewValue,
                OffsetMm = region.OffsetMm
            };
            DrawCircleRegionOutline(visual, preview, floor, RegionDragPreviewColor);
        }
        else
        {
            float startAlong = region.StartAlongMm;
            float endAlong = region.EndAlongMm;
            float bottom = region.BottomMm;
            float top = region.TopMm;

            switch (edge)
            {
                case WallRegionEdgeKind.StartAlong:
                    startAlong = previewValue;
                    break;
                case WallRegionEdgeKind.EndAlong:
                    endAlong = previewValue;
                    break;
                case WallRegionEdgeKind.Bottom:
                    bottom = previewValue;
                    break;
                case WallRegionEdgeKind.Top:
                    top = previewValue;
                    break;
            }

            var (s, e, b, t) = WallRegionGeometry.GetEffectiveBounds(
                new WallRegion
                {
                    Shape = WallRegionShape.Rectangular,
                    StartAlongMm = startAlong,
                    EndAlongMm = endAlong,
                    BottomMm = bottom,
                    TopMm = top,
                    OffsetMm = region.OffsetMm,
                    OffsetEdgeStartAlongMm = region.OffsetEdgeStartAlongMm,
                    OffsetEdgeEndAlongMm = region.OffsetEdgeEndAlongMm,
                    OffsetEdgeBottomMm = region.OffsetEdgeBottomMm,
                    OffsetEdgeTopMm = region.OffsetEdgeTopMm
                },
                wall.Length,
                wallTop);

            DrawRegionOutline(visual, region.Face, s, e, floor + b, floor + t, RegionDragPreviewColor);
        }

        RenderEngine.EndLineBatch(3f, depthTest: false);
    }

    public static void DrawRegionBodyDragPreview(
        VisualWallSegment visual,
        WallRegion region,
        float deltaAlong,
        float deltaHeight)
    {
        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);

        RenderEngine.BeginLineBatch();

        if (region.Shape == WallRegionShape.Circular)
        {
            var preview = new WallRegion
            {
                Shape = WallRegionShape.Circular,
                Face = region.Face,
                CenterAlongMm = region.CenterAlongMm + deltaAlong,
                CenterHeightMm = region.CenterHeightMm + deltaHeight,
                RadiusMm = region.RadiusMm,
                OffsetMm = region.OffsetMm
            };
            DrawCircleRegionOutline(visual, preview, floor, RegionDragPreviewColor);
        }
        else if (region.Shape == WallRegionShape.Polygon && region.PolygonAlongMm.Count >= 3)
        {
            var preview = new WallRegion
            {
                Shape = WallRegionShape.Polygon,
                Face = region.Face,
                OffsetMm = region.OffsetMm
            };

            for (int i = 0; i < region.PolygonAlongMm.Count; i++)
            {
                preview.PolygonAlongMm.Add(region.PolygonAlongMm[i] + deltaAlong);
                preview.PolygonHeightMm.Add(region.PolygonHeightMm[i] + deltaHeight);
            }

            DrawPolygonRegionOutline(visual, preview, floor, RegionDragPreviewColor);
        }
        else
        {
            var preview = new WallRegion
            {
                Shape = WallRegionShape.Rectangular,
                Face = region.Face,
                StartAlongMm = region.StartAlongMm + deltaAlong,
                EndAlongMm = region.EndAlongMm + deltaAlong,
                BottomMm = region.BottomMm + deltaHeight,
                TopMm = region.TopMm + deltaHeight,
                OffsetMm = region.OffsetMm,
                OffsetEdgeStartAlongMm = region.OffsetEdgeStartAlongMm,
                OffsetEdgeEndAlongMm = region.OffsetEdgeEndAlongMm,
                OffsetEdgeBottomMm = region.OffsetEdgeBottomMm,
                OffsetEdgeTopMm = region.OffsetEdgeTopMm
            };

            var (s, e, b, t) = WallRegionGeometry.GetEffectiveBounds(preview, wall.Length, wallTop);
            DrawRegionOutline(visual, region.Face, s, e, floor + b, floor + t, RegionDragPreviewColor);
        }

        RenderEngine.EndLineBatch(3f, depthTest: false);
    }

    public static void DrawRegionOffsetArrows(VisualWallSegment visual, WallRegion region)
    {
        if (region.Shape != WallRegionShape.Rectangular)
            return;

        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        var (start, end, bottom, top) = WallRegionGeometry.GetEffectiveBounds(region, wall.Length, wallTop);
        float midAlong = (start + end) * 0.5f;
        float midH = (bottom + top) * 0.5f;
        float spacing = RegionOffsetArrowSpacingMm;

        RenderEngine.BeginLineBatch();

        DrawOffsetChevron(visual, region.Face, floor, start - spacing, midH, -1f, 0f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, start + spacing, midH, 1f, 0f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, end + spacing, midH, 1f, 0f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, end - spacing, midH, -1f, 0f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, midAlong, bottom - spacing, 0f, -1f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, midAlong, bottom + spacing, 0f, 1f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, midAlong, top + spacing, 0f, 1f, RegionOffsetArrowColor);
        DrawOffsetChevron(visual, region.Face, floor, midAlong, top - spacing, 0f, -1f, RegionOffsetArrowColor);

        RenderEngine.EndLineBatch(2.5f, depthTest: false);
    }

    private static void DrawOffsetChevron(
        VisualWallSegment segment,
        FaceType face,
        float floor,
        float along,
        float height,
        float dirAlong,
        float dirHeight,
        Vector4 color)
    {
        float size = RegionOffsetArrowSizeMm;
        float wing = size * 0.45f;
        float tipAlong = along + dirAlong * size * 0.5f;
        float tipHeight = height + dirHeight * size * 0.5f;
        float baseAlong = along - dirAlong * size * 0.5f;
        float baseHeight = height - dirHeight * size * 0.5f;

        float perpAlong = dirHeight;
        float perpHeight = -dirAlong;

        DrawPolygonEdge(segment, face, floor, tipAlong, tipHeight, baseAlong + perpAlong * wing, baseHeight + perpHeight * wing, color);
        DrawPolygonEdge(segment, face, floor, tipAlong, tipHeight, baseAlong - perpAlong * wing, baseHeight - perpHeight * wing, color);
    }

    public static void DrawRegionRotationHandle(VisualWallSegment visual, WallRegion region)
    {
        if (region.Shape == WallRegionShape.Circular)
            return;

        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        WallRegionGeometry.GetRotationHandlePosition(region, wall.Length, wallTop, out float hx, out float hy);
        float size = 55f;

        RenderEngine.BeginLineBatch();
        DrawPolygonEdge(visual, region.Face, floor, hx - size, hy - size, hx + size, hy - size, RegionRotationHandleColor);
        DrawPolygonEdge(visual, region.Face, floor, hx + size, hy - size, hx + size, hy + size, RegionRotationHandleColor);
        DrawPolygonEdge(visual, region.Face, floor, hx + size, hy + size, hx - size, hy + size, RegionRotationHandleColor);
        DrawPolygonEdge(visual, region.Face, floor, hx - size, hy + size, hx - size, hy - size, RegionRotationHandleColor);
        RenderEngine.EndLineBatch(3f, depthTest: false);

        RenderEngine.BeginTriangleBatch();
        Vector2 f1 = region.Face == FaceType.Internal ? visual.A1 : visual.B1;
        Vector2 f2 = region.Face == FaceType.Internal ? visual.A2 : visual.B2;
        float length = wall.Length;
        Vector3 v0 = ToFacePoint(f1, f2, length, floor, hx - size, hy - size);
        Vector3 v1 = ToFacePoint(f1, f2, length, floor, hx + size, hy - size);
        Vector3 v2 = ToFacePoint(f1, f2, length, floor, hx + size, hy + size);
        Vector3 v3 = ToFacePoint(f1, f2, length, floor, hx - size, hy + size);
        RenderEngine.Triangle(v0, v1, v2, RegionRotationHandleColor);
        RenderEngine.Triangle(v0, v2, v3, RegionRotationHandleColor);
        RenderEngine.EndTriangleBatch(blend: true);
    }

    public static void DrawRegionRotationPreview(
        VisualWallSegment visual,
        WallRegion region,
        float previewDeltaDegrees)
    {
        if (region.Shape == WallRegionShape.Circular || MathF.Abs(previewDeltaDegrees) < 0.01f)
            return;

        var preview = new WallRegion
        {
            Shape = region.Shape,
            Face = region.Face,
            StartAlongMm = region.StartAlongMm,
            EndAlongMm = region.EndAlongMm,
            BottomMm = region.BottomMm,
            TopMm = region.TopMm,
            RotationDegrees = region.RotationDegrees
        };

        preview.PolygonAlongMm.AddRange(region.PolygonAlongMm);
        preview.PolygonHeightMm.AddRange(region.PolygonHeightMm);
        WallRegionGeometry.ApplyRotationDelta(preview, previewDeltaDegrees);
        float floor = visual.Wall.FloorOffset;

        if (preview.Shape == WallRegionShape.Polygon && preview.PolygonAlongMm.Count >= 3)
            DrawPolygonRegionOutline(visual, preview, floor, RegionDragPreviewColor);
        else
            DrawRotatedRectRegionOutline(visual, preview, floor, RegionDragPreviewColor);
    }

    public static void DrawRegionVerticalCutPreview(
        VisualWallSegment visual,
        WallRegion region,
        float cutAlongMm)
    {
        if (region.Shape == WallRegionShape.Circular)
            return;

        var wall = visual.Wall;
        float floor = wall.FloorOffset;
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        WallRegionGeometry.GetVerticalCutLineSpan(region, wall.Length, wallTop, cutAlongMm, out float bottom, out float top);
        DrawPolygonEdge(visual, region.Face, floor, cutAlongMm, bottom, cutAlongMm, top, RegionVerticalCutPreviewColor);
    }

    private static void DrawRotatedRectRegionFill(
        VisualWallSegment segment,
        WallRegion region,
        bool faceA,
        float floor,
        Vector4 color)
    {
        Span<float> along = stackalloc float[4];
        Span<float> height = stackalloc float[4];
        WallRegionGeometry.GetRectCorners(region, along, height);
        var temp = new WallRegion { Shape = WallRegionShape.Polygon, Face = region.Face };
        temp.PolygonAlongMm.AddRange(along.ToArray());
        temp.PolygonHeightMm.AddRange(height.ToArray());
        DrawPolygonRegionFill(segment, temp, faceA, floor, color);
    }

    private static void DrawRotatedRectRegionOutline(
        VisualWallSegment segment,
        WallRegion region,
        float floor,
        Vector4 color)
    {
        Span<float> along = stackalloc float[4];
        Span<float> height = stackalloc float[4];
        WallRegionGeometry.GetRectCorners(region, along, height);

        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            DrawPolygonEdge(segment, region.Face, floor, along[i], height[i], along[j], height[j], color);
        }
    }

    private static void DrawPolygonRegionFill(
        VisualWallSegment segment,
        WallRegion region,
        bool faceA,
        float floor,
        Vector4 color)
    {
        if (region.PolygonAlongMm.Count < 3)
            return;

        var along = region.PolygonAlongMm.ToArray();
        var height = region.PolygonHeightMm.ToArray();
        var triangles = WallRegionGeometry.TriangulatePolygon(along, height);

        Vector2 f1 = faceA ? segment.A1 : segment.B1;
        Vector2 f2 = faceA ? segment.A2 : segment.B2;
        float length = segment.Wall.Length;

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v0 = ToFacePoint(f1, f2, length, floor, triangles[i].along, triangles[i].height);
            Vector3 v1 = ToFacePoint(f1, f2, length, floor, triangles[i + 1].along, triangles[i + 1].height);
            Vector3 v2 = ToFacePoint(f1, f2, length, floor, triangles[i + 2].along, triangles[i + 2].height);
            RenderEngine.Triangle(v0, v1, v2, color);
        }
    }

    private static void DrawPolygonRegionOutline(
        VisualWallSegment segment,
        WallRegion region,
        float floor,
        Vector4 color)
    {
        if (region.PolygonAlongMm.Count < 2)
            return;

        int n = region.PolygonAlongMm.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            DrawPolygonEdge(
                segment,
                region.Face,
                floor,
                region.PolygonAlongMm[i],
                region.PolygonHeightMm[i],
                region.PolygonAlongMm[j],
                region.PolygonHeightMm[j],
                color);
        }
    }

    private static void DrawPolygonEdge(
        VisualWallSegment segment,
        FaceType face,
        float floor,
        float along0,
        float height0,
        float along1,
        float height1,
        Vector4 color)
    {
        Vector2 f1 = face == FaceType.Internal ? segment.A1 : segment.B1;
        Vector2 f2 = face == FaceType.Internal ? segment.A2 : segment.B2;
        float length = segment.Wall.Length;

        Vector2 p0 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along0);
        Vector2 p1 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along1);
        RenderEngine.Line(
            new Vector3(p0.X, floor + height0, p0.Y),
            new Vector3(p1.X, floor + height1, p1.Y),
            color);
    }

    private static Vector3 ToFacePoint(Vector2 f1, Vector2 f2, float length, float floor, float along, float height)
    {
        Vector2 p = WallFaceGeometry.LerpAlongFace(f1, f2, length, along);
        return new Vector3(p.X, floor + height, p.Y);
    }

    private static void DrawCircleRegionFill(
        VisualWallSegment segment,
        WallRegion region,
        bool faceA,
        float floor,
        Vector4 color)
    {
        Vector2 f1 = faceA ? segment.A1 : segment.B1;
        Vector2 f2 = faceA ? segment.A2 : segment.B2;
        float length = segment.Wall.Length;
        float radius = WallRegionGeometry.GetEffectiveRadius(region);
        float centerY = floor + region.CenterHeightMm;
        int segments = WallRegionGeometry.CircleSegmentCount;

        RenderEngine.BeginTriangleBatch();

        Vector2 center2 = WallFaceGeometry.LerpAlongFace(f1, f2, length, region.CenterAlongMm);
        Vector3 center = new(center2.X, centerY, center2.Y);

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * MathF.PI * 2f;
            float a1 = (i + 1) / (float)segments * MathF.PI * 2f;

            float along0 = region.CenterAlongMm + MathF.Cos(a0) * radius;
            float height0 = region.CenterHeightMm + MathF.Sin(a0) * radius;
            float along1 = region.CenterAlongMm + MathF.Cos(a1) * radius;
            float height1 = region.CenterHeightMm + MathF.Sin(a1) * radius;

            Vector2 p0 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along0);
            Vector2 p1 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along1);

            RenderEngine.Triangle(
                center,
                new Vector3(p0.X, floor + height0, p0.Y),
                new Vector3(p1.X, floor + height1, p1.Y),
                color);
        }

        RenderEngine.EndTriangleBatch(blend: true);
    }

    private static void DrawCircleRegionOutline(
        VisualWallSegment segment,
        WallRegion region,
        float floor,
        Vector4 color)
    {
        Vector2 f1 = region.Face == FaceType.Internal ? segment.A1 : segment.B1;
        Vector2 f2 = region.Face == FaceType.Internal ? segment.A2 : segment.B2;
        float length = segment.Wall.Length;
        float radius = WallRegionGeometry.GetEffectiveRadius(region);
        int segments = WallRegionGeometry.CircleSegmentCount;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * MathF.PI * 2f;
            float a1 = (i + 1) / (float)segments * MathF.PI * 2f;

            float along0 = region.CenterAlongMm + MathF.Cos(a0) * radius;
            float height0 = region.CenterHeightMm + MathF.Sin(a0) * radius;
            float along1 = region.CenterAlongMm + MathF.Cos(a1) * radius;
            float height1 = region.CenterHeightMm + MathF.Sin(a1) * radius;

            Vector2 p0 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along0);
            Vector2 p1 = WallFaceGeometry.LerpAlongFace(f1, f2, length, along1);

            RenderEngine.Line(
                new Vector3(p0.X, floor + height0, p0.Y),
                new Vector3(p1.X, floor + height1, p1.Y),
                color);
        }
    }

    private static void DrawRegionOutline(
        VisualWallSegment segment,
        FaceType face,
        float startAlong,
        float endAlong,
        float yBottom,
        float yTop,
        Vector4 color)
    {
        float length = segment.Wall.Length;

        if (face == FaceType.Internal)
        {
            WallFaceGeometry.DrawRectOutline(segment.A1, segment.A2, length, startAlong, endAlong, yBottom, yTop, color);
        }
        else
        {
            WallFaceGeometry.DrawRectOutline(segment.B1, segment.B2, length, startAlong, endAlong, yBottom, yTop, color);
        }
    }

    private static void DrawFaceVerticalStrip(
        VisualWallSegment segment,
        bool faceA,
        float d1,
        float d2,
        float y1,
        float y2,
        Vector4 color)
    {
        Vector2 f1 = faceA ? segment.A1 : segment.B1;
        Vector2 f2 = faceA ? segment.A2 : segment.B2;
        float length = segment.Wall.Length;

        if (length < 0.001f)
            return;

        Vector2 p1 = WallFaceGeometry.LerpAlongFace(f1, f2, length, d1);
        Vector2 p2 = WallFaceGeometry.LerpAlongFace(f1, f2, length, d2);

        Vector3 v0 = new(p1.X, y1, p1.Y);
        Vector3 v1 = new(p2.X, y1, p2.Y);
        Vector3 v2 = new(p2.X, y2, p2.Y);
        Vector3 v3 = new(p1.X, y2, p1.Y);
        RenderEngine.QuadDouble(v0, v1, v2, v3, color);
    }

    private static void DrawVerticalBandLine(
        Vector2 faceStart,
        Vector2 faceEnd,
        float wallLength,
        float along,
        float yBottom,
        float yTop,
        Vector4 color)
    {
        Vector2 p = WallFaceGeometry.LerpAlongFace(faceStart, faceEnd, wallLength, along);
        RenderEngine.Line(new Vector3(p.X, yBottom, p.Y), new Vector3(p.X, yTop, p.Y), color);
    }

    private static void DrawHorizontalBandLine(
        Vector2 faceStart,
        Vector2 faceEnd,
        float wallLength,
        float y,
        Vector4 color)
    {
        Vector2 p1 = WallFaceGeometry.LerpAlongFace(faceStart, faceEnd, wallLength, 0f);
        Vector2 p2 = WallFaceGeometry.LerpAlongFace(faceStart, faceEnd, wallLength, wallLength);
        RenderEngine.Line(new Vector3(p1.X, y, p1.Y), new Vector3(p2.X, y, p2.Y), color);
    }
}
