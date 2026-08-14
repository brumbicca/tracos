using System;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Contornos de portas e janelas no viewport (inclui paredes curvas tesselladas).
/// </summary>
public static class OpeningViewportRenderer
{
    private static readonly Vector4 SelectedColor = new(0.05f, 0.15f, 1f, 1f);
    private static readonly Vector4 WindowColor = new(0.2f, 0.55f, 0.95f, 1f);
    private static readonly Vector4 DoorColor = new(0.35f, 0.25f, 0.15f, 1f);
    private static readonly Vector4 PlacementPreviewColor = new(0.2f, 1f, 0.35f, 1f);

    public static void DrawOutlines(VisualWallSegment segment, Guid? selectedOpeningId, bool xRay)
    {
        if (segment.Wall.Openings.Count == 0)
            return;

        RenderEngine.BeginLineBatch();

        foreach (var opening in segment.Wall.Openings)
        {
            bool selected = selectedOpeningId.HasValue && selectedOpeningId.Value == opening.Id;

            Vector4 color = selected
                ? SelectedColor
                : opening.Type == OpeningType.Window
                    ? WindowColor
                    : DoorColor;

            DrawBox(segment, opening, color);
        }

        RenderEngine.EndLineBatch(2f, depthTest: !xRay, polygonOffsetLine: !xRay);
    }

    public static void DrawPlacementPreview(VisualWallSegment segment, WallOpening opening)
    {
        RenderEngine.BeginLineBatch();
        DrawBox(segment, opening, PlacementPreviewColor);
        RenderEngine.EndLineBatch(2f, depthTest: false);
    }

    public static void DrawBox(VisualWallSegment segment, WallOpening opening, Vector4 color)
    {
        float d1 = opening.DistanceFromStart;
        float d2 = opening.EndDistance;
        float floor = segment.Wall.FloorOffset;
        float y1 = floor + opening.SillHeight;
        float y2 = floor + opening.TopHeight;

        if (segment.IsCurved &&
            segment.TessellatedFaceA != null &&
            segment.TessellatedFaceB != null)
        {
            float length = segment.Wall.Length;
            Vector2 a1 = WallFaceGeometry.GetPointOnTessellatedFace(segment.TessellatedFaceA, length, d1);
            Vector2 a2 = WallFaceGeometry.GetPointOnTessellatedFace(segment.TessellatedFaceA, length, d2);
            Vector2 b1 = WallFaceGeometry.GetPointOnTessellatedFace(segment.TessellatedFaceB, length, d1);
            Vector2 b2 = WallFaceGeometry.GetPointOnTessellatedFace(segment.TessellatedFaceB, length, d2);

            WallFaceGeometry.DrawRectOutline(a1, a2, y1, y2, color);
            WallFaceGeometry.DrawRectOutline(b1, b2, y1, y2, color);
            return;
        }

        float wallLength = segment.Wall.Length;
        WallFaceGeometry.DrawRectOutline(segment.A1, segment.A2, wallLength, d1, d2, y1, y2, color);
        WallFaceGeometry.DrawRectOutline(segment.B1, segment.B2, wallLength, d1, d2, y1, y2, color);
    }
}
