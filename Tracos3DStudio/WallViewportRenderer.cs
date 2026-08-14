using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Sólidos, arestas e cota de parede no viewport (reta e curva, com recorte de aberturas).
/// </summary>
public static class WallViewportRenderer
{
    public static void DrawSegmentSolid(
        VisualWallSegment segment,
        bool faceSelected,
        bool groupSelected,
        bool xRay,
        bool insertionHighlight = false,
        bool thirtyFortyMoving = false,
        LayerFillMode fillMode = LayerFillMode.Default)
    {
        if (!LayerFillModeCatalog.ShouldDrawSolid(fillMode))
            return;

        if (segment.IsCurved &&
            segment.TessellatedFaceA != null &&
            segment.TessellatedFaceB != null)
        {
            if (segment.Wall.Openings.Count == 0)
                DrawCurvedSegmentSolid(segment, faceSelected, groupSelected, xRay, insertionHighlight, thirtyFortyMoving, fillMode);
            else
                DrawCurvedSegmentSolidWithOpenings(segment, faceSelected, groupSelected, xRay, insertionHighlight, thirtyFortyMoving, fillMode);
            return;
        }

        var wall = segment.Wall;
        bool transparent = xRay && !faceSelected && !insertionHighlight && !thirtyFortyMoving;
        float alpha = LayerFillModeCatalog.ResolveSolidAlpha(fillMode, transparent ? 0.35f : 1f);
        bool blend = transparent || fillMode == LayerFillMode.Ghost;

        float yFloor = wall.FloorOffset;
        float yTopStart = yFloor + wall.HeightStart;
        float yTopEnd = yFloor + wall.HeightEnd;

        Vector3 a0 = new(segment.A1.X, yFloor, segment.A1.Y);
        Vector3 a1 = new(segment.A1.X, yTopStart, segment.A1.Y);
        Vector3 a2 = new(segment.A2.X, yFloor, segment.A2.Y);
        Vector3 a3 = new(segment.A2.X, yTopEnd, segment.A2.Y);

        Vector3 b0 = new(segment.B1.X, yFloor, segment.B1.Y);
        Vector3 b1 = new(segment.B1.X, yTopStart, segment.B1.Y);
        Vector3 b2 = new(segment.B2.X, yFloor, segment.B2.Y);
        Vector3 b3 = new(segment.B2.X, yTopEnd, segment.B2.Y);

        var (colorFaceA, colorFaceB) = GetFaceColors(
            wall,
            faceSelected,
            groupSelected,
            insertionHighlight,
            thirtyFortyMoving,
            alpha);

        if (wall.Openings.Count == 0)
        {
            RenderEngine.BeginTriangleBatch();
            RenderEngine.QuadDouble(a0, a2, a3, a1, colorFaceA);
            RenderEngine.QuadDouble(b2, b0, b1, b3, colorFaceB);
            RenderEngine.QuadDouble(a0, b0, b1, a1, new Vector4(0.80f, 0.80f, 0.76f, alpha));
            RenderEngine.QuadDouble(a2, b2, b3, a3, new Vector4(0.78f, 0.78f, 0.74f, alpha));
            RenderEngine.QuadDouble(a1, a3, b3, b1, new Vector4(0.74f, 0.74f, 0.70f, alpha));
            RenderEngine.EndTriangleBatch(blend: blend, polygonOffsetFill: !blend);
            return;
        }

        DrawLongFaceWithOpenings(segment, faceA: true, colorFaceA.X, colorFaceA.Y, colorFaceA.Z, alpha);
        DrawLongFaceWithOpenings(segment, faceA: false, colorFaceB.X, colorFaceB.Y, colorFaceB.Z, alpha);

        RenderEngine.BeginTriangleBatch();
        RenderEngine.QuadDouble(a0, b0, b1, a1, new Vector4(0.80f, 0.80f, 0.76f, alpha));
        RenderEngine.QuadDouble(a2, b2, b3, a3, new Vector4(0.78f, 0.78f, 0.74f, alpha));
        RenderEngine.QuadDouble(a1, a3, b3, b1, new Vector4(0.74f, 0.74f, 0.70f, alpha));
        RenderEngine.EndTriangleBatch(blend: blend, polygonOffsetFill: !blend);
    }

