using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tracos3DStudio;

public partial class MaterialsPanel : UserControl
{
    private Project? _project;
    private Func<MaterialApplicationContext>? _getContext;
    private Action<string, MaterialApplicationTarget, string?>? _onMaterialSelected;
    private Action? _onBeginCopyMode;
    private bool _syncingSelection;
    private Point _dragStartPoint;
    private bool _isDragging;

    public MaterialsPanel()
    {
        InitializeComponent();
        FilterCombo.SelectedIndex = 0;
        MaterialsListBox.PreviewMouseLeftButtonDown += MaterialsListBox_PreviewMouseLeftButtonDown;
    }

    public void Bind(
        Project project,
        Func<MaterialApplicationContext> getContext,
        Action<string, MaterialApplicationTarget, string?> onMaterialSelected,
        Action onBeginCopyMode)
    {
        _project = project;
        _getContext = getContext;
        _onMaterialSelected = onMaterialSelected;
        _onBeginCopyMode = onBeginCopyMode;
        RefreshFromProject();
    }

    public void RefreshFromProject()
    {
        if (_project == null)
            return;

        ProjectTitleText.Text = _project.Metadata.Name;
        SelectModeCombo(MaterialApplicationService.ApplicationMode);
        RefreshMaterialList();
        UpdateSummary();
    }

    private MaterialApplicationContext CurrentContext =>
        _getContext?.Invoke() ?? new MaterialApplicationContext();

    private MaterialListFilter CurrentFilter =>
        FilterCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag
            ? tag switch
            {
                "Modules" => MaterialListFilter.Modules,
                "Floors" => MaterialListFilter.Floors,
                _ => MaterialListFilter.All
            }
            : MaterialListFilter.All;

    private static MaterialApplicationMode ParseModeTag(string? tag) =>
        tag switch
        {
            "Module" => MaterialApplicationMode.Module,
            "WallFace" => MaterialApplicationMode.WallFace,
            "WallBand" => MaterialApplicationMode.WallBand,
            "WallRegion" => MaterialApplicationMode.WallRegion,
            "Floor" => MaterialApplicationMode.Floor,
            "FloorZone" => MaterialApplicationMode.FloorZone,
            _ => MaterialApplicationMode.Auto
        };

    private void SelectModeCombo(MaterialApplicationMode mode)
    {
        for (int i = 0; i < ModeCombo.Items.Count; i++)
        {
            if (ModeCombo.Items[i] is ComboBoxItem item &&
                item.Tag is string tag &&
                ParseModeTag(tag) == mode)
            {
                ModeCombo.SelectedIndex = i;
                return;
            }
        }

        ModeCombo.SelectedIndex = 0;
    }

    private void RefreshMaterialList()
    {
        _syncingSelection = true;

        var options = MaterialApplicationService.GetFilteredOptions(CurrentFilter);
        MaterialsListBox.Items.Clear();

        foreach (var option in options)
            MaterialsListBox.Items.Add(BuildMaterialRow(option));

        SelectActiveMaterialInList();
        _syncingSelection = false;
    }

    private UIElement BuildMaterialRow(WallSurfaceMaterialOption option)
    {
        var grid = new Grid
        {
            Margin = new Thickness(4, 6, 4, 6),
            Tag = option
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var swatch = new Rectangle
        {
            Width = 24,
            Height = 24,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            RadiusX = 2,
            RadiusY = 2,
            Fill = CreateBrush(option.ColorHex)
        };

        var label = new TextBlock
        {
            Text = option.DisplayName,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        grid.Children.Add(swatch);
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        grid.SetValue(AutomationProperties.AutomationIdProperty, $"MaterialItem_{option.Id}");
        grid.SetValue(AutomationProperties.NameProperty, option.DisplayName);
        grid.MouseMove += MaterialRow_MouseMove;
        grid.PreviewMouseLeftButtonDown += MaterialRow_PreviewMouseLeftButtonDown;

        return grid;
    }

    private void MaterialsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(MaterialsListBox);
        _isDragging = false;
    }

    private void MaterialRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(MaterialsListBox);
        _isDragging = false;
    }

    private void MaterialRow_MouseMove(object sender, MouseEventArgs e) => TryStartDrag(e);

    private void TryStartDrag(MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
            return;

        Point position = e.GetPosition(MaterialsListBox);
        Vector delta = position - _dragStartPoint;

        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        Grid? grid = e.OriginalSource as Grid;

        if (grid?.Tag is not WallSurfaceMaterialOption option)
        {
            if (e.OriginalSource is DependencyObject dep)
                grid = FindAncestorGridWithMaterial(dep);

            if (grid?.Tag is not WallSurfaceMaterialOption option2)
                return;

            option = option2;
        }

        _isDragging = true;
        var data = new DataObject(MaterialDragFormats.MaterialId, option.Id);
        DragDropEffects effect = DragDrop.DoDragDrop(grid!, data, DragDropEffects.Copy);
        _isDragging = false;

        if (effect == DragDropEffects.Copy)
            StatusText.Text = $"Arraste concluído: {option.DisplayName}.";
    }

