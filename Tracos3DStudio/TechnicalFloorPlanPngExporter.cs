using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tracos3DStudio;

public static class TechnicalFloorPlanPngExporter
{
    private const double Margin = 48;
    private const double Header = 28;
    private const double Scale = 0.12;

    public static void Export(TechnicalDrawingSet drawing, string filePath, string projectName)
    {
        double contentWidth = Math.Max(1, drawing.MaxX - drawing.MinX);
        double contentHeight = Math.Max(1, drawing.MaxY - drawing.MinY);
        int width = (int)Math.Ceiling(contentWidth * Scale + Margin * 2);
        int height = (int)Math.Ceiling(contentHeight * Scale + Margin * 2 + Header);

        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            dc.DrawText(
                new FormattedText(
                    $"Planta — {projectName}",
                    CultureInfo.GetCultureInfo("pt-BR"),
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    Brushes.Black,
                    1.0),
                new Point(Margin, 12));

            foreach (var wall in drawing.FloorPlanWalls)
                DrawLine(dc, drawing, wall.X1, wall.Y1, wall.X2, wall.Y2, Brushes.Black, 2);

            foreach (var module in drawing.FloorPlanModules)
            {
                var rect = MapRect(drawing, module.X, module.Y, module.Width, module.Height);
                dc.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(0xD8, 0xE4, 0xF8)),
                    new Pen(Brushes.DimGray, 1),
                    rect);
            }

            foreach (var dim in drawing.FloorPlanDimensions)
            {
                DrawLine(dc, drawing, dim.X1, dim.Y1, dim.X2, dim.Y2, Brushes.SteelBlue, 1);
                var center = MapPoint(drawing, (dim.X1 + dim.X2) / 2f, (dim.Y1 + dim.Y2) / 2f);
                dc.DrawText(
                    new FormattedText(
                        dim.Text,
                        CultureInfo.GetCultureInfo("pt-BR"),
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        10,
                        Brushes.SteelBlue,
                        1.0),
                    new Point(center.X - 16, center.Y - 6));
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private static void DrawLine(
        DrawingContext dc,
        TechnicalDrawingSet drawing,
        float x1,
        float y1,
        float x2,
        float y2,
        Brush brush,
        double thickness)
    {
        var p1 = MapPoint(drawing, x1, y1);
        var p2 = MapPoint(drawing, x2, y2);
        dc.DrawLine(new Pen(brush, thickness), p1, p2);
    }

    private static Point MapPoint(TechnicalDrawingSet drawing, float x, float y) =>
        new(
            Margin + (x - drawing.MinX) * Scale,
            Margin + Header + (drawing.MaxY - y) * Scale);

    private static Rect MapRect(TechnicalDrawingSet drawing, float x, float y, float w, float h)
    {
        var topLeft = MapPoint(drawing, x, y + h);
        return new Rect(topLeft.X, topLeft.Y, w * Scale, h * Scale);
    }
}
