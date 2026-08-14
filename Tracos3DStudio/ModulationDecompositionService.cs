namespace Tracos3DStudio;

/// <summary>
/// Decomposição de peças guiada por <see cref="ModulationRules"/> (V3.7c).
/// </summary>
public static class ModulationDecompositionService
{
    public static IReadOnlyList<PartPiece> Decompose(
        ModuleInstance module,
        ModuleDefinition definition,
        ModulationRules rules,
        float panelThicknessMm,
        float backThicknessMm)
    {
        if (rules.Pieces.Count == 0)
            return Array.Empty<PartPiece>();

        var context = ModulationDimensionContext.FromModule(
            module,
            rules,
            panelThicknessMm,
            backThicknessMm);

        string modName = ModuleInstanceNamingService.GetEffectiveDisplayName(module);
        string material = ResolveMaterialName(module);
        var pieces = new List<PartPiece>(rules.Pieces.Count);

        foreach (var rule in rules.Pieces)
        {
            pieces.Add(new PartPiece
            {
                ModuleId = module.Id,
                ModuleName = modName,
                Name = rule.Name,
                LengthMm = ModulationDimensionResolver.Resolve(rule.Length, context),
                WidthMm = ModulationDimensionResolver.Resolve(rule.Width, context),
                ThicknessMm = ModulationDimensionResolver.Resolve(rule.Thickness, context),
                Quantity = Math.Max(1, rule.Quantity),
                MaterialName = material,
                EdgeBandingSpec = rule.EdgeBanding,
                DrillingPattern = rule.DrillingPattern
            });
        }

        return pieces;
    }

    private static string ResolveMaterialName(ModuleInstance module)
    {
        if (MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null)
            return mat.DisplayName;

        return MaterialCatalog.GetDefault().DisplayName;
    }
}
