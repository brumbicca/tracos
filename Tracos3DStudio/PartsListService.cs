namespace Tracos3DStudio;

public static class PartsListService
{
    public static PartsListSummary Build(Project project)
    {
        var items = new List<PartPiece>();
        float panelThickness = project.Metadata.PanelThicknessMm;
        float backThickness = project.Metadata.BackThicknessMm;
        var dimensionSettings = DimensionConfiguratorService.GetSettings(project);

        foreach (var module in project.Modules)
        {
            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            foreach (var piece in ModuleDecompositionService.Decompose(
                         module,
                         definition,
                         panelThickness,
                         backThickness,
                         dimensionSettings))
            {
                var holes = CabinetDrillingService.Calculate(piece);
                items.Add(holes.Count > 0 ? piece.WithHoles(holes) : piece);
            }
        }

        return new PartsListSummary
        {
            Items = items,
            ProjectName = project.Metadata.Name,
            PanelThicknessMm = panelThickness
        };
    }
}
