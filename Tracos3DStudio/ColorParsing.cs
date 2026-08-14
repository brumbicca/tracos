namespace Tracos3DStudio;

public static class ColorParsing
{
    public static (float R, float G, float B) ParseHexRgb(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return (0.82f, 0.78f, 0.72f);

        string value = hex.TrimStart('#');

        if (value.Length != 6)
            return (0.82f, 0.78f, 0.72f);

        if (!int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out int rgb))
            return (0.82f, 0.78f, 0.72f);

        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;
        return (r, g, b);
    }
}
