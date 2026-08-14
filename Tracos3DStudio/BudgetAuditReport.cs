namespace Tracos3DStudio;

public enum BudgetAuditSeverity
{
    Error,
    Warning,
    Info
}

public sealed class BudgetAuditFinding
{
    public BudgetAuditSeverity Severity { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? RelatedItem { get; init; }
}

public sealed class BudgetAuditReport
{
    public required IReadOnlyList<BudgetAuditFinding> Findings { get; init; }

    public int ErrorCount => Findings.Count(f => f.Severity == BudgetAuditSeverity.Error);

    public int WarningCount => Findings.Count(f => f.Severity == BudgetAuditSeverity.Warning);

    public int InfoCount => Findings.Count(f => f.Severity == BudgetAuditSeverity.Info);

    public bool HasErrors => ErrorCount > 0;

    public bool HasWarnings => WarningCount > 0;

    public bool HasBlockingIssues => HasErrors;

    public bool IsClean => Findings.Count == 0;
}