    public static void DrawSegmentEdges(
        VisualWallSegment segment,
        bool faceSelected,
        bool groupSelected,
        bool xRay,
        bool insertionHighlight = false,
        LayerFillMode fillMode = LayerFillMode.Default,
        string? layerId = null)
    {
        var wall = segment.Wall;
        float floor = wall.FloorOffset;
        float yTopStart = floor + wall.HeightStart;
        float yTopEnd = floor + wall.HeightEnd;

        const float baseY = 4f;
        const float topLift = 3f;
        float yBase = floor + baseY;
        float yTopA = yTopStart + topLift;
        float yTopB = yTopEnd + topLift;

        const float edgeOutset = 2f;
        Vector2 aOut = -wall.LeftNormal * edgeOutset;
        Vector2 bOut = wall.LeftNormal * edgeOutset;

        var color = insertionHighlight
            ? new Vector4(0.85f, 0.65f, 0.05f, 1f)
            : faceSelected
                ? new Vector4(0.0f, 0.15f, 1.0f, 1f)
                : groupSelected
                    ? new Vector4(0.85f, 0.05f, 0.05f, 1f)
                    : fillMode == LayerFillMode.OutlineOnly
                        ? WallLayerCatalog.GetLayerOutlineColor(layerId ?? segment.Wall.LayerId)
                        : new Vector4(0f, 0f, 0f, 1f);

        float thickness = fillMode == LayerFillMode.OutlineOnly
            ? 2.5f
            : insertionHighlight ? 2.5f : faceSelected ? 3f : groupSelected ? 2.5f : 2f;

        RenderEngine.BeginLineBatch();

        DrawEdgeLine(segment.A1 + aOut, segment.A2 + aOut, yBase, color);
        DrawEdgeLine(segment.B1 + bOut, segment.B2 + bOut, yBase, color);
        DrawEdgeLine(segment.A1 + aOut, segment.B1 + bOut, yBase, color);
        DrawEdgeLine(segment.A2 + aOut, segment.B2 + bOut, yBase, color);

        DrawEdgeLine(segment.A1 + aOut, segment.A2 + aOut, yTopA, yTopB, color);
        DrawEdgeLine(segment.B1 + bOut, segment.B2 + bOut, yTopA, yTopB, color);
        DrawEdgeLine(segment.A1 + aOut, segment.B1 + bOut, yTopA, yTopA, color);
        DrawEdgeLine(segment.A2 + aOut, segment.B2 + bOut, yTopB, yTopB, color);

        DrawVerticalEdge(segment.A1 + aOut, floor, yTopStart, color);
        DrawVerticalEdge(segment.A2 + aOut, floor, yTopEnd, color);
        DrawVerticalEdge(segment.B1 + bOut, floor, yTopStart, color);
        DrawVerticalEdge(segment.B2 + bOut, floor, yTopEnd, color);

        RenderEngine.EndLineBatch(
            thickness,
            depthTest: !xRay,
            polygonOffsetLine: !xRay,
            polygonOffsetFactor: -1f,
            polygonOffsetUnits: -2f);
    }

