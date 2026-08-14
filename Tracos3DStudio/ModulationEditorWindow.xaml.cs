using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Tracos3DStudio;

public partial class ModulationEditorWindow : Window
{
    private readonly CustomModuleData _module;
    private readonly ModulationEditorState _state;

    public ModulationEditorWindow(CustomModuleData module)
    {
        InitializeComponent();
        _module = module;
        _state = ModulationEditorState.FromModule(module);
        LoadUiFromState();
    }

    public ModulationRules? ResultRules { get; private set; }

    private void LoadUiFromState()
    {
        ModuleTitleText.Text = string.IsNullOrWhiteSpace(_module.DisplayName)
            ? _module.Id
            : _module.DisplayName;
        ModuleInfoText.Text =
            $"ID: {_module.Id} · {FormatMm(_module.DefaultWidth)} × {FormatMm(_module.DefaultHeight)} × {FormatMm(_module.DefaultDepth)} mm";

        PanelThicknessBox.Text = FormatNumber(_state.PanelThicknessMm);
        BackThicknessBox.Text = FormatNumber(_state.BackThicknessMm);
        FrontThicknessBox.Text = FormatNumber(_state.FrontThicknessMm);
        FrontGapBox.Text = FormatNumber(_state.FrontGapMm);
        DoorCountBox.Text = _state.DoorCount.ToString(CultureInfo.InvariantCulture);
        DrawerCountBox.Text = _state.DrawerCount.ToString(CultureInfo.InvariantCulture);
        IncludeShelfCheck.IsChecked = _state.IncludeShelf;
        ShelfHeightBox.Text = FormatPercent(_state.ShelfHeightFraction);

        RefreshPreview();
    }

    private bool TryReadStateFromUi(out string? error)
    {
        error = null;

        if (!TryParseFloat(PanelThicknessBox.Text, out float panel) ||
            !TryParseFloat(BackThicknessBox.Text, out float back) ||
            !TryParseFloat(FrontThicknessBox.Text, out float front) ||
            !TryParseFloat(FrontGapBox.Text, out float gap))
        {
            error = "Informe espessuras e folgas numéricas válidas (mm).";
            return false;
        }

        if (!int.TryParse(DoorCountBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int doors) ||
            !int.TryParse(DrawerCountBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int drawers))
        {
            error = "Portas e gavetas devem ser números inteiros.";
            return false;
        }

        if (!TryParsePercent(ShelfHeightBox.Text, out float shelfHeight))
        {
            error = "Altura da prateleira deve ser uma porcentagem entre 5 e 95.";
            return false;
        }

        _state.PanelThicknessMm = panel;
        _state.BackThicknessMm = back;
        _state.FrontThicknessMm = front;
        _state.FrontGapMm = gap;
        _state.DoorCount = doors;
        _state.DrawerCount = drawers;
        _state.IncludeShelf = IncludeShelfCheck.IsChecked == true;
        _state.ShelfHeightFraction = shelfHeight;
        _state.NormalizeCounts();

        DoorCountBox.Text = _state.DoorCount.ToString(CultureInfo.InvariantCulture);
        DrawerCountBox.Text = _state.DrawerCount.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private void RefreshPreview()
    {
        if (!TryReadStateFromUi(out _))
            return;

        var rules = ModulationRulesBuilder.BuildFromEditorState(_state);
        FrontBaysList.ItemsSource = rules.Structure.FrontBays
            .Select(bay => new FrontBayRow(bay))
            .ToList();

        PreviewSummaryText.Text = _state.DrawerCount > 0
            ? $"Template caixa com {_state.DrawerCount} gaveta(s) empilhadas."
            : $"Template caixa com {_state.DoorCount} porta(s)" +
              (_state.IncludeShelf ? " e prateleira interna." : ".");

        PiecesSummaryText.Text =
            $"Peças geradas: {rules.Pieces.Count} regra(s) — " +
            string.Join(", ", rules.Pieces.GroupBy(p => p.Role).Select(g => $"{g.Key}×{g.Sum(x => x.Quantity)}"));
    }

    private void PreviewRules_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadStateFromUi(out string? error))
        {
            MessageBox.Show(error, "Editor de modulação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        RefreshPreview();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadStateFromUi(out string? error))
        {
            MessageBox.Show(error, "Editor de modulação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ModulationRulesBuilder.ApplyToModule(_module, _state);
        ResultRules = _module.ModulationRules;
        DialogResult = true;
        Close();
    }

    private static string FormatMm(float value) => value.ToString("0", CultureInfo.InvariantCulture);

    private static string FormatNumber(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatPercent(float fraction) =>
        (fraction * 100f).ToString("0", CultureInfo.InvariantCulture);

    private static bool TryParseFloat(string text, out float value) =>
        float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static bool TryParsePercent(string text, out float fraction)
    {
        fraction = 0.5f;
        if (!TryParseFloat(text, out float parsed))
            return false;

        if (parsed > 1f)
            parsed /= 100f;

        if (parsed < 0.05f || parsed > 0.95f)
            return false;

        fraction = parsed;
        return true;
    }

    private sealed class FrontBayRow(ModulationFrontBay bay)
    {
        public string Label =>
            $"{bay.Id} · {DescribeType(bay.Type)} · L {(bay.WidthFraction * 100f):0}% × A {(bay.HeightFraction * 100f):0}%";

        private static string DescribeType(ModulationFrontType type) => type switch
        {
            ModulationFrontType.Door => "Porta",
            ModulationFrontType.Drawer => "Gaveta",
            ModulationFrontType.Open => "Vão aberto",
            _ => type.ToString()
        };
    }
}
