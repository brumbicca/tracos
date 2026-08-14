using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Tracos3DStudio;

public partial class WallLayersWindow : Window
{
    private readonly Project _project;
    private readonly Action _onLayersChanged;
    private readonly List<CheckBox> _visibilityCheckBoxes = new();
    private readonly List<CheckBox> _lockCheckBoxes = new();
    private readonly List<ComboBox> _fillModeComboBoxes = new();

    public WallLayersWindow(Project project, Action onLayersChanged)
    {
        InitializeComponent();
        _project = project;
        _onLayersChanged = onLayersChanged;

        ProjectTitleText.Text = project.Metadata.Name;
        RefreshLayerList();
    }

    private void RefreshLayerList()
    {
        LayersPanel.Children.Clear();
        _visibilityCheckBoxes.Clear();
        _lockCheckBoxes.Clear();
        _fillModeComboBoxes.Clear();

        var walls = _project.Room.Walls;
        var modules = _project.Modules;
        int totalWalls = walls.Count;
        int totalModules = modules.Count;

        SummaryText.Text = totalWalls == 0 && totalModules == 0
            ? "Nenhum item no projeto. Marque visibilidade, preenchimento e bloqueio por camada quando houver paredes ou módulos."
            : $"{totalWalls} parede(s), {totalModules} módulo(s) — desmarque para ocultar; altere o preenchimento ou bloqueie para impedir seleção.";

        foreach (var layer in WallLayerCatalog.GetDefinitions(_project.Metadata))
        {
            int wallCount = WallLayerCatalog.CountWallsOnLayer(walls, layer.Id);
            int moduleCount = WallLayerCatalog.CountModulesOnLayer(modules, layer.Id);

            var row = new Grid { Margin = new Thickness(4, 6, 4, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var visibilityCheckBox = new CheckBox
            {
                IsChecked = layer.IsVisible,
                Tag = layer.Id,
                VerticalAlignment = VerticalAlignment.Center
            };
            visibilityCheckBox.SetValue(AutomationProperties.AutomationIdProperty, $"WallLayerVisibleCheck_{layer.Id}");
            visibilityCheckBox.Click += LayerVisibility_Click;
            _visibilityCheckBoxes.Add(visibilityCheckBox);

            var label = new TextBlock
            {
                Text = $"{layer.DisplayName} ({wallCount} parede(s), {moduleCount} módulo(s))",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            var fillModeCombo = new ComboBox
            {
                Tag = layer.Id,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 28
            };
            fillModeCombo.SetValue(AutomationProperties.NameProperty, $"Preenchimento {layer.DisplayName}");
            fillModeCombo.SetValue(AutomationProperties.AutomationIdProperty, $"WallLayerFillModeCombo_{layer.Id}");

            foreach (var (mode, displayName) in LayerFillModeCatalog.GetOptions())
                fillModeCombo.Items.Add(new LayerFillModeItem(mode, displayName));

            fillModeCombo.SelectedItem = fillModeCombo.Items
                .Cast<LayerFillModeItem>()
                .FirstOrDefault(item => item.Mode == layer.FillMode)
                ?? fillModeCombo.Items[0];

            fillModeCombo.SelectionChanged += LayerFillMode_SelectionChanged;
            _fillModeComboBoxes.Add(fillModeCombo);

            var lockCheckBox = new CheckBox
            {
                Content = "Bloqueada",
                IsChecked = layer.IsLocked,
                Tag = layer.Id,
                VerticalAlignment = VerticalAlignment.Center
            };
            lockCheckBox.SetValue(AutomationProperties.AutomationIdProperty, $"WallLayerLockedCheck_{layer.Id}");
            lockCheckBox.Click += LayerLock_Click;
            _lockCheckBoxes.Add(lockCheckBox);

            row.Children.Add(visibilityCheckBox);
            Grid.SetColumn(label, 1);
            row.Children.Add(label);
            Grid.SetColumn(fillModeCombo, 2);
            row.Children.Add(fillModeCombo);
            Grid.SetColumn(lockCheckBox, 3);
            row.Children.Add(lockCheckBox);

            LayersPanel.Children.Add(row);
        }
    }

    private void LayerVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.Tag is not string layerId)
            return;

        bool visible = checkBox.IsChecked == true;
        WallLayerCatalog.SetLayerVisible(_project.Metadata, layerId, visible);
        _onLayersChanged();
    }

    private void LayerFillMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox ||
            comboBox.Tag is not string layerId ||
            comboBox.SelectedItem is not LayerFillModeItem selected)
            return;

        WallLayerCatalog.SetLayerFillMode(_project.Metadata, layerId, selected.Mode);
        _onLayersChanged();
    }

    private void LayerLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.Tag is not string layerId)
            return;

        bool locked = checkBox.IsChecked == true;
        WallLayerCatalog.SetLayerLocked(_project.Metadata, layerId, locked);
        _onLayersChanged();
    }

    private void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        string name = NewLayerNameBox.Text;

        if (!WallLayerCatalog.TryAddCustomLayer(_project.Metadata, name, out _, out string? error))
        {
            MessageBox.Show(
                error ?? "Não foi possível adicionar a camada.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        NewLayerNameBox.Text = "";
        RefreshLayerList();
        _onLayersChanged();
    }

    private void ShowAll_Click(object sender, RoutedEventArgs e)
    {
        WallLayerCatalog.SetAllLayersVisible(_project.Metadata, true);
        foreach (var checkBox in _visibilityCheckBoxes)
            checkBox.IsChecked = true;

        _onLayersChanged();
    }

    private void HideAll_Click(object sender, RoutedEventArgs e)
    {
        WallLayerCatalog.SetAllLayersVisible(_project.Metadata, false);
        foreach (var checkBox in _visibilityCheckBoxes)
            checkBox.IsChecked = false;

        _onLayersChanged();
    }

    private void RemoveEmptyLayers_Click(object sender, RoutedEventArgs e)
    {
        var emptyLayers = WallLayerCatalog.GetEmptyCustomLayers(
            _project.Metadata,
            _project.Room.Walls,
            _project.Modules);

        if (emptyLayers.Count == 0)
        {
            MessageBox.Show(
                "Não há camadas customizadas vazias para remover.",
                "Traços 3D Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var message = new StringBuilder();
        message.AppendLine("As seguintes camadas customizadas não têm paredes nem módulos e serão removidas:");
        message.AppendLine();

        foreach (var layer in emptyLayers)
            message.AppendLine($"• {layer.DisplayName}");

        message.AppendLine();
        message.Append("Deseja continuar?");

        MessageBoxResult confirm = MessageBox.Show(
            message.ToString(),
            "Traços 3D Studio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        WallLayerCatalog.TryRemoveEmptyCustomLayers(
            _project.Metadata,
            _project.Room.Walls,
            _project.Modules,
            out _);

        RefreshLayerList();
        _onLayersChanged();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed record LayerFillModeItem(LayerFillMode Mode, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
