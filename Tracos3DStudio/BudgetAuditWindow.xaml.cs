using System.Windows;
using System.Windows.Media;

namespace Tracos3DStudio;

public partial class BudgetAuditWindow : Window
{
    private readonly BudgetAuditReport _report;

    public BudgetAuditWindow(BudgetAuditReport report, string projectName, bool allowContinue = true)
    {
        InitializeComponent();
        _report = report;

        ProjectTitleText.Text = projectName;
        SummaryText.Text = BuildSummaryText(report);
        SummaryText.Foreground = report.HasErrors
            ? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28))
            : report.HasWarnings
                ? new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00))
                : new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));

        FindingsGrid.ItemsSource = report.Findings
            .Select(f => new FindingRowViewModel(f))
            .ToList();

        if (!allowContinue)
            ContinueButton.Visibility = Visibility.Collapsed;
        else if (report.HasErrors)
            ContinueButton.Content = "Continuar mesmo assim...";
    }

    private static string BuildSummaryText(BudgetAuditReport report)
    {
        if (report.IsClean)
            return "Nenhum problema encontrado — o projeto está pronto para orçamento.";

        var parts = new List<string>();

        if (report.ErrorCount > 0)
            parts.Add($"{report.ErrorCount} erro(s)");

        if (report.WarningCount > 0)
            parts.Add($"{report.WarningCount} aviso(s)");

        if (report.InfoCount > 0)
            parts.Add($"{report.InfoCount} informação(ões)");

        string summary = string.Join(", ", parts);

        if (report.HasErrors)
            return $"ATENÇÃO: {summary}. Corrija os erros antes de exportar o PDF.";

        return $"Revisão recomendada: {summary}.";
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private sealed class FindingRowViewModel
    {
        public FindingRowViewModel(BudgetAuditFinding finding)
        {
            SeverityText = finding.Severity switch
            {
                BudgetAuditSeverity.Error => "Erro",
                BudgetAuditSeverity.Warning => "Aviso",
                _ => "Info"
            };
            Message = finding.Message;
        }

        public string SeverityText { get; }

        public string Message { get; }
    }
}
