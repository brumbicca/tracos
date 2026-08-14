namespace Tracos3DStudio;

public static class ConstructionProfiles
{
    public const string Padrao = "padrao";
    public const string Reforcado = "reforcado";
    public const string Economico = "economico";

    public static IReadOnlyList<(string Id, string DisplayName)> All =>
    [
        (Padrao, "Padrão (18 mm)"),
        (Reforcado, "Reforçado (25 mm)"),
        (Economico, "Econômico (15 mm)")
    ];

    public static void Apply(Project project, string profileId)
    {
        project.Metadata.ConstructionProfileId = profileId;
        project.Metadata.PanelThicknessMm = profileId switch
        {
            Reforcado => 25f,
            Economico => 15f,
            _ => 18f
        };
    }

    public static string GetDisplayName(string profileId) =>
        All.FirstOrDefault(p => p.Id == profileId).DisplayName ?? "Padrão (18 mm)";
}
