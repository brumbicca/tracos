using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Tracos3DStudio;

public partial class CutPlanWindow : Window
{
    private readonly Project _project;
    private readonly Action _onProjectChanged;
    private CutPlanSummary? _plan;

    private static readonly Color[] PieceColors =
    [
        Color.FromRgb(0xD8, 0xE4, 0xF8),
        Color.FromRgb(0xE8, 0xE0, 0xD0),
        Color.FromRgb(0xD4, 0xED, 0xDA),
        Color.FromRgb(0xF8, 0xE0, 0xD8),
        Color.FromRgb(0xE8, 0xD8, 0xF0)
    ];

    public CutPlanWindow(Project project, Action onProjectChanged)
    {
        InitializeComponent();
        _project = project;
        _onProjectChanged = onProjectChanged;

        SheetLengthBox.Text = project.Metadata.SheetLengthMm.ToString("0", CultureInfo.InvariantCulture);
        SheetWidthBox.Text = project.Metadata.SheetWidthMm.ToString("0", CultureInfo.InvariantCulture);
        Reload();
    }

    private void Reload()
    {
        _plan = CutPlanService.Build(_project);
        ProjectTitleText.Text = _plan.ProjectName;
        SummaryText.Text =
            $"{_plan.TotalSheets} chapa(s) · Aproveitamento médio: {_plan.OverallUtilizationPercent:0.0}% · " +
            $"Chapa padrão: {_plan.SheetLengthMm:0} × {_plan.SheetWidthMm:0} mm · Nesting: MaxRects";

        SheetsPanel.Children.Clear();

        foreach (var sheet in _plan.Sheets)
            SheetsPanel.Children.Add(BuildSheetView(sheet));
    }

    private UIElement BuildSheetView(CutSheet sheet)
    {
        const double canvasWidth = 520;
        const double canvasHeight = 360;
        float scale = (float)Math.Min(
            canvasWidth / sheet.SheetLengthMm,
            canvasHeight / sheet.SheetWidthMm);

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        panel.Children.Add(new TextBlock
        {
            Text = sheet.Title,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var canvas = new Canvas
        {
            Width = canvasWidth,
            Height = canvasHeight,
            Background = Brushes.WhiteSmoke
        };

        double sheetW = sheet.SheetLengthMm * scale;
        double sheetH = sheet.SheetWidthMm * scale;
        double offsetX = (canvasWidth - sheetW) / 2;
        double offsetY = (canvasHeight - sheetH) / 2;

        canvas.Children.Add(new Rectangle
        {
            Width = sheetW,
            Height = sheetH,
            Stroke = Brushes.Gray,
            StrokeThickness = 2,
            Fill = Brushes.White
        });
        Canvas.SetLeft(canvas.Children[^1], offsetX);
        Canvas.SetTop(canvas.Children[^1], offsetY);

        int colorIndex = 0;

        foreach (var placement in sheet.Placements)
        {
            var rect = new Rectangle
            {
                Width = placement.WidthMm * scale,
                Height = placement.HeightMm * scale,
                Fill = new SolidColorBrush(PieceColors[colorIndex % PieceColors.Length]),
                Stroke = Brushes.DimGray,
                StrokeThickness = 1,
                ToolTip = placement.Piece.Label
            };

            Canvas.SetLeft(rect, offsetX + placement.X * scale);
            Canvas.SetTop(rect, offsetY + placement.Y * scale);
            canvas.Children.Add(rect);

            var label = new TextBlock
            {
                Text = placement.Piece.PieceName,
                FontSize = 8,
                Width = placement.WidthMm * scale,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            Canvas.SetLeft(label, offsetX + placement.X * scale);
            Canvas.SetTop(label, offsetY + placement.Y * scale + placement.HeightMm * scale * 0.4);
            canvas.Children.Add(label);

            colorIndex++;
        }

        panel.Children.Add(canvas);
        return panel;
    }

    private void Recalculate_Click(object sender, RoutedEventArgs e)
    {
        if (!float.TryParse(SheetLengthBox.Text.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out float length) ||
            !float.TryParse(SheetWidthBox.Text.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out float width) ||
            length <= 0 || width <= 0)
        {
            MessageBox.Show("Informe comprimento e largura da chapa em mm.", "Plano de corte",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _project.Metadata.SheetLengthMm = length;
        _project.Metadata.SheetWidthMm = width;
        _onProjectChanged();
        Reload();
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_plan == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"{_project.Metadata.Name}-plano-corte.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        CutPlanCsvExporter.Export(_plan, dialog.FileName);

        MessageBox.Show(
            "CSV exportado com sucesso.",
            "Plano de corte",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportCncDrillCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV furos CNC (*.csv)|*.csv",
            FileName = $"{_project.Metadata.Name}-furos-cnc.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        int drillRows = MachineCutPlanExportService.CountDrillRows(_project);
        MachineCutPlanExportService.ExportDrillCsv(_project, dialog.FileName);

        MessageBox.Show(
            $"CSV exportado — {drillRows} furo(s) com coordenadas na chapa.",
            "Plano de corte",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportCncJobJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON CNC (*.json)|*.json",
            FileName = $"{_project.Metadata.Name}-cnc-job.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var (cutOps, drillOps) = MachineCutPlanExportService.CountCncJobOperations(_project);
        MachineCutPlanExportService.ExportCncJob(_project, dialog.FileName);

        MessageBox.Show(
            $"JSON CNC exportado — {cutOps} corte(s), {drillOps} furo(s).",
            "Plano de corte",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportJaraguaTap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "G-code Mach4 (*.tap)|*.tap",
            FileName = $"{_project.Metadata.Name}.tap"
        };

        if (dialog.ShowDialog() != true)
            return;

        var (cutOps, drillOps, sheets) = MachineCutPlanExportService.CountJaraguaTapOperations(_project);
        MachineCutPlanExportService.ExportJaraguaTap(_project, dialog.FileName);

        string sheetNote = sheets > 1 ? $" ({sheets} arquivos *-chapa-NN.tap)" : string.Empty;

        MessageBox.Show(
            $"G-code Jaraguá exportado — {cutOps} corte(s), {drillOps} furo(s){sheetNote}.",
            "Plano de corte",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
