using RectangleBinPacking;

namespace Tracos3DStudio;

public static class CutPlanService
{
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
        var remaining = pieces
            .OrderByDescending(p => p.LengthMm * p.WidthMm)
            .ToList();

        int binWidth = Math.Max(1, (int)Math.Floor(sheetLength - margin * 2));
        int binHeight = Math.Max(1, (int)Math.Floor(sheetWidth - margin * 2));

        while (remaining.Count > 0)
        {
            var packer = new MaxRectsBinPack(binWidth, binHeight, allowRotations: true);
            var placements = new List<PlacedCutPiece>();
            var notPlaced = new List<CutPieceInstance>();

            foreach (var piece in remaining)
            {
                if (TryPlacePiece(packer, piece, kerf, margin, placements))
                    continue;

                notPlaced.Add(piece);
            }

            if (placements.Count == 0)
            {
                var piece = notPlaced[0];
                notPlaced.RemoveAt(0);
                placements.Add(PlacePiece(piece, margin, margin, piece.LengthMm, piece.WidthMm, false));
            }

            sheets.Add(new CutSheet
            {
                Index = sheets.Count + 1,
                MaterialName = materialName,
                ThicknessMm = thicknessMm,
                SheetLengthMm = sheetLength,
                SheetWidthMm = sheetWidth,
                Placements = placements
            });

            remaining = notPlaced;
        }

        return sheets;
    }

    private static bool TryPlacePiece(
        MaxRectsBinPack packer,
        CutPieceInstance piece,
        float kerf,
        float margin,
        List<PlacedCutPiece> placements)
    {
        int length = Math.Max(1, (int)Math.Ceiling(piece.LengthMm));
        int width = Math.Max(1, (int)Math.Ceiling(piece.WidthMm));
        int kerfInt = Math.Max(0, (int)Math.Ceiling(kerf));

        var rect = packer.Insert(
            length + kerfInt,
            width + kerfInt,
            FreeRectChoiceHeuristic.RectBestShortSideFit);

        if (rect.Height <= 0)
            return false;

        bool rotated = rect.Width == width + kerfInt && rect.Height == length + kerfInt;
        float placedWidth = rotated ? piece.WidthMm : piece.LengthMm;
        float placedHeight = rotated ? piece.LengthMm : piece.WidthMm;

        placements.Add(PlacePiece(
            piece,
            margin + rect.X,
            margin + rect.Y,
            placedWidth,
            placedHeight,
            rotated));

        return true;
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
