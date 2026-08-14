using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracos3DStudio;

public sealed class MachineCutPlanDocument
{
    public int SchemaVersion { get; set; } = 1;

    public string Format { get; set; } = "tracos-cut-plan";

    public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;

    public required MachineCutPlanProjectInfo Project { get; init; }

    public required MachineCutPlanSettings Settings { get; init; }

    public required MachineCutPlanTotals Summary { get; init; }

    public required IReadOnlyList<MachineCutSheet> Sheets { get; init; }
}

public sealed class MachineCutPlanProjectInfo
{
    public required string Name { get; init; }

    public string? ClientName { get; init; }

    public string? ConstructionProfileId { get; init; }
}

public sealed class MachineCutPlanSettings
{
    public float SheetLengthMm { get; init; }

    public float SheetWidthMm { get; init; }

    public float CutKerfMm { get; init; }

    public float SheetMarginMm { get; init; }

    public float PanelThicknessMm { get; init; }

    public string Algorithm { get; init; } = "MaxRects";
}

public sealed class MachineCutPlanTotals
{
    public int TotalSheets { get; init; }

    public int TotalPlacedPieces { get; init; }

    public float OverallUtilizationPercent { get; init; }
}

public sealed class MachineCutSheet
{
    public int Index { get; init; }

    public required string MaterialName { get; init; }

    public float ThicknessMm { get; init; }

    public float LengthMm { get; init; }

    public float WidthMm { get; init; }

    public float UtilizationPercent { get; init; }

    public required IReadOnlyList<MachineCutPlacement> Pieces { get; init; }
}

public sealed class MachineCutPlacement
{
    public int InstanceId { get; init; }

    public Guid ModuleId { get; init; }

    public required string ModuleName { get; init; }

    public required string PieceName { get; init; }

    public float SheetXmm { get; init; }

    public float SheetYmm { get; init; }

    public float LengthMm { get; init; }

    public float WidthMm { get; init; }

    public bool Rotated { get; init; }

    public string? EdgeBand { get; init; }

    public IReadOnlyList<MachineCutHole> Holes { get; init; } = Array.Empty<MachineCutHole>();
}

public sealed class MachineCutHole
{
    public DrillHoleKind Kind { get; init; }

    public DrillHoleEdge Edge { get; init; }

    public float PosXmm { get; init; }

    public float PosYmm { get; init; }

    public float DiameterMm { get; init; }

    public float DepthMm { get; init; }
}

public static class MachineCutPlanExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static MachineCutPlanDocument Build(Project project)
    {
        var parts = PartsListService.Build(project);
        var holesByKey = BuildHoleLookup(parts);
        var plan = CutPlanService.Build(project);

        var sheets = plan.Sheets
            .Select(sheet => new MachineCutSheet
            {
                Index = sheet.Index,
                MaterialName = sheet.MaterialName,
                ThicknessMm = sheet.ThicknessMm,
                LengthMm = sheet.SheetLengthMm,
                WidthMm = sheet.SheetWidthMm,
                UtilizationPercent = sheet.UtilizationPercent,
                Pieces = sheet.Placements
                    .Select(placement => MapPlacement(placement, holesByKey))
                    .ToList()
            })
            .ToList();

        return new MachineCutPlanDocument
        {
            Project = new MachineCutPlanProjectInfo
            {
                Name = project.Metadata.Name,
                ClientName = project.Metadata.ClientName,
                ConstructionProfileId = project.Metadata.ConstructionProfileId
            },
            Settings = new MachineCutPlanSettings
            {
                SheetLengthMm = project.Metadata.SheetLengthMm,
                SheetWidthMm = project.Metadata.SheetWidthMm,
                CutKerfMm = project.Metadata.CutKerfMm,
                SheetMarginMm = project.Metadata.SheetMarginMm,
                PanelThicknessMm = project.Metadata.PanelThicknessMm
            },
            Summary = new MachineCutPlanTotals
            {
                TotalSheets = plan.TotalSheets,
                TotalPlacedPieces = sheets.Sum(s => s.Pieces.Count),
                OverallUtilizationPercent = plan.OverallUtilizationPercent
            },
            Sheets = sheets
        };
    }

    public static void ExportDrillCsv(Project project, string filePath)
    {
        var document = Build(project);
        CncDrillCsvExporter.Export(document, filePath);
    }

    public static int CountDrillRows(Project project) =>
        CncDrillCsvExporter.CountDrillRows(Build(project));

    public static void ExportCncJob(Project project, string filePath)
    {
        var document = Build(project);
        CncJobExporter.Export(document, filePath);
    }

    public static (int CutOps, int DrillOps) CountCncJobOperations(Project project) =>
        CncJobExporter.CountOperations(Build(project));

    public static void ExportJaraguaTap(Project project, string filePath, JaraguaMach4TapSettings? settings = null)
    {
        var document = Build(project);
        var job = CncJobExporter.Build(document);
        JaraguaMach4TapExporter.ExportToFile(job, filePath, settings);
    }

    public static void ExportCutPlanDxf(Project project, string baseFilePath)
    {
        var document = Build(project);
        DxfExporter.ExportCutPlanSheets(document, baseFilePath);
    }

    public static (int CutOps, int DrillOps, int Sheets) CountJaraguaTapOperations(Project project)
    {
        var document = Build(project);
        var job = CncJobExporter.Build(document);
        return (job.Summary.TotalCutOperations, job.Summary.TotalDrillOperations, job.Sheets.Count);
    }

    public static void ExportToFile(Project project, string filePath)
    {
        var document = Build(project);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static Dictionary<(Guid ModuleId, string PieceName), IReadOnlyList<DrillHole>> BuildHoleLookup(
        PartsListSummary parts)
    {
        var lookup = new Dictionary<(Guid, string), IReadOnlyList<DrillHole>>();

        foreach (var piece in parts.Items.Where(p => p.Holes.Count > 0))
            lookup[(piece.ModuleId, piece.Name)] = piece.Holes;

        return lookup;
    }

    private static MachineCutPlacement MapPlacement(
        PlacedCutPiece placement,
        Dictionary<(Guid ModuleId, string PieceName), IReadOnlyList<DrillHole>> holesByKey)
    {
        var piece = placement.Piece;
        holesByKey.TryGetValue((piece.ModuleId, piece.PieceName), out var holes);

        return new MachineCutPlacement
        {
            InstanceId = piece.InstanceId,
            ModuleId = piece.ModuleId,
            ModuleName = piece.ModuleName,
            PieceName = piece.PieceName,
            SheetXmm = placement.X,
            SheetYmm = placement.Y,
            LengthMm = placement.WidthMm,
            WidthMm = placement.HeightMm,
            Rotated = placement.Rotated,
            EdgeBand = piece.EdgeBand,
            Holes = holes?.Select(MapHole).ToList() ?? []
        };
    }

    private static MachineCutHole MapHole(DrillHole hole) =>
        new()
        {
            Kind = hole.Kind,
            Edge = hole.Edge,
            PosXmm = hole.PosXmm,
            PosYmm = hole.PosYmm,
            DiameterMm = hole.DiameterMm,
            DepthMm = hole.DepthMm
        };
}
