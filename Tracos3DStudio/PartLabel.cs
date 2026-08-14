namespace Tracos3DStudio;

public sealed class PartLabel
{
    public int Index { get; init; }

    public int Total { get; init; }

    public Guid ModuleId { get; init; }

    public required string ProjectName { get; init; }

    public required string ModuleName { get; init; }

    public required string PieceName { get; init; }

    public required string DimensionsText { get; init; }

    public required string MaterialName { get; init; }

    public float ThicknessMm { get; init; }

    public required string ShortCode { get; init; }

    public string? DrillingText { get; init; }
}

public sealed class PartLabelsSummary
{
    public required IReadOnlyList<PartLabel> Labels { get; init; }

    public string ProjectName { get; init; } = "Projeto sem título";

    public int TotalCount => Labels.Count;
}
