namespace Tracos3DStudio;

public sealed class PartPiece
{
    public Guid ModuleId { get; init; }

    public required string ModuleName { get; init; }

    public required string Name { get; init; }

    public float LengthMm { get; init; }

    public float WidthMm { get; init; }

    public float ThicknessMm { get; init; }

    public int Quantity { get; init; } = 1;

    public required string MaterialName { get; init; }

    /// <summary>Fita de borda da regra do template (V3.7d). Null = heurística legada.</summary>
    public ModulationEdgeBanding? EdgeBandingSpec { get; init; }

    /// <summary>Padrão de furação da regra do template (V3.7d). Null = Auto (legado).</summary>
    public ModulationDrillingPattern? DrillingPattern { get; init; }

    public IReadOnlyList<DrillHole> Holes { get; init; } = Array.Empty<DrillHole>();

    public string DimensionsText =>
        $"{LengthMm:0} × {WidthMm:0} × {ThicknessMm:0}";

    public string DrillingText =>
        Holes.Count == 0
            ? "-"
            : string.Join(", ", Holes.Select(h => h.Summary));

    public PartPiece WithHoles(IReadOnlyList<DrillHole> holes) =>
        new()
        {
            ModuleId = ModuleId,
            ModuleName = ModuleName,
            Name = Name,
            LengthMm = LengthMm,
            WidthMm = WidthMm,
            ThicknessMm = ThicknessMm,
            Quantity = Quantity,
            MaterialName = MaterialName,
            EdgeBandingSpec = EdgeBandingSpec,
            DrillingPattern = DrillingPattern,
            Holes = holes
        };
}

public sealed class PartsListSummary
{
    public required IReadOnlyList<PartPiece> Items { get; init; }

    public string ProjectName { get; init; } = "Projeto sem título";

    public float PanelThicknessMm { get; init; }

    public int TotalPieceCount => Items.Sum(i => i.Quantity);
}
