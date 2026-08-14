using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class FloorPatternRenderer
{
    public static void DrawPolygon(
        IReadOnlyList<Vector2> polygon,
        FloorMaterialDefinition material,
        float y,
        bool highlight = false)
    {
        if (polygon.Count < 3)
            return;

        if (material.Pattern == FloorMaterialPattern.Solid)
        {
            DrawSolidPolygon(polygon, y, ResolveColor(material.ColorHex, highlight));
            return;
        }

        DrawPatternedPolygon(polygon, material, y, highlight);
    }

    private static void DrawSolidPolygon(IReadOnlyList<Vector2> polygon, float y, Vector4 color)
    {
        var ordered = Geometry2D.IsClockwise(polygon)
            ? polygon.AsEnumerable().Reverse().ToList()
            : polygon.ToList();

        RenderEngine.BeginTriangleBatch();

        for (int i = 1; i < ordered.Count - 1; i++)
        {
            RenderEngine.Triangle(
                new Vector3(ordered[0].X, y, ordered[0].Y),
                new Vector3(ordered[i].X, y, ordered[i].Y),
                new Vector3(ordered[i + 1].X, y, ordered[i + 1].Y),
                color);
        }

        RenderEngine.EndTriangleBatch();
    }

    private static void DrawPatternedPolygon(
        IReadOnlyList<Vector2> polygon,
        FloorMaterialDefinition material,
        float y,
        bool highlight)
    {
        float minX = polygon.Min(p => p.X);
        float minY = polygon.Min(p => p.Y);
        float maxX = polygon.Max(p => p.X);
        float maxY = polygon.Max(p => p.Y);
        float step = MathF.Max(50f, material.TileSizeMm);

        var baseColor = ResolveColor(material.ColorHex, highlight);
        var accentColor = ResolveColor(material.AccentColorHex, highlight, accent: true);

        RenderEngine.BeginTriangleBatch();

        int startCol = (int)MathF.Floor(minX / step);
        int endCol = (int)MathF.Ceiling(maxX / step);
        int startRow = (int)MathF.Floor(minY / step);
        int endRow = (int)MathF.Ceiling(maxY / step);

        for (int col = startCol; col < endCol; col++)
        {
            for (int row = startRow; row < endRow; row++)
            {
                float x0 = col * step;
                float y0 = row * step;
                float x1 = x0 + step;
                float y1 = y0 + step;

                var center = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
                if (!Geometry2D.ContainsPoint(polygon, center))
                    continue;

                bool accent = material.Pattern switch
                {
                    FloorMaterialPattern.WoodPlank => col % 2 != 0,
                    _ => (col + row) % 2 != 0
                };

                var color = accent ? accentColor : baseColor;

                RenderEngine.Triangle(
                    new Vector3(x0, y, y0),
                    new Vector3(x1, y, y0),
                    new Vector3(x1, y, y1),
                    color);
                RenderEngine.Triangle(
                    new Vector3(x0, y, y0),
                    new Vector3(x1, y, y1),
                    new Vector3(x0, y, y1),
                    color);
            }
        }

        RenderEngine.EndTriangleBatch();
    }

    private static Vector4 ResolveColor(string hex, bool highlight, bool accent = false)
    {
        var (r, g, b) = ColorParsing.ParseHexRgb(hex);

        if (highlight)
        {
            r = MathF.Min(1f, r + 0.08f);
            g = MathF.Min(1f, g + 0.10f);
            b = MathF.Min(1f, b + 0.14f);
        }
        else if (accent)
        {
            r *= 0.92f;
            g *= 0.92f;
            b *= 0.92f;
        }

        return new Vector4(r, g, b, 1f);
    }
}
