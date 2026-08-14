namespace Tracos3DStudio.Tests;

/// <summary>Referência do algoritmo greedy em fileiras (pré-MaxRects) para comparar aproveitamento.</summary>
internal static class GreedyCutPlanBenchmark
{
    public static int TotalPieceCount(Project project) =>
        PartsListService.Build(project).TotalPieceCount;

    public static CutPlanSummary Build(Project project)
    {
        var parts = PartsListService.Build(project);
        var instances = ExpandInstances(parts);
        var sheets = new List<CutSheet>();

        foreach (var group in instances.GroupBy(i => (i.MaterialName, i.ThicknessMm)))
        {
            sheets.AddRange(NestGroup(
                group.ToList(),
                group.Key.MaterialName,
                group.Key.ThicknessMm,
                project.Metadata.SheetLengthMm,
                project.Metadata.SheetWidthMm,
                project.Metadata.CutKerfMm,
                project.Metadata.SheetMarginMm));
        }

        for (int i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            sheets[i] = new CutSheet
            {
                Index = i + 1,
                MaterialName = sheet.MaterialName,
                ThicknessMm = sheet.ThicknessMm,
                SheetLengthMm = sheet.SheetLengthMm,
                SheetWidthMm = sheet.SheetWidthMm,
                Placements = sheet.Placements
            };
        }

        return new CutPlanSummary
        {
            Sheets = sheets,
            ProjectName = project.Metadata.Name,
            SheetLengthMm = project.Metadata.SheetLengthMm,
            SheetWidthMm = project.Metadata.SheetWidthMm
        };
    }

    private static List<CutPieceInstance> ExpandInstances(PartsListSummary parts)
    {
        var instances = new List<CutPieceInstance>();
        int id = 1;

        foreach (var piece in parts.Items)
        {
            for (int q = 0; q < piece.Quantity; q++)
            {
                instances.Add(new CutPieceInstance
                {
                    InstanceId = id++,
                    ModuleId = piece.ModuleId,
                    ModuleName = piece.ModuleName,
                    PieceName = piece.Name,
                    LengthMm = piece.LengthMm,
                    WidthMm = piece.WidthMm,
                    ThicknessMm = piece.ThicknessMm,
                    MaterialName = piece.MaterialName,
                    EdgeBand = EdgeBandService.ComputeEdgeBand(piece)
                });
            }
        }

        return instances;
    }

    private static List<CutSheet> NestGroup(
        List<CutPieceInstance> pieces,
        string materialName,
        float thicknessMm,
        float sheetLength,
        float sheetWidth,
        float kerf,
        float margin)
    {
        var sheets = new List<CutSheet>();
        var sorted = pieces.OrderByDescending(p => Math.Max(p.LengthMm, p.WidthMm)).ToList();

        var placements = new List<PlacedCutPiece>();
        float x = margin;
        float y = margin;
        float rowHeight = 0f;

        float usableLength = sheetLength - margin * 2;
        float usableWidth = sheetWidth - margin * 2;

        void FlushSheet()
        {
            if (placements.Count == 0)
                return;

            sheets.Add(new CutSheet
            {
                Index = sheets.Count + 1,
                MaterialName = materialName,
                ThicknessMm = thicknessMm,
                SheetLengthMm = sheetLength,
                SheetWidthMm = sheetWidth,
                Placements = placements.ToList()
            });

            placements.Clear();
            x = margin;
            y = margin;
            rowHeight = 0f;
        }

        foreach (var piece in sorted)
        {
            if (!TryPlace(piece, ref x, ref y, ref rowHeight, placements, usableLength, usableWidth, kerf, margin))
            {
                FlushSheet();

                if (!TryPlace(piece, ref x, ref y, ref rowHeight, placements, usableLength, usableWidth, kerf, margin))
                {
                    placements.Add(PlacePiece(piece, margin, margin, piece.LengthMm, piece.WidthMm, false));
                    x = margin + piece.LengthMm + kerf;
                    y = margin;
                    rowHeight = piece.WidthMm;
                }
            }
        }

        FlushSheet();
        return sheets;
    }

    private static bool TryPlace(
        CutPieceInstance piece,
        ref float x,
        ref float y,
        ref float rowHeight,
        List<PlacedCutPiece> placements,
        float usableLength,
        float usableWidth,
        float kerf,
        float margin)
    {
        if (TryPlaceInRow(piece, ref x, ref y, ref rowHeight, placements, usableLength, usableWidth, kerf, margin))
            return true;

        x = margin;
        y += rowHeight + kerf;
        rowHeight = 0f;

        if (y - margin + Math.Min(piece.LengthMm, piece.WidthMm) > usableWidth + 0.1f)
            return false;

        return TryPlaceInRow(piece, ref x, ref y, ref rowHeight, placements, usableLength, usableWidth, kerf, margin);
    }

    private static bool TryPlaceInRow(
        CutPieceInstance piece,
        ref float x,
        ref float y,
        ref float rowHeight,
        List<PlacedCutPiece> placements,
        float usableLength,
        float usableWidth,
        float kerf,
        float margin)
    {
        var options = new[]
        {
            (Width: piece.LengthMm, Height: piece.WidthMm, Rotated: false),
            (Width: piece.WidthMm, Height: piece.LengthMm, Rotated: true)
        }.OrderByDescending(o => o.Width * o.Height);

        foreach (var option in options)
        {
            float localX = x - margin;
            float localY = y - margin;

            if (localX + option.Width > usableLength + 0.1f)
                continue;

            if (localY + option.Height > usableWidth + 0.1f)
                continue;

            placements.Add(PlacePiece(piece, x, y, option.Width, option.Height, option.Rotated));
            x += option.Width + kerf;
            rowHeight = Math.Max(rowHeight, option.Height);
            return true;
        }

        return false;
    }

    private static PlacedCutPiece PlacePiece(
        CutPieceInstance piece,
        float x,
        float y,
        float width,
        float height,
        bool rotated) =>
        new()
        {
            Piece = piece,
            X = x,
            Y = y,
            WidthMm = width,
            HeightMm = height,
            Rotated = rotated
        };
}
