using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Microsoft.Win32;

namespace Tracos3DStudio;

public partial class BudgetWindow : Window
{
    private readonly Project _project;
    private readonly Action _onProjectChanged;
    private readonly Func<byte[]?>? _captureViewportPng;
    private readonly ObservableCollection<BudgetRowViewModel> _rows = new();
    private BudgetAuditReport _audit;

    public BudgetWindow(
        Project project,
        Action onProjectChanged,
        Func<byte[]?>? captureViewportPng = null,
        BudgetAuditReport? initialAudit = null)
    {
        InitializeComponent();
        _project = project;
        _onProjectChanged = onProjectChanged;
        _captureViewportPng = captureViewportPng;
        _audit = initialAudit ?? BudgetAuditService.Run(project);

        BudgetGrid.ItemsSource = _rows;
        BudgetGrid.CellEditEnding += BudgetGrid_CellEditEnding;

        LoadClientFields();
        Reload();
    }

    private void LoadClientFields()
    {
        ClientMetadataFields.LoadClient(_project.Metadata, ClientFields);
        UpdateAuditPanel();
    }

    private void ClientFields_FieldsChanged(object? sender, EventArgs e)
    {
        ClientMetadataFields.SaveClient(_project.Metadata, ClientFields);
        _onProjectChanged();
        _audit = BudgetAuditService.Run(_project);
        UpdateAuditPanel();
    }

    private void Reload()
    {
        ProjectTitleText.Text = _project.Metadata.GetWorkDisplayName();
        _rows.Clear();

        var summary = BudgetService.Build(_project);

        foreach (var item in summary.Items)
            _rows.Add(new BudgetRowViewModel(item));

        _audit = BudgetAuditService.Run(_project);
        UpdateGrandTotal(summary);
        UpdateAuditPanel();
    }

    private void BudgetGrid_CellEditEnding(object? sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is not BudgetRowViewModel row)
            return;

        Dispatcher.BeginInvoke(() =>
        {
            _project.Metadata.CustomModulePrices ??= new Dictionary<string, decimal>();
            _project.Metadata.CustomModulePrices[row.ModuleId.ToString()] = row.BasePrice;
            _onProjectChanged();
            Reload();
        });
    }

    private void UpdateGrandTotal(BudgetSummary summary)
    {
        GrandTotalText.Text = BuildGrandTotalLabel(summary);
    }

    private static string BuildGrandTotalLabel(BudgetSummary summary)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        if (summary.DiscountAmount <= 0m)
        {
            return
                $"Total: {summary.FinalTotal.ToString("C2", culture)}   |   PDF completo: {summary.FinalTotal.ToString("C2", culture)}";
        }

        return
            $"Subtotal: {summary.Subtotal.ToString("C2", culture)}   |   " +
            $"Desconto {summary.BudgetDiscountPercent:0.#}%: −{summary.DiscountAmount.ToString("C2", culture)}   |   " +
            $"Total: {summary.FinalTotal.ToString("C2", culture)}";
    }

    private void UpdateAuditPanel()
    {
        if (_audit.IsClean)
        {
            AuditPanel.Visibility = Visibility.Collapsed;
            return;
        }

        AuditPanel.Visibility = Visibility.Visible;
        AuditSummaryText.Text = BuildAuditSummary(_audit);

        if (_audit.HasErrors)
            AuditSummaryText.Foreground = System.Windows.Media.Brushes.Firebrick;
        else
            AuditSummaryText.Foreground = System.Windows.Media.Brushes.DarkOrange;
    }

    private static string BuildAuditSummary(BudgetAuditReport audit)
    {
        if (audit.HasErrors)
            return $"ATENÇÃO: {audit.ErrorCount} erro(s) e {audit.WarningCount} aviso(s) — corrija antes de exportar o PDF.";

        if (audit.HasWarnings)
            return $"Revisão recomendada: {audit.WarningCount} aviso(s) encontrado(s).";

        return $"Informações: {audit.InfoCount} observação(ões) para revisar.";
    }

    private void OpenAudit_Click(object sender, RoutedEventArgs e)
    {
        var window = new BudgetAuditWindow(_audit, _project.Metadata.GetWorkDisplayName(), allowContinue: false);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        ClientMetadataFields.SaveClient(_project.Metadata, ClientFields);
        _onProjectChanged();
        _audit = BudgetAuditService.Run(_project);

        if (_audit.HasErrors)
        {
            MessageBox.Show(
                "O orçamento contém erros que impedem a exportação. Abra a auditoria e corrija os itens em vermelho.",
                "Exportar PDF",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            UpdateAuditPanel();
            return;
        }

        if (_audit.HasWarnings)
        {
            var confirm = MessageBox.Show(
                "O projeto possui avisos na auditoria. Deseja exportar o PDF mesmo assim?",
                "Exportar PDF",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{_project.Metadata.GetWorkDisplayName()}-orcamento.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        var summary = BudgetService.Build(_project);
        byte[]? png = _captureViewportPng?.Invoke();
        BudgetPdfExporter.Export(summary, dialog.FileName, png);

        MessageBox.Show(
            "PDF exportado com sucesso.",
            "Orçamento",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class BudgetRowViewModel
    {
        public BudgetRowViewModel(BudgetLineItem item)
        {
            ModuleId = item.ModuleId!.Value;
            Description = item.Description;
            DimensionsText = item.DimensionsText;
            MaterialName = item.MaterialName;
            BasePrice = item.BasePrice;
            MaterialAddOn = item.MaterialAddOn;
        }

        public Guid ModuleId { get; }

        public string Description { get; }

        public string DimensionsText { get; }

        public string MaterialName { get; }

        public decimal BasePrice { get; set; }

        public decimal MaterialAddOn { get; }

        public decimal Total => BasePrice + MaterialAddOn;
    }
}
