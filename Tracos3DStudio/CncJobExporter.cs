using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracos3DStudio;

public sealed class CncJobDocument
{
    public int SchemaVersion { get; set; } = 1;

    public string Format { get; set; } = "tracos-cnc-job";

    public string Units { get; set; } = "mm";

    public string CoordinateSystem { get; set; } = "sheet-origin-bottom-left";

    public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;

    public required MachineCutPlanProjectInfo Project { get; init; }

    public required MachineCutPlanSettings Settings { get; init; }

    public required CncJobTotals Summary { get; init; }

    public required IReadOnlyList<CncJobSheet> Sheets { get; init; }
}

public sealed class CncJobTotals
{
    public int TotalSheets { get; init; }

    public int TotalCutOperations { get; init; }

    public int TotalDrillOperations { get; init; }
}

public sealed class CncJobSheet
{
    public int Index { get; init; }

    public required string MaterialName { get; init; }

    public float ThicknessMm { get; init; }

    public float LengthMm { get; init; }

    public float WidthMm { get; init; }

    public required IReadOnlyList<CncJobOperation> Operations { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CncCutOperation), "cut")]
[JsonDerivedType(typeof(CncDrillOperation), "drill")]
public abstract class CncJobOperation;

public sealed class CncCutOperation : CncJobOperation
{
    public int InstanceId { get; init; }

    public required string ModuleName { get; init; }

    public required string PieceName { get; init; }

    public bool Rotated { get; init; }

    public string? EdgeBand { get; init; }

    /// <summary>Retângulo fechado (4 vértices) em coordenadas da chapa.</summary>
    public required IReadOnlyList<float[]> ContourMm { get; init; }
}

public sealed class CncDrillOperation : CncJobOperation
{
    public int InstanceId { get; init; }

    public required string ModuleName { get; init; }

    public required string PieceName { get; init; }

    public float SheetXmm { get; init; }

    public float SheetYmm { get; init; }

    public float LocalXmm { get; init; }

    public float LocalYmm { get; init; }

    public DrillHoleKind Kind { get; init; }

    public DrillHoleEdge Edge { get; init; }

    public float DiameterMm { get; init; }

    public float DepthMm { get; init; }
}

public static class CncJobExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static CncJobDocument Build(MachineCutPlanDocument source)
    {
        var sheets = source.Sheets
            .Select(MapSheet)
            .ToList();

        return new CncJobDocument
        {
            Project = source.Project,
            Settings = source.Settings,
            Summary = new CncJobTotals
            {
                TotalSheets = sheets.Count,
                TotalCutOperations = sheets.Sum(s => s.Operations.Count(o => o is CncCutOperation)),
                TotalDrillOperations = sheets.Sum(s => s.Operations.Count(o => o is CncDrillOperation))
            },
            Sheets = sheets
        };
    }

    public static void Export(MachineCutPlanDocument source, string filePath)
    {
        var document = Build(source);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static (int CutOps, int DrillOps) CountOperations(MachineCutPlanDocument source)
    {
        var document = Build(source);
        return (document.Summary.TotalCutOperations, document.Summary.TotalDrillOperations);
    }

    public static IReadOnlyList<float[]> BuildCutContour(MachineCutPlacement placement) =>
    [
        [placement.SheetXmm, placement.SheetYmm],
        [placement.SheetXmm + placement.LengthMm, placement.SheetYmm],
        [placement.SheetXmm + placement.LengthMm, placement.SheetYmm + placement.WidthMm],
        [placement.SheetXmm, placement.SheetYmm + placement.WidthMm]
    ];

    private static CncJobSheet MapSheet(MachineCutSheet sheet)
    {
        var operations = new List<CncJobOperation>();

        foreach (var piece in sheet.Pieces)
        {
            operations.Add(new CncCutOperation
            {
                InstanceId = piece.InstanceId,
                ModuleName = piece.ModuleName,
                PieceName = piece.PieceName,
                Rotated = piece.Rotated,
                EdgeBand = piece.EdgeBand,
                ContourMm = BuildCutContour(piece)
            });

            foreach (var hole in piece.Holes)
            {
                var (sheetX, sheetY) = CncDrillCoordinateService.ToSheetCoordinates(piece, hole);

                operations.Add(new CncDrillOperation
                {
                    InstanceId = piece.InstanceId,
                    ModuleName = piece.ModuleName,
                    PieceName = piece.PieceName,
                    SheetXmm = sheetX,
                    SheetYmm = sheetY,
                    LocalXmm = hole.PosXmm,
                    LocalYmm = hole.PosYmm,
                    Kind = hole.Kind,
                    Edge = hole.Edge,
                    DiameterMm = hole.DiameterMm,
                    DepthMm = hole.DepthMm
                });
            }
        }

        return new CncJobSheet
        {
            Index = sheet.Index,
            MaterialName = sheet.MaterialName,
            ThicknessMm = sheet.ThicknessMm,
            LengthMm = sheet.LengthMm,
            WidthMm = sheet.WidthMm,
            Operations = operations
        };
    }
}
