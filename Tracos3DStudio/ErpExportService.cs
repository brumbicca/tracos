using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracos3DStudio;

public sealed class ErpExportDocument
{
    public int SchemaVersion { get; set; } = 1;

    public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;

    public required ProjectMetadata Project { get; init; }

    public int ModuleCount { get; init; }

    public int WallCount { get; init; }

    public BudgetSummary Budget { get; init; } = null!;

    public PartsListSummary Parts { get; init; } = null!;

    public CutPlanSummary CutPlan { get; init; } = null!;

    public string? LibraryName { get; init; }
}

public static class ErpExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static ErpExportDocument Build(Project project, string? libraryName = null) =>
        new()
        {
            Project = project.Metadata,
            ModuleCount = project.Modules.Count,
            WallCount = project.Room.Walls.Count,
            Budget = BudgetService.Build(project),
            Parts = PartsListService.Build(project),
            CutPlan = CutPlanService.Build(project),
            LibraryName = libraryName
        };

    public static void ExportToFile(Project project, string filePath, string? libraryName = null)
    {
        var document = Build(project, libraryName);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }
}
