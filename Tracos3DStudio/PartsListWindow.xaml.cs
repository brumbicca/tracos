using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Tracos3DStudio;

public partial class PartsListWindow : Window
{
    private readonly Project _project;
    private readonly Action _onProjectChanged;

    public PartsListWindow(Project project, Action onProjectChanged)
    {
        InitializeComponent();
        _project = project;
        _onProjectChanged = onProjectChanged;

        PanelThicknessCombo.SelectionChanged += PanelThicknessCombo_SelectionChanged;
        SelectThickness(project.Metadata.PanelThicknessMm);
        Reload();
    }

    private void SelectThickness(float thickness)
    {
        foreach (ComboBoxItem item in PanelThicknessCombo.Items)
        {
            if (item.Content?.ToString() == thickness.ToString("0", CultureInfo.InvariantCulture))
            {
                PanelThicknessCombo.SelectedItem = item;
                return;
            }
        }

        PanelThicknessCombo.SelectedIndex = 1;
    }

    private void Reload()
    {
        var summary = PartsListService.Build(_project);
        ProjectTitleText.Text = summary.ProjectName;
        PartsGrid.ItemsSource = summary.Items;
        PieceCountText.Text = $"{summary.TotalPieceCount} peças (espessura {summary.PanelThicknessMm:0} mm)";
    }

    private void PanelThicknessCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelThicknessCombo.SelectedItem is not ComboBoxItem item)
            return;

        if (!float.TryParse(item.Content?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float thickness))
            return;

        if (Math.Abs(_project.Metadata.PanelThicknessMm - thickness) < 0.1f)
            return;

        _project.Metadata.PanelThicknessMm = thickness;
        _onProjectChanged();
        Reload();
    }

    private void ExportLabelsPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{_project.Metadata.Name}-etiquetas.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var labels = PartLabelsService.Build(_project);
        PartLabelsPdfExporter.Export(labels, dialog.FileName);

        MessageBox.Show(
            $"{labels.TotalCount} etiquetas exportadas com sucesso.",
            "Lista de peças",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportTechnicalPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{_project.Metadata.Name}-tecnico.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var parts = PartsListService.Build(_project);
        var drawing = TechnicalDrawingService.Build(_project);
        TechnicalPdfExporter.Export(_project, parts, drawing, dialog.FileName);

        MessageBox.Show(
            "PDF técnico exportado com sucesso.",
            "Lista de peças",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportDxf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "DXF (*.dxf)|*.dxf",
            FileName = $"{_project.Metadata.Name}-planta.dxf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var drawing = TechnicalDrawingService.Build(_project);
        DxfExporter.ExportFloorPlan(drawing, dialog.FileName);

        MessageBox.Show(
            "DXF da planta exportado com sucesso.",
            "Lista de peças",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExportDxfPieces_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "DXF (*.dxf)|*.dxf",
            FileName = $"{_project.Metadata.Name}-pecas.dxf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var parts = PartsListService.Build(_project);
        DxfExporter.ExportPieces(parts, dialog.FileName);

        MessageBox.Show(
            "DXF das peças exportado com sucesso.",
            "Lista de peças",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