    public static void DrawSelectedMeasurement(VisualWallSegment segment)
    {
        Vector2 dir = segment.Wall.Direction;
        Vector2 normal = new(-dir.Y, dir.X);
        Vector2 start = segment.Wall.Start + normal * 320f;
        Vector2 end = segment.Wall.End + normal * 320f;

        RenderEngine.BeginLineBatch();
        RenderEngine.Color4(0.05f, 0.10f, 1f, 1f);

        float measY = Math.Max(segment.Wall.HeightStart, segment.Wall.HeightEnd)
                      + segment.Wall.FloorOffset + 180f;
        DrawEdgeLine(start, end, measY);
        DrawEdgeLine(segment.Wall.Start, start, measY);
        DrawEdgeLine(segment.Wall.End, end, measY);

        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    private static (Vector4 faceA, Vector4 faceB) GetFaceColors(
        WallSegment wall,
        bool faceSelected,
        bool groupSelected,
        bool insertionHighlight,
        bool thirtyFortyMoving,
        float alpha)
    {
        Vector4 colorFaceA = thirtyFortyMoving
            ? new Vector4(0.92f, 0.32f, 0.22f, alpha)
            : insertionHighlight
                ? new Vector4(1.0f, 0.90f, 0.30f, alpha)
                : faceSelected
                    ? new Vector4(0.48f, 0.66f, 1.0f, alpha)
                    : groupSelected
                        ? new Vector4(0.96f, 0.96f, 0.90f, alpha)
                        : new Vector4(0.94f, 0.94f, 0.88f, alpha);

        Vector4 colorFaceB = thirtyFortyMoving
            ? new Vector4(0.85f, 0.28f, 0.18f, alpha)
            : insertionHighlight
                ? new Vector4(0.95f, 0.84f, 0.22f, alpha)
                : faceSelected
                    ? new Vector4(0.38f, 0.54f, 0.96f, alpha)
                    : groupSelected
                        ? new Vector4(0.88f, 0.88f, 0.82f, alpha)
                        : new Vector4(0.86f, 0.86f, 0.80f, alpha);

        ApplyDryWallTintIfNeeded(wall, faceSelected, groupSelected, insertionHighlight, thirtyFortyMoving, ref colorFaceA, ref colorFaceB);
        return (colorFaceA, colorFaceB);
    }

    private static void ApplyDryWallTintIfNeeded(
        WallSegment wall,
        bool faceSelected,
        bool groupSelected,
        bool insertionHighlight,
        bool thirtyFortyMoving,
        ref Vector4 colorFaceA,
        ref Vector4 colorFaceB)
    {
        if (wall.ConstructionType != WallConstructionType.DryWall ||
            faceSelected || groupSelected || insertionHighlight || thirtyFortyMoving)
            return;

        colorFaceA = new Vector4(0.98f, 0.98f, 1.0f, colorFaceA.W);
        colorFaceB = new Vector4(0.94f, 0.94f, 0.98f, colorFaceB.W);
    }

    private static void DrawCurvedSegmentSolid(
        VisualWallSegment segment,
        bool faceSelected,
        bool groupSelected,
        bool xRay,
        bool insertionHighlight,
        bool thirtyFortyMoving,
        LayerFillMode fillMode)
    {
        var wall = segment.Wall;
        var faceA = segment.TessellatedFaceA!;
        var faceB = segment.TessellatedFaceB!;

        bool transparent = xRay && !faceSelected && !insertionHighlight && !thirtyFortyMoving;
        float alpha = LayerFillModeCatalog.ResolveSolidAlpha(fillMode, transparent ? 0.35f : 1f);
        bool blend = transparent || fillMode == LayerFillMode.Ghost;

        var (colorFaceA, colorFaceB) = GetFaceColors(
            wall,
            faceSelected,
            groupSelected,
            insertionHighlight,
            thirtyFortyMoving,
            alpha);

        float yFloor = wall.FloorOffset;
        int count = faceA.Count;

        RenderEngine.BeginTriangleBatch();

        for (int i = 0; i < count - 1; i++)
        {
            float t0 = i / (float)(count - 1);
            float t1 = (i + 1) / (float)(count - 1);
            float yTop0 = yFloor + wall.HeightAtDistance(wall.Length * t0);
            float yTop1 = yFloor + wall.HeightAtDistance(wall.Length * t1);

            Vector3 a0 = new(faceA[i].X, yFloor, faceA[i].Y);
            Vector3 a1 = new(faceA[i].X, yTop0, faceA[i].Y);
            Vector3 a2 = new(faceA[i + 1].X, yFloor, faceA[i + 1].Y);
            Vector3 a3 = new(faceA[i + 1].X, yTop1, faceA[i + 1].Y);

            Vector3 b0 = new(faceB[i].X, yFloor, faceB[i].Y);
            Vector3 b1 = new(faceB[i].X, yTop0, faceB[i].Y);
            Vector3 b2 = new(faceB[i + 1].X, yFloor, faceB[i + 1].Y);
            Vector3 b3 = new(faceB[i + 1].X, yTop1, faceB[i + 1].Y);

            RenderEngine.QuadDouble(a0, a2, a3, a1, colorFaceA);
            RenderEngine.QuadDouble(b2, b0, b1, b3, colorFaceB);
            RenderEngine.QuadDouble(a0, b0, b1, a1, new Vector4(0.80f, 0.80f, 0.76f, alpha));
            RenderEngine.QuadDouble(a2, b2, b3, a3, new Vector4(0.78f, 0.78f, 0.74f, alpha));
        }

        if (count >= 2)
        {
            float yTopStart = yFloor + wall.HeightStart;
            float yTopEnd = yFloor + wall.HeightEnd;
            Vector3 aStartTop = new(faceA[0].X, yTopStart, faceA[0].Y);
            Vector3 aEndTop = new(faceA[^1].X, yTopEnd, faceA[^1].Y);
            Vector3 bStartTop = new(faceB[0].X, yTopStart, faceB[0].Y);
            Vector3 bEndTop = new(faceB[^1].X, yTopEnd, faceB[^1].Y);
            Vector3 aStartFloor = new(faceA[0].X, yFloor, faceA[0].Y);
            Vector3 aEndFloor = new(faceA[^1].X, yFloor, faceA[^1].Y);
            Vector3 bStartFloor = new(faceB[0].X, yFloor, faceB[0].Y);
            Vector3 bEndFloor = new(faceB[^1].X, yFloor, faceB[^1].Y);

            RenderEngine.QuadDouble(aStartFloor, bStartFloor, bStartTop, aStartTop, new Vector4(0.80f, 0.80f, 0.76f, alpha));
            RenderEngine.QuadDouble(aEndFloor, bEndFloor, bEndTop, aEndTop, new Vector4(0.78f, 0.78f, 0.74f, alpha));
            RenderEngine.QuadDouble(aStartTop, aEndTop, bEndTop, bStartTop, new Vector4(0.74f, 0.74f, 0.70f, alpha));
        }

        RenderEngine.EndTriangleBatch(blend: blend, polygonOffsetFill: !blend);
    }

    private static void DrawCurvedSegmentSolidWithOpenings(
        VisualWallSegment segment,
        bool faceSelected,
        bool groupSelected,
        bool xRay,
        bool insertionHighlight,
        bool thirtyFortyMoving,
        LayerFillMode fillMode)
    {
        var wall = segment.Wall;
        var faceA = segment.TessellatedFaceA!;
        var faceB = segment.TessellatedFaceB!;

        bool transparent = xRay && !faceSelected && !insertionHighlight && !thirtyFortyMoving;
        float alpha = LayerFillModeCatalog.ResolveSolidAlpha(fillMode, transparent ? 0.35f : 1f);
        bool blend = transparent || fillMode == LayerFillMode.Ghost;

        var (colorFaceA, colorFaceB) = GetFaceColors(
            wall,
            faceSelected,
            groupSelected,
            insertionHighlight,
            thirtyFortyMoving,
            alpha);

        float yFloor = wall.FloorOffset;
        float length = wall.Length;
        int count = faceA.Count;

        var ys = new List<float> { yFloor };
        foreach (var opening in wall.Openings)
        {
            if (!opening.AutoCutWall)
                continue;

            ys.Add(yFloor + opening.SillHeight);
            ys.Add(yFloor + opening.TopHeight);
        }

        ys = ys.Distinct().OrderBy(y => y).ToList();

        RenderEngine.BeginTriangleBatch();

        for (int i = 0; i < count - 1; i++)
        {
            float d0 = length * i / (float)(count - 1);
            float d1 = length * (i + 1) / (float)(count - 1);
            float yTop0 = yFloor + wall.HeightAtDistance(d0);
            float yTop1 = yFloor + wall.HeightAtDistance(d1);

            DrawCurvedFaceStrip(faceA, i, yFloor, yTop0, yTop1, d0, d1, wall, ys, colorFaceA);
            DrawCurvedFaceStrip(faceB, i, yFloor, yTop0, yTop1, d0, d1, wall, ys, colorFaceB);

            Vector3 a0 = new(faceA[i].X, yFloor, faceA[i].Y);
            Vector3 a1 = new(faceA[i].X, yTop0, faceA[i].Y);
            Vector3 a2 = new(faceA[i + 1].X, yFloor, faceA[i + 1].Y);
            Vector3 a3 = new(faceA[i + 1].X, yTop1, faceA[i + 1].Y);
            Vector3 b0 = new(faceB[i].X, yFloor, faceB[i].Y);
            Vector3 b1 = new(faceB[i].X, yTop0, faceB[i].Y);
            Vector3 b2 = new(faceB[i + 1].X, yFloor, faceB[i + 1].Y);
            Vector3 b3 = new(faceB[i + 1].X, yTop1, faceB[i + 1].Y);

            if (!IsCurvedStripInsideOpening(wall, d0, d1, yFloor, Math.Min(yTop0, yTop1)))
                RenderEngine.QuadDouble(a0, b0, b1, a1, new Vector4(0.80f, 0.80f, 0.76f, alpha));

            if (!IsCurvedStripInsideOpening(wall, d0, d1, yFloor, Math.Min(yTop0, yTop1)))
                RenderEngine.QuadDouble(a2, b2, b3, a3, new Vector4(0.78f, 0.78f, 0.74f, alpha));
        }

        if (count >= 2)
        {
            float yTopStart = yFloor + wall.HeightStart;
            float yTopEnd = yFloor + wall.HeightEnd;
            Vector3 aStartTop = new(faceA[0].X, yTopStart, faceA[0].Y);
            Vector3 aEndTop = new(faceA[^1].X, yTopEnd, faceA[^1].Y);
            Vector3 bStartTop = new(faceB[0].X, yTopStart, faceB[0].Y);
            Vector3 bEndTop = new(faceB[^1].X, yTopEnd, faceB[^1].Y);
            Vector3 aStartFloor = new(faceA[0].X, yFloor, faceA[0].Y);
            Vector3 aEndFloor = new(faceA[^1].X, yFloor, faceA[^1].Y);
            Vector3 bStartFloor = new(faceB[0].X, yFloor, faceB[0].Y);
            Vector3 bEndFloor = new(faceB[^1].X, yFloor, faceB[^1].Y);

            RenderEngine.QuadDouble(aStartFloor, bStartFloor, bStartTop, aStartTop, new Vector4(0.80f, 0.80f, 0.76f, alpha));
            RenderEngine.QuadDouble(aEndFloor, bEndFloor, bEndTop, aEndTop, new Vector4(0.78f, 0.78f, 0.74f, alpha));
            RenderEngine.QuadDouble(aStartTop, aEndTop, bEndTop, bStartTop, new Vector4(0.74f, 0.74f, 0.70f, alpha));
        }

        RenderEngine.EndTriangleBatch(blend: blend, polygonOffsetFill: !blend);
    }

    private static void DrawCurvedFaceStrip(
        List<Vector2> face,
        int index,
        float yFloor,
        float yTopLeft,
        float yTopRight,
        float d0,
        float d1,
        WallSegment wall,
        List<float> ys,
        Vector4 color)
    {
        for (int yi = 0; yi < ys.Count - 1; yi++)
        {
            float y1 = ys[yi];
            float y2 = ys[yi + 1];
            float topAtY1 = MathF.Max(yTopLeft, yTopRight);
            if (y2 > topAtY1)
                break;

            float cx = (d0 + d1) * 0.5f;
            float cy = (y1 + y2) * 0.5f - yFloor;

            if (WallFaceGeometry.IsInsideAnyOpening(wall, cx, cy))
                continue;

            float leftY2 = MathF.Min(y2, yTopLeft);
            float rightY2 = MathF.Min(y2, yTopRight);

            Vector3 p0 = new(face[index].X, y1, face[index].Y);
            Vector3 p1 = new(face[index + 1].X, y1, face[index + 1].Y);
            Vector3 p2 = new(face[index + 1].X, rightY2, face[index + 1].Y);
            Vector3 p3 = new(face[index].X, leftY2, face[index].Y);

            RenderEngine.QuadDouble(p0, p1, p2, p3, color);
        }
    }

    private static bool IsCurvedStripInsideOpening(WallSegment wall, float d0, float d1, float yFloor, float yTop)
    {
        float cx = (d0 + d1) * 0.5f;
        float cy = (yFloor + yTop) * 0.5f - yFloor;
        return WallFaceGeometry.IsInsideAnyOpening(wall, cx, cy);
    }

    private static void DrawLongFaceWithOpenings(
        VisualWallSegment segment,
        bool faceA,
        float cr,
        float cg,
        float cb,
        float alpha = 1f)
    {
        var wall = segment.Wall;
        Vector2 f1 = faceA ? segment.A1 : segment.B1;
        Vector2 f2 = faceA ? segment.A2 : segment.B2;
        float length = wall.Length;
        float floor = wall.FloorOffset;

        if (length < 0.001f)
            return;

        var xs = new List<float> { 0f, length };
        var ys = new List<float> { floor };

        foreach (var opening in wall.Openings)
        {
            if (!opening.AutoCutWall)
                continue;

            xs.Add(opening.DistanceFromStart);
            xs.Add(opening.EndDistance);
            ys.Add(floor + opening.SillHeight);
            ys.Add(floor + opening.TopHeight);
        }

        xs = xs.Distinct().OrderBy(x => x).ToList();
        ys = ys.Distinct().OrderBy(y => y).ToList();

        var color = new Vector4(cr, cg, cb, alpha);
        RenderEngine.BeginTriangleBatch();

        for (var xi = 0; xi < xs.Count - 1; xi++)
        {
            float x1 = xs[xi];
            float x2 = xs[xi + 1];

            float topY1 = floor + wall.HeightAtDistance(x1);
            float topY2 = floor + wall.HeightAtDistance(x2);
            float topYMax = Math.Max(topY1, topY2);

            for (var yi = 0; yi < ys.Count - 1; yi++)
            {
                float y1 = ys[yi];
                float y2 = ys[yi + 1];

                if (y2 > topYMax)
                    break;

                float cx = (x1 + x2) * 0.5f;
                float cy = (y1 + y2) * 0.5f - floor;

                if (WallFaceGeometry.IsInsideAnyOpening(wall, cx, cy))
                    continue;

                WallFaceGeometry.DrawQuad(f1, f2, length, x1, x2, y1, y2, color);
            }

            float lastFixedY = ys.Count > 0 ? ys[^1] : floor;
            if (lastFixedY < topYMax)
            {
                float cx = (x1 + x2) * 0.5f;
                float cy = (lastFixedY + topYMax) * 0.5f - floor;

                if (!WallFaceGeometry.IsInsideAnyOpening(wall, cx, cy))
                    WallFaceGeometry.DrawQuadTrapezoid(f1, f2, length, x1, x2, lastFixedY, topY1, topY2, color);
            }
        }

        RenderEngine.EndTriangleBatch(blend: alpha < 1f, polygonOffsetFill: alpha >= 1f);
    }

    private static void DrawEdgeLine(Vector2 p1, Vector2 p2, float y, Vector4? color = null)
    {
        RenderEngine.Line(p1, p2, y, color);
    }

    private static void DrawEdgeLine(Vector2 p1, Vector2 p2, float y1, float y2, Vector4 color)
    {
        RenderEngine.Line(new Vector3(p1.X, y1, p1.Y), new Vector3(p2.X, y2, p2.Y), color);
    }

    private static void DrawVerticalEdge(Vector2 p, float yStart, float yEnd, Vector4 color)
    {
        RenderEngine.Line(new Vector3(p.X, yStart, p.Y), new Vector3(p.X, yEnd, p.Y), color);
    }
}
