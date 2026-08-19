namespace Tracos3DStudio;

public static class ModuleDecompositionService
{
    private const float FrontGapMm = 4f;

    public static IReadOnlyList<PartPiece> Decompose(
        ModuleInstance module,
        ModuleDefinition definition,
        float panelThicknessMm,
        float backThicknessMm)
    {
        return Decompose(module, definition, panelThicknessMm, backThicknessMm, dimensionSettings: null);
    }

    public static IReadOnlyList<PartPiece> Decompose(
        ModuleInstance module,
        ModuleDefinition definition,
        float panelThicknessMm,
        float backThicknessMm,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        if (BalconyModuleBuilder.TryDecompose(module, definition, out var balconyPieces))
            return balconyPieces;

        if (DrawerModuleBuilder.TryDecompose(
                module, definition, panelThicknessMm, backThicknessMm,
                dimensionSettings, out var drawerPieces))
            return drawerPieces;

        if (dimensionSettings != null || definition.ModulationRules is { Pieces.Count: > 0 })
        {
            var effectiveRules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
            if (effectiveRules is { Pieces.Count: > 0 })
            {
                return ModulationDecompositionService.Decompose(
                    module, definition, effectiveRules, panelThicknessMm, backThicknessMm);
            }
        }

        var material = ResolveMaterialName(module);
        var pieces = new List<PartPiece>();

        if (definition.IsDecorativePanel)
        {
            pieces.Add(Make(
                module,
                ModuleInstanceNamingService.GetEffectiveDisplayName(module),
                material,
                "Painel",
                module.Width,
                module.Height,
                module.Depth));

            return pieces;
        }

        float w = module.Width;
        float h = module.Height;
        float d = module.Depth;
        float innerW = Math.Max(0, w - 2 * panelThicknessMm);
        float innerD = Math.Max(0, d - backThicknessMm);
        float innerH = Math.Max(0, h - 2 * panelThicknessMm);
        string modName = ModuleInstanceNamingService.GetEffectiveDisplayName(module);

        pieces.Add(Make(module, modName, material, "Lateral", d, h, panelThicknessMm, 2));

        if (!definition.IsWallMounted)
            pieces.Add(Make(module, modName, material, "Base inferior", innerW, innerD, panelThicknessMm));

        pieces.Add(Make(module, modName, material, "Tampo interno", innerW, innerD, panelThicknessMm));
        pieces.Add(Make(module, modName, material, "Fundo", innerW, innerH, backThicknessMm));

        if (definition.DoorCount > 0 && definition.DrawerCount == 0)
            pieces.Add(Make(module, modName, material, "Prateleira", innerW - 4f, innerD - 20f, panelThicknessMm));

        AddFronts(pieces, module, definition, modName, material, w, h, definition.FrontThickness);

        return pieces;
    }

    private static void AddFronts(
        List<PartPiece> pieces,
        ModuleInstance module,
        ModuleDefinition definition,
        string modName,
        string material,
        float width,
        float height,
        float frontThickness)
    {
        if (definition.DrawerCount > 0)
        {
            float drawerHeight = (height - FrontGapMm * (definition.DrawerCount + 1)) / definition.DrawerCount;

            for (int i = 0; i < definition.DrawerCount; i++)
                pieces.Add(Make(module, modName, material, $"Frente gaveta {i + 1}", width - 2 * FrontGapMm, drawerHeight, frontThickness));

            return;
        }

        int doorCount = Math.Max(1, definition.DoorCount);
        float doorWidth = (width - FrontGapMm * (doorCount + 1)) / doorCount;
        float doorHeight = height - 2 * FrontGapMm;

        for (int i = 0; i < doorCount; i++)
            pieces.Add(Make(module, modName, material, $"Frente porta {i + 1}", doorWidth, doorHeight, frontThickness));
    }

    private static PartPiece Make(
        ModuleInstance module,
        string modName,
        string material,
        string name,
        float length,
        float width,
        float thickness,
        int quantity = 1) =>
        new()
        {
            ModuleId = module.Id,
            ModuleName = modName,
            Name = name,
            LengthMm = Math.Max(0, length),
            WidthMm = Math.Max(0, width),
            ThicknessMm = thickness,
            Quantity = quantity,
            MaterialName = material
        };

    private static string ResolveMaterialName(ModuleInstance module)
    {
        if (MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null)
            return mat.DisplayName;

        return MaterialCatalog.GetDefault().DisplayName;
    }
}
