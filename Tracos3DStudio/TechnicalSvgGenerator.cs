using System.Globalization;
using System.Text;

namespace Tracos3DStudio;

public static class TechnicalSvgGenerator
{
  public static string FloorPlan(TechnicalDrawingSet drawing, int width = 800, int height = 400)
  {
    float margin = 40f;
    float worldW = Math.Max(1, drawing.MaxX - drawing.MinX);
    float worldH = Math.Max(1, drawing.MaxY - drawing.MinY);
    float scale = Math.Min((width - margin * 2) / worldW, (height - margin * 2) / worldH);

    float ToX(float x) => margin + (x - drawing.MinX) * scale;
    float ToY(float y) => height - margin - (y - drawing.MinY) * scale;

    var sb = new StringBuilder();
    sb.AppendLine(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\">");
    sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

    foreach (var wall in drawing.FloorPlanWalls)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"<line x1=\"{ToX(wall.X1):0.##}\" y1=\"{ToY(wall.Y1):0.##}\" x2=\"{ToX(wall.X2):0.##}\" y2=\"{ToY(wall.Y2):0.##}\" stroke=\"#333\" stroke-width=\"2\"/>");
    }

    foreach (var module in drawing.FloorPlanModules)
    {
      float x = ToX(module.X);
      float y = ToY(module.Y + module.Height);
      float w = module.Width * scale;
      float h = module.Height * scale;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"<rect x=\"{x:0.##}\" y=\"{y:0.##}\" width=\"{w:0.##}\" height=\"{h:0.##}\" fill=\"#e8e0d0\" stroke=\"#666\" stroke-width=\"1\"/>");

      if (!string.IsNullOrWhiteSpace(module.Label))
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"<text x=\"{x + w / 2:0.##}\" y=\"{y + h / 2:0.##}\" text-anchor=\"middle\" font-size=\"10\" fill=\"#333\">{Escape(module.Label)}</text>");
      }
    }

    foreach (var dim in drawing.FloorPlanDimensions)
    {
      float mx = ToX((dim.X1 + dim.X2) * 0.5f);
      float my = ToY((dim.Y1 + dim.Y2) * 0.5f) - 12f;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"<text x=\"{mx:0.##}\" y=\"{my:0.##}\" text-anchor=\"middle\" font-size=\"9\" fill=\"#0066cc\">{Escape(dim.Text)}</text>");
    }

    sb.AppendLine("</svg>");
    return sb.ToString();
  }

  public static string Elevation(TechnicalElevation elevation, int width = 800, int height = 300)
  {
    if (elevation.Modules.Count == 0)
      return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\"></svg>";

    float minX = elevation.Modules.Min(m => m.X);
    float minY = elevation.Modules.Min(m => m.Y);
    float maxX = elevation.Modules.Max(m => m.X + m.Width);
    float maxY = elevation.Modules.Max(m => m.Y + m.Height);

    float margin = 40f;
    float worldW = Math.Max(1, maxX - minX);
    float worldH = Math.Max(1, maxY - minY);
    float scale = Math.Min((width - margin * 2) / worldW, (height - margin * 2) / worldH);

    float ToX(float x) => margin + (x - minX) * scale;
    float ToY(float y) => height - margin - (y - minY) * scale;

    var sb = new StringBuilder();
    sb.AppendLine(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\">");
    sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

    foreach (var module in elevation.Modules)
    {
      float x = ToX(module.X);
      float y = ToY(module.Y + module.Height);
      float w = module.Width * scale;
      float h = module.Height * scale;
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"<rect x=\"{x:0.##}\" y=\"{y:0.##}\" width=\"{w:0.##}\" height=\"{h:0.##}\" fill=\"#d8e4f8\" stroke=\"#333\" stroke-width=\"1\"/>");

      if (!string.IsNullOrWhiteSpace(module.Label))
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"<text x=\"{x + w / 2:0.##}\" y=\"{y + h / 2:0.##}\" text-anchor=\"middle\" font-size=\"9\" fill=\"#333\">{Escape(module.Label)}</text>");
      }
    }

    foreach (var dim in elevation.Dimensions)
    {
      float mx = ToX((dim.X1 + dim.X2) * 0.5f);
      float my = ToY((dim.Y1 + dim.Y2) * 0.5f);
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"<text x=\"{mx:0.##}\" y=\"{my:0.##}\" text-anchor=\"middle\" font-size=\"8\" fill=\"#0066cc\">{Escape(dim.Text)}</text>");
    }

    sb.AppendLine("</svg>");
    return sb.ToString();
  }

  private static string Escape(string text) =>
    text.Replace("&", "&amp;", StringComparison.Ordinal)
      .Replace("<", "&lt;", StringComparison.Ordinal)
      .Replace(">", "&gt;", StringComparison.Ordinal);
}
