using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tracos3DStudio;

public static class ModuleCatalogThumbnail
{
    private static readonly Color BodyFill = Color.FromRgb(0xD8, 0xD8, 0xD8);
    private static readonly Color DoorFill = Color.FromRgb(0xB0, 0x85, 0x55);
    private static readonly Color BlindFill = Color.FromRgb(0xC8, 0xC8, 0xC8);
    private static readonly Color GolaFill = Color.FromRgb(0x6A, 0x6A, 0x6A);
    private static readonly Color Stroke = Color.FromRgb(0x55, 0x55, 0x55);

    public static Color GetAccentColor(ModuleDefinition definition) =>
        definition.Category switch
        {
            ModuleCategory.Dormitorio => Color.FromRgb(0x8B, 0x6F, 0x47),
            ModuleCategory.Paineis => Color.FromRgb(0x6E, 0x6E, 0x6E),
            _ => Color.FromRgb(0x5C, 0x8A, 0xB8)
        };

    public static SolidColorBrush GetAccentBrush(ModuleDefinition definition) =>
        new(GetAccentColor(definition));

    public static string GetIconHint(ModuleDefinition definition)
    {
        if (definition.IsDecorativePanel)
        {
            return definition.Id.ToLowerInvariant() switch
            {
                "painel-canaletado" => "C",
                "painel-ripado" => "R",
                _ => "P"
            };
        }

        if (definition.IsWallMounted)
            return "A";

        return definition.ShapeKind switch
        {
            ModuleShapeKind.BlindCornerLeft or ModuleShapeKind.BlindCornerRight => "CR",
            ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight => "L",
            ModuleShapeKind.Oblique => "Ob",
            ModuleShapeKind.CurvedFront => "Cv",
            ModuleShapeKind.Bifold => "Bf",
            ModuleShapeKind.ColumnDoors => "Col",
            ModuleShapeKind.PullOutNarrow => "Ex",
            ModuleShapeKind.WineRack => "Ad",
            ModuleShapeKind.ApplianceBay => "El",
            ModuleShapeKind.EndDiagonal => "Dg",
            ModuleShapeKind.EndCurved => "Cu",
            ModuleShapeKind.EndChamfer => "Ch",
            ModuleShapeKind.EndZ => "Z",
            ModuleShapeKind.OpenCornerShelves => "Ct",
            ModuleShapeKind.Filler => "F",
            _ when definition.DrawerCount > 0 && definition.DoorCount == 0 => $"{definition.DrawerCount}G",
            _ when definition.DoorCount > 0 => $"{definition.DoorCount}P",
            _ => "M"
        };
    }

    public static UIElement BuildIcon(ModuleDefinition definition, double size = 36)
    {
        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(2),
            BorderBrush = new SolidColorBrush(Stroke),
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            ToolTip = definition.DisplayName,
            ClipToBounds = true
        };

