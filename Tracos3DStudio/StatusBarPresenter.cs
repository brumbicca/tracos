namespace Tracos3DStudio;

public sealed class StatusBarInput
{
    public string ProfileName { get; init; } = "Padrão (18 mm)";

    public string ProjectName { get; init; } = "Sem título";

    public bool IsProjectDirty { get; init; }

    public string BuildLabel { get; init; } = "desenvolvimento";

    public string ViewLabel { get; init; } = "Perspectiva";

    public int ModuleCount { get; init; }

    public int WallCount { get; init; }

    public bool RoomClosed { get; init; }

    public string ActiveMaterialName { get; init; } = string.Empty;

    public MaterialApplicationMode ApplicationMode { get; init; } = MaterialApplicationMode.Auto;

    public bool CollisionEnabled { get; init; }

    public int CollidingModuleCount { get; init; }

    public string? ContextOverride { get; init; }

    public string? HintOverride { get; init; }
}

public sealed class StatusBarPresentation
{
    public required string ProjectInfo { get; init; }

    public required string ViewContext { get; init; }

    public required string MaterialInfo { get; init; }

    public required string Hint { get; init; }

    public required string Status { get; init; }

    public required string FullText { get; init; }
}

public static class StatusBarPresenter
{
    public static StatusBarPresentation Build(StatusBarInput input)
    {
        string projectInfo =
            $"Perfil: {input.ProfileName}   ·   Unidade: mm   ·   Build: {input.BuildLabel}   ·   " +
            $"Projeto: {input.ProjectName}{(input.IsProjectDirty ? " *" : "")}";

        string viewContext = BuildViewContext(input);
        string materialInfo = BuildMaterialInfo(input);
        string hint = input.HintOverride ?? string.Empty;
        string status = BuildStatus(input);

        string fullText = JoinSegments(projectInfo, viewContext, materialInfo, hint, status);

        return new StatusBarPresentation
        {
            ProjectInfo = projectInfo,
            ViewContext = viewContext,
            MaterialInfo = materialInfo,
            Hint = hint,
            Status = status,
            FullText = fullText
        };
    }

    public static string FormatSelection(string kind, float primaryMm, string? detailName = null)
    {
        if (!string.IsNullOrWhiteSpace(detailName))
            return $"{kind}: {detailName} — {primaryMm:0} mm";

        return $"{kind}: {primaryMm:0} mm";
    }

    public static string FormatClosedRoom(int wallCount, int moduleCount)
    {
        string modules = moduleCount == 1 ? "1 módulo" : $"{moduleCount} módulos";
        return $"Ambiente: Fechado ({wallCount} paredes)   ·   {modules}";
    }

    public static string GetApplicationModeLabel(MaterialApplicationMode mode) =>
        mode switch
        {
            MaterialApplicationMode.Module => "Módulo",
            MaterialApplicationMode.WallFace => "Face da parede",
            MaterialApplicationMode.WallBand => "Faixa",
            MaterialApplicationMode.WallRegion => "Região",
            MaterialApplicationMode.Floor => "Piso",
            MaterialApplicationMode.FloorZone => "Região do piso",
            _ => "Automático"
        };

    private static string BuildViewContext(StatusBarInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.ContextOverride))
            return $"Vista: {input.ViewLabel}   ·   {input.ContextOverride}";

        if (input.RoomClosed)
            return $"Vista: {input.ViewLabel}   ·   {FormatClosedRoom(input.WallCount, input.ModuleCount)}";

        return $"Vista: {input.ViewLabel}   ·   Face: Nenhuma";
    }

    private static string BuildMaterialInfo(StatusBarInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ActiveMaterialName))
            return string.Empty;

        return
            $"Material: {input.ActiveMaterialName}   ·   Modo: {GetApplicationModeLabel(input.ApplicationMode)}";
    }

    private static string BuildStatus(StatusBarInput input)
    {
        if (input.CollisionEnabled && input.CollidingModuleCount > 0)
        {
            string countLabel = input.CollidingModuleCount == 1
                ? "1 módulo"
                : $"{input.CollidingModuleCount} módulos";

            return $"Status: Colisão ({countLabel})";
        }

        return "Status: Pronto";
    }

    private static string JoinSegments(params string[] segments)
    {
        var parts = segments.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return string.Join("   |   ", parts);
    }
}
