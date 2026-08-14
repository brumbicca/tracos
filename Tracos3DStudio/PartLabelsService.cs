namespace Tracos3DStudio;

public static class PartLabelsService
{
    public static PartLabelsSummary Build(Project project)
    {
        var parts = PartsListService.Build(project);
        var labels = new List<PartLabel>();
        int total = parts.TotalPieceCount;
        int index = 0;

        foreach (var piece in parts.Items)
        {
            for (int copy = 1; copy <= piece.Quantity; copy++)
            {
                index++;
                labels.Add(new PartLabel
                {
                    Index = index,
                    Total = total,
                    ModuleId = piece.ModuleId,
                    ProjectName = parts.ProjectName,
                    ModuleName = piece.ModuleName,
                    PieceName = piece.Name,
                    DimensionsText = piece.DimensionsText,
                    MaterialName = piece.MaterialName,
                    ThicknessMm = piece.ThicknessMm,
                    ShortCode = BuildShortCode(piece, copy),
                    DrillingText = piece.Holes.Count > 0 ? piece.DrillingText : null
                });
            }
        }

        return new PartLabelsSummary
        {
            Labels = labels,
            ProjectName = parts.ProjectName
        };
    }

    private static string BuildShortCode(PartPiece piece, int copyIndex)
    {
        string moduleToken = Abbreviate(piece.ModuleName, 3);
        string pieceToken = Abbreviate(piece.Name, 3);
        string thickness = piece.ThicknessMm.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        return copyIndex > 1
            ? $"{moduleToken}-{pieceToken}-{thickness}-{copyIndex:00}"
            : $"{moduleToken}-{pieceToken}-{thickness}";
    }

    private static string Abbreviate(string value, int maxParts)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "PEC";

        var parts = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(maxParts)
            .Select(p => char.ToUpperInvariant(p[0]))
            .ToArray();

        return parts.Length > 0 ? new string(parts) : "PEC";
    }
}