        border.Child = TryBuildPlanPreview(definition, size) ?? BuildLetterFallback(definition, size);
        return border;
    }

    public static UIElement BuildInsertButtonContent(ModuleDefinition definition)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        panel.Children.Add(BuildIcon(definition, 40));

        panel.Children.Add(new TextBlock
        {
            Text = definition.DisplayName,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap
        });

        return panel;
    }

    private static UIElement BuildLetterFallback(ModuleDefinition definition, double size) =>
        new Grid
        {
            Background = GetAccentBrush(definition),
            Children =
            {
                new TextBlock
                {
                    Text = GetIconHint(definition),
                    Foreground = Brushes.White,
                    FontSize = size >= 32 ? 11 : 9,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

    private static UIElement? TryBuildPlanPreview(ModuleDefinition definition, double size) =>
        definition.ShapeKind switch
        {
            ModuleShapeKind.BlindCornerLeft => BuildBlindPlan(size, leftHand: true, definition.DoorCount),
            ModuleShapeKind.BlindCornerRight => BuildBlindPlan(size, leftHand: false, definition.DoorCount),
            ModuleShapeKind.CornerLLeft => BuildLPlan(size, leftHand: true, definition.DoorCount),
            ModuleShapeKind.CornerLRight => BuildLPlan(size, leftHand: false, definition.DoorCount),
            ModuleShapeKind.Oblique => BuildObliquePlan(size, definition.DoorCount),
            ModuleShapeKind.CornerDrawer => BuildObliquePlan(size, Math.Max(1, definition.DrawerCount)),
            ModuleShapeKind.CornerCurved => BuildObliquePlan(size, definition.DoorCount),
            ModuleShapeKind.Bifold when definition.Id
                .StartsWith("canto-bifold-l-", StringComparison.OrdinalIgnoreCase)
                => BuildLPlan(size,
                    leftHand: definition.Id.Contains("esq", StringComparison.OrdinalIgnoreCase),
                    doors: Math.Max(2, definition.DoorCount)),
            _ => null
        };

    private static Canvas BuildBlindPlan(double size, bool leftHand, int doors)
    {
        var canvas = NewCanvas(size);
        double m = size * 0.08;
        double bodyW = size - 2 * m;
        double bodyH = size - 2 * m;
        double blindW = bodyW * 0.38;
        double golaW = bodyW * 0.06;
        double doorW = bodyW - blindW - golaW;

        // Corpo
        canvas.Children.Add(Rect(m, m, bodyW, bodyH, BodyFill));

        if (leftHand)
        {
            // CR Esq: porta | fechamento | (falsa atrás)
            AddDoorStripes(canvas, m, m, doorW, bodyH, doors);
            canvas.Children.Add(Rect(m + doorW, m, golaW, bodyH, GolaFill));
            canvas.Children.Add(Rect(m + doorW + golaW, m, blindW, bodyH, BlindFill));
        }
        else
        {
            // CR Dir: (falsa) | fechamento | porta
            canvas.Children.Add(Rect(m, m, blindW, bodyH, BlindFill));
            canvas.Children.Add(Rect(m + blindW, m, golaW, bodyH, GolaFill));
            AddDoorStripes(canvas, m + blindW + golaW, m, doorW, bodyH, doors);
        }

        return canvas;
    }

    private static Canvas BuildLPlan(double size, bool leftHand, int doors)
    {
        var canvas = NewCanvas(size);
        double m = size * 0.08;
        double s = size - 2 * m;
        double thick = s * 0.42;

        if (leftHand)
        {
            // Perna horizontal (baixo) + vertical (esquerda)
            canvas.Children.Add(Rect(m, m + s - thick, s, thick, BodyFill));
            canvas.Children.Add(Rect(m, m, thick, s - thick, BodyFill));
            // Portas no interior do L
            canvas.Children.Add(Rect(m + thick * 0.15, m + s - thick, s - thick * 0.2, thick * 0.35, DoorFill));
            if (doors >= 2)
                canvas.Children.Add(Rect(m + thick * 0.65, m + thick * 0.1, thick * 0.35, s - thick * 1.1, DoorFill));
        }
        else
        {
            canvas.Children.Add(Rect(m, m + s - thick, s, thick, BodyFill));
            canvas.Children.Add(Rect(m + s - thick, m, thick, s - thick, BodyFill));
            canvas.Children.Add(Rect(m + thick * 0.05, m + s - thick, s - thick * 0.2, thick * 0.35, DoorFill));
            if (doors >= 2)
                canvas.Children.Add(Rect(m + s - thick, m + thick * 0.1, thick * 0.35, s - thick * 1.1, DoorFill));
        }

        return canvas;
    }

    private static Canvas BuildObliquePlan(double size, int doors)
    {
        var canvas = NewCanvas(size);
        double m = size * 0.1;
        double s = size - 2 * m;

        var body = new Polygon
        {
            Fill = new SolidColorBrush(BodyFill),
            Stroke = new SolidColorBrush(Stroke),
            StrokeThickness = 0.8,
            Points =
            [
                new Point(m, m),
                new Point(m + s, m),
                new Point(m + s, m + s * 0.55),
                new Point(m + s * 0.55, m + s),
                new Point(m, m + s)
            ]
        };
        canvas.Children.Add(body);

        // Frente diagonal
        var door = new Polygon
        {
            Fill = new SolidColorBrush(DoorFill),
            Stroke = new SolidColorBrush(Stroke),
            StrokeThickness = 0.6,
            Points =
            [
                new Point(m + s * 0.12, m + s * 0.62),
                new Point(m + s * 0.88, m + s * 0.48),
                new Point(m + s * 0.55, m + s * 0.92),
                new Point(m + s * 0.18, m + s * 0.92)
            ]
        };
        canvas.Children.Add(door);

        if (doors >= 2)
        {
            canvas.Children.Add(new Line
            {
                X1 = m + s * 0.48,
                Y1 = m + s * 0.55,
                X2 = m + s * 0.36,
                Y2 = m + s * 0.92,
                Stroke = new SolidColorBrush(Stroke),
                StrokeThickness = 1
            });
        }

        return canvas;
    }

    private static void AddDoorStripes(Canvas canvas, double x, double y, double w, double h, int doors)
    {
        int n = Math.Max(1, doors);
        double seg = w / n;
        for (int i = 0; i < n; i++)
            canvas.Children.Add(Rect(x + i * seg + 0.5, y, seg - 1, h, DoorFill));
    }

    private static Canvas NewCanvas(double size) =>
        new()
        {
            Width = size,
            Height = size,
            Background = Brushes.White,
            SnapsToDevicePixels = true
        };

    private static Rectangle Rect(double x, double y, double w, double h, Color fill)
    {
        var rect = new Rectangle
        {
            Width = Math.Max(1, w),
            Height = Math.Max(1, h),
            Fill = new SolidColorBrush(fill),
            Stroke = new SolidColorBrush(Stroke),
            StrokeThickness = 0.6
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        return rect;
    }
}
