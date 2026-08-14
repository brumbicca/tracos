namespace Tracos3DStudio;

public sealed class CutPieceInstance
{
    public int InstanceId { get; init; }

    public Guid ModuleId { get; init; }

    public required string ModuleName { get; init; }

    public required string PieceName { get; init; }

    public float LengthMm { get; init; }

    public float WidthMm { get; init; }

    public float ThicknessMm { get; init; }

    public required string MaterialName { get; init; }

    public string? EdgeBand { get; init; }

    public string Label => $"{ModuleName} — {PieceName}";
}

public sealed class PlacedCutPiece
{
    public required CutPieceInstance Piece { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public float WidthMm { get; init; }

    public float HeightMm { get; init; }

    public bool Rotated { get; init; }
}

public sealed class CutSheet
{
    public int Index { get; init; }

    public required string MaterialName { get; init; }

    public float ThicknessMm { get; init; }

    public float SheetLengthMm { get; init; }

    public float SheetWidthMm { get; init; }

    public required IReadOnlyList<PlacedCutPiece> Placements { get; init; }

    public float UsedAreaMm2 =>
        Placements.Sum(p => p.WidthMm * p.HeightMm);

    public float UtilizationPercent =>
        SheetLengthMm <= 0 || SheetWidthMm <= 0
            ? 0f
            : UsedAreaMm2 / (SheetLengthMm * SheetWidthMm) * 100f;

    public string Title =>
        $"Chapa {Index} — {MaterialName} {ThicknessMm:0} mm ({UtilizationPercent:0.0}% aproveit.)";
}

public sealed class CutPlanSummary
{
    public required IReadOnlyList<CutSheet> Sheets { get; init; }

    public string ProjectName { get; init; } = "Projeto sem título";

    public float SheetLengthMm { get; init; }

    public float SheetWidthMm { get; init; }

    public int TotalSheets => Sheets.Count;

    public float OverallUtilizationPercent
    {
        get
        {
            if (Sheets.Count == 0)
                return 0f;

            float totalSheetArea = Sheets.Sum(s => s.SheetLengthMm * s.SheetWidthMm);
            float usedArea = Sheets.Sum(s => s.UsedAreaMm2);
            return totalSheetArea <= 0 ? 0f : usedArea / totalSheetArea * 100f;
        }
    }
}
