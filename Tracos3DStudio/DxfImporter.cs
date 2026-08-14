using System.Globalization;
using System.IO;
using System.Text;

namespace Tracos3DStudio;

public static class DxfImporter
{
    public sealed class ImportResult
    {
        public required IReadOnlyList<WallSegment> Walls { get; init; }

        public int LineCount { get; init; }
    }

    public static ImportResult ImportFloorPlan(string filePath, float defaultHeight = 2600f, float defaultThickness = 150f)
    {
        var lines = ParseLines(filePath);
        var walls = new List<WallSegment>();

        foreach (var line in lines)
        {
            if (line.Length < 0.1f)
                continue;

            walls.Add(new WallSegment(line.Start, line.End, defaultThickness, defaultHeight, WallOrientation.Right));
        }

        return new ImportResult
        {
            Walls = walls,
            LineCount = lines.Count
        };
    }

    private static List<(OpenTK.Mathematics.Vector2 Start, OpenTK.Mathematics.Vector2 End, float Length)> ParseLines(string filePath)
    {
        var tokens = Tokenize(File.ReadAllText(filePath, Encoding.ASCII));
        var result = new List<(OpenTK.Mathematics.Vector2 Start, OpenTK.Mathematics.Vector2 End, float Length)>();

        bool inEntities = false;

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i] == "2" && tokens[i + 1] == "ENTITIES")
            {
                inEntities = true;
                continue;
            }

            if (inEntities && tokens[i] == "0" && tokens[i + 1] == "ENDSEC")
                break;

            if (!inEntities || tokens[i] != "0" || tokens[i + 1] != "LINE")
                continue;

            if (!TryReadLine(tokens, i, out var start, out var end))
                continue;

            float length = (end - start).Length;
            result.Add((start, end, length));
        }

        return result;
    }

    private static bool TryReadLine(
        IReadOnlyList<string> tokens,
        int entityIndex,
        out OpenTK.Mathematics.Vector2 start,
        out OpenTK.Mathematics.Vector2 end)
    {
        start = default;
        end = default;

        float? x1 = null, y1 = null, x2 = null, y2 = null;

        for (int i = entityIndex + 2; i < tokens.Count - 1; i++)
        {
            if (tokens[i] == "0")
                break;

            if (!int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code))
                continue;

            string value = tokens[i + 1];

            switch (code)
            {
                case 10:
                    x1 = ParseFloat(value);
                    break;
                case 20:
                    y1 = ParseFloat(value);
                    break;
                case 11:
                    x2 = ParseFloat(value);
                    break;
                case 21:
                    y2 = ParseFloat(value);
                    break;
            }

            i++;
        }

        if (x1 == null || y1 == null || x2 == null || y2 == null)
            return false;

        start = new OpenTK.Mathematics.Vector2(x1.Value, y1.Value);
        end = new OpenTK.Mathematics.Vector2(x2.Value, y2.Value);
        return true;
    }

    private static float ParseFloat(string value) =>
        float.Parse(value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static List<string> Tokenize(string content)
    {
        var tokens = new List<string>();
        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) != null)
            tokens.Add(line.TrimEnd());

        return tokens;
    }
}
