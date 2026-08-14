using System.Windows;
using System.Windows.Controls;

namespace Tracos3DStudio;

public partial class WallBandsWindow : Window
{
    private readonly WallSegment _wall;
    private readonly Action _onBandsChanged;
    private readonly Action _beginHorizontalBandPick;
    private readonly Action _beginVerticalBandPick;
    private readonly Action _openRegionsEditor;
    private readonly Action<Guid?> _onBandSelectionChanged;
    private bool _syncing;

    public WallBandsWindow(
        WallSegment wall,
        string projectName,
        IReadOnlyList<WallSurfaceMaterialOption> materials,
        Action onBandsChanged,
        Action beginHorizontalBandPick,
        Action beginVerticalBandPick,
        Action openRegionsEditor,
        Action<Guid?> onBandSelectionChanged)
    {
        InitializeComponent();
        _wall = wall;
        _onBandsChanged = onBandsChanged;
        _beginHorizontalBandPick = beginHorizontalBandPick;
        _beginVerticalBandPick = beginVerticalBandPick;
        _openRegionsEditor = openRegionsEditor;
        _onBandSelectionChanged = onBandSelectionChanged;

        WallTitleText.Text = projectName;
        MaterialCombo.ItemsSource = materials;
        RefreshFromWall();
    }

    public void RefreshFromWall()
    {
        float wallTop = MathF.Max(_wall.HeightStart, _wall.HeightEnd);
        WallInfoText.Text =
            $"Parede selecionada — comprimento {_wall.Length:0} mm · pé-direito {wallTop:0} mm · {_wall.Bands.Count} faixa(s)";

        var items = _wall.Bands
            .Select(b => new BandListItem(b.Id, WallBandService.FormatLabel(b)))
            .ToList();

        Guid? previousId = (BandsListBox.SelectedItem as BandListItem)?.Id;

        _syncing = true;
        BandsListBox.ItemsSource = items;
        BandsListBox.SelectedItem = previousId.HasValue
            ? items.FirstOrDefault(i => i.Id == previousId.Value) ?? items.FirstOrDefault()
            : items.FirstOrDefault();
        _syncing = false;

        UpdateSummary();
        SyncMaterialCombo();
        UpdateButtons();
    }

    public void SelectBand(Guid bandId)
    {
        if (BandsListBox.ItemsSource is not IEnumerable<BandListItem> items)
            return;

        _syncing = true;
        BandsListBox.SelectedItem = items.FirstOrDefault(i => i.Id == bandId);
        _syncing = false;
        SyncMaterialCombo();
    }

    private void UpdateSummary()
    {
        SummaryText.Text = _wall.Bands.Count == 0
            ? "Nenhuma faixa. Use os botões acima e clique duas vezes na face da parede no viewport (Esc cancela). Arraste linhas laranja para ajustar (10 mm)."
            : string.Join("\n", _wall.Bands.Select(WallBandService.FormatSummaryLine));
    }

    private void UpdateButtons()
    {
        bool hasSelection = BandsListBox.SelectedItem != null;
        RemoveBandButton.IsEnabled = hasSelection;
        MaterialCombo.IsEnabled = hasSelection;
    }

    private WallBand? GetSelectedBand()
    {
        if (BandsListBox.SelectedItem is not BandListItem selected)
            return null;

        return _wall.Bands.FirstOrDefault(b => b.Id == selected.Id);
    }

    private void SyncMaterialCombo()
    {
        var band = GetSelectedBand();

        _syncing = true;
        MaterialCombo.SelectedItem = band == null || string.IsNullOrWhiteSpace(band.MaterialId)
            ? null
            : MaterialCombo.ItemsSource?
                .Cast<WallSurfaceMaterialOption>()
                .FirstOrDefault(m => m.Id == band.MaterialId);
        _syncing = false;
    }

    private void BandsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;

        SyncMaterialCombo();
        UpdateButtons();
        _onBandSelectionChanged((BandsListBox.SelectedItem as BandListItem)?.Id);
    }

    private void MaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;

        var band = GetSelectedBand();

        if (band == null || MaterialCombo.SelectedItem is not WallSurfaceMaterialOption material)
            return;

        band.MaterialId = material.Id;
        StatusText.Text = $"Material da faixa: {material.DisplayName}.";
        UpdateSummary();
        _onBandsChanged();
    }

    private void AddHorizontalBandButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Clique a primeira altura na face da parede; segundo clique define a faixa horizontal.";
        _beginHorizontalBandPick();
    }

    private void AddVerticalBandButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Clique a primeira posição na face; segundo clique define a faixa vertical.";
        _beginVerticalBandPick();
    }

    private void EditRegionsButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Painel Regiões aberto à direita — edite regiões no viewport ou no expander Regiões.";
        _openRegionsEditor();
    }

    private void RemoveBandButton_Click(object sender, RoutedEventArgs e)
    {
        var band = GetSelectedBand();

        if (band == null)
            return;

        if (!WallBandService.TryRemoveBand(_wall, band.Id, out string? error))
        {
            StatusText.Text = error ?? "Não foi possível remover a faixa.";
            return;
        }

        StatusText.Text = "Faixa removida.";
        RefreshFromWall();
        _onBandsChanged();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed record BandListItem(Guid Id, string Label);
}
