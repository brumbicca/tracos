using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Geometria compartilhada para faces de parede no viewport (interpolação ao longo da face, recortes de abertura).
/// </summary>
public static class WallFaceGeometry
{
    public static Vector2 LerpAlongFace(Vector2 start, Vector2 end, float wallLength, float distance)
    {
        if (wallLength < 0.001f)
            return start;

        float t = distance / wallLength;
        return start + (end - start) * t;
    }

    public static Vector2 GetPointOnTessellatedFace(List<Vector2> face, float wallLength, float distanceAlong)
    {
        if (face.Count == 0)
            return Vector2.Zero;

        if (face.Count == 1 || wallLength < 0.001f)
            return face[0];

        float t = Math.Clamp(distanceAlong / wallLength, 0f, 1f) * (face.Count - 1);
        int i = (int)MathF.Floor(t);
        int j = Math.Min(i + 1, face.Count - 1);
        float frac = t - i;
        return face[i] + (face[j] - face[i]) * frac;
    }

    public static bool IsInsideAnyOpening(WallSegment wall, float distanceAlong, float y)
    {
        foreach (var opening in wall.Openings)
        {
            if (!opening.AutoCutWall)
                continue;

            if (distanceAlong >= opening.DistanceFromStart &&
                distanceAlong <= opening.EndDistance &&
                y >= opening.SillHeight &&
                y <= opening.TopHeight)
            {
                return true;
            }
        }

        return false;
    }

    public static void DrawQuad(
        Vector2 faceStart,
        Vector2 faceEnd,
        float wallLength,
        float d1,
        float d2,
        float y1,
        float y2,
        Vector4 color)
    {
        Vector2 bl = LerpAlongFace(faceStart, faceEnd, wallLength, d1);
        Vector2 br = LerpAlongFace(faceStart, faceEnd, wallLength, d2);

        RenderEngine.QuadDouble(
            new Vector3(bl.X, y1, bl.Y),
            new Vector3(br.X, y1, br.Y),
            new Vector3(br.X, y2, br.Y),
            new Vector3(bl.X, y2, bl.Y),
            color);
    }

    public static void DrawQuadTrapezoid(
        Vector2 faceStart,
        Vector2 faceEnd,
        float wallLength,
        float d1,
        float d2,
        float yBottom,
        float yTopLeft,
        float yTopRight,
        Vector4 color)
    {
        Vector2 bl = LerpAlongFace(faceStart, faceEnd, wallLength, d1);
        Vector2 br = LerpAlongFace(faceStart, faceEnd, wallLength, d2);

        RenderEngine.QuadDouble(
            new Vector3(bl.X, yBottom, bl.Y),
            new Vector3(br.X, yBottom, br.Y),
            new Vector3(br.X, yTopRight, br.Y),
            new Vector3(bl.X, yTopLeft, bl.Y),
            color);
    }

    public static void DrawRectOutline(
        Vector2 faceStart,
        Vector2 faceEnd,
        float wallLength,
        float d1,
        float d2,
        float y1,
        float y2,
        Vector4 color)
    {
        Vector2 p1 = LerpAlongFace(faceStart, faceEnd, wallLength, d1);
        Vector2 p2 = LerpAlongFace(faceStart, faceEnd, wallLength, d2);
        DrawRectOutline(p1, p2, y1, y2, color);
    }

    public static void DrawRectOutline(Vector2 p1, Vector2 p2, float y1, float y2, Vector4 color)
    {
        RenderEngine.Line(new Vector3(p1.X, y1, p1.Y), new Vector3(p2.X, y1, p2.Y), color);
        RenderEngine.Line(new Vector3(p2.X, y1, p2.Y), new Vector3(p2.X, y2, p2.Y), color);
        RenderEngine.Line(new Vector3(p2.X, y2, p2.Y), new Vector3(p1.X, y2, p1.Y), color);
        RenderEngine.Line(new Vector3(p1.X, y2, p1.Y), new Vector3(p1.X, y1, p1.Y), color);
    }
}