    private static Grid? FindAncestorGridWithMaterial(DependencyObject current)
    {
        while (current != null)
        {
            if (current is Grid grid && grid.Tag is WallSurfaceMaterialOption)
                return grid;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static SolidColorBrush CreateBrush(string colorHex)
    {
        var (r, g, b) = ColorParsing.ParseHexRgb(colorHex);
        return new SolidColorBrush(Color.FromRgb(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255)));
    }

    private void SelectActiveMaterialInList()
    {
        string activeId = MaterialApplicationService.ActiveMaterialId;

        for (int i = 0; i < MaterialsListBox.Items.Count; i++)
        {
            if (MaterialsListBox.Items[i] is Grid grid &&
                grid.Tag is WallSurfaceMaterialOption option &&
                option.Id.Equals(activeId, StringComparison.OrdinalIgnoreCase))
            {
                MaterialsListBox.SelectedIndex = i;
                return;
            }
        }

        if (MaterialsListBox.Items.Count > 0)
            MaterialsListBox.SelectedIndex = 0;
    }

    private void UpdateSummary()
    {
        int count = MaterialApplicationService.GetFilteredOptions(CurrentFilter).Count;
        string modeHint = MaterialApplicationService.ApplicationMode switch
        {
            MaterialApplicationMode.Module => "Modo Módulo: clique aplica ao módulo selecionado; arraste sobre módulo.",
            MaterialApplicationMode.WallFace => "Modo Face da parede: área livre da face; arraste na parede (ignora faixa/região).",
            MaterialApplicationMode.WallBand => "Modo Faixa: apenas faixas; arraste sobre faixa.",
            MaterialApplicationMode.WallRegion => "Modo Região: apenas regiões; arraste sobre região.",
            MaterialApplicationMode.Floor => "Modo Piso: base do piso; arraste no piso fora de regiões.",
            MaterialApplicationMode.FloorZone => "Modo Região do piso: arraste sobre região do piso.",
            _ => CurrentContext.HasApplyTarget
                ? "Clique para aplicar ao item selecionado no projeto."
                : "Selecione módulo, face, faixa, região ou piso para aplicar ao clicar."
        };

        SummaryText.Text =
            $"{count} material(is) — {modeHint} Use Copiar do selecionado ou Copiar no viewport (M3). Arraste para aplicar no cursor.";
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (ModeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            MaterialApplicationService.ApplicationMode = ParseModeTag(tag);

        UpdateSummary();
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        RefreshMaterialList();
        UpdateSummary();
    }

    private void MaterialsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || _project == null)
            return;

        if (MaterialsListBox.SelectedItem is not Grid grid || grid.Tag is not WallSurfaceMaterialOption option)
            return;

        if (!MaterialApplicationService.TryApplyMaterial(
                _project,
                CurrentContext,
                option.Id,
                out MaterialApplicationTarget target,
                out string? error))
        {
            StatusText.Text = error ?? "Não foi possível aplicar o material.";
            return;
        }

        StatusText.Text = target switch
        {
            MaterialApplicationTarget.Module => $"Aplicado ao módulo: {option.DisplayName}.",
            MaterialApplicationTarget.WallBand => $"Aplicado à faixa: {option.DisplayName}.",
            MaterialApplicationTarget.WallRegion => $"Aplicado à região: {option.DisplayName}.",
            MaterialApplicationTarget.WallFace => $"Aplicado à face da parede: {option.DisplayName}.",
            MaterialApplicationTarget.FloorZone => $"Aplicado à região do piso: {option.DisplayName}.",
            MaterialApplicationTarget.FloorBase => $"Aplicado ao piso: {option.DisplayName}.",
            _ => $"Material ativo: {option.DisplayName}."
        };

        _onMaterialSelected?.Invoke(option.Id, target, null);
    }

    private void MaterialsCopyFromSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null)
            return;

        if (!MaterialCopyService.TryCaptureToActive(
                _project,
                CurrentContext,
                out MaterialApplicationTarget source,
                out string? error))
        {
            StatusText.Text = error ?? "Não foi possível copiar o material.";
            return;
        }

        string name = WallSurfaceMaterialCatalog.GetDisplayName(MaterialApplicationService.ActiveMaterialId);
        StatusText.Text = $"Material copiado ({FormatSource(source)}): {name}. Selecione destino e clique na lista ou use Copiar no viewport.";
        SelectActiveMaterialInList();
        _onMaterialSelected?.Invoke(MaterialApplicationService.ActiveMaterialId, MaterialApplicationTarget.None, null);
    }

    private void MaterialsCopyViewportButton_Click(object sender, RoutedEventArgs e)
    {
        _onBeginCopyMode?.Invoke();
    }

    private static string FormatSource(MaterialApplicationTarget target) =>
        target switch
        {
            MaterialApplicationTarget.Module => "módulo",
            MaterialApplicationTarget.WallBand => "faixa",
            MaterialApplicationTarget.WallRegion => "região",
            MaterialApplicationTarget.WallFace => "face",
            MaterialApplicationTarget.FloorZone => "região do piso",
            MaterialApplicationTarget.FloorBase => "piso",
            _ => "item"
        };
}
