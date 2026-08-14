namespace Tracos3DStudio;

/// <summary>
/// Resolve espessuras de chapa por tipo de peça (V3.7f Fase 3b).
/// </summary>
public static class ChapaConfiguratorService
{
    public static void EnsureChapasInitialized(DimensionConfiguratorSettings settings)
    {
        if (settings.CozinhaChapas.Pieces.Count == 0)
        {
            settings.CozinhaChapas = CategoryChapaSettings.CreateCozinhaDefaults(
                settings.CozinhaPanelThicknessMm,
                settings.CozinhaBackThicknessMm,
                settings.CozinhaFrontThicknessMm);
        }

        if (settings.DormitorioChapas.Pieces.Count == 0)
        {
            settings.DormitorioChapas = CategoryChapaSettings.CreateDormitorioDefaults(
                settings.DormitorioPanelThicknessMm,
                settings.DormitorioBackThicknessMm,
                settings.DormitorioFrontThicknessMm);
        }
        else
        {
            SeedMissingDormitorioChapaPieces(settings);
        }
    }

    private static void SeedMissingDormitorioChapaPieces(DimensionConfiguratorSettings settings)
    {
        var defaults = CategoryChapaSettings.CreateDormitorioDefaults(
            settings.DormitorioPanelThicknessMm,
            settings.DormitorioBackThicknessMm,
            settings.DormitorioFrontThicknessMm);

        foreach (var (key, piece) in defaults.Pieces)
        {
            if (!settings.DormitorioChapas.Pieces.ContainsKey(key))
                settings.DormitorioChapas.Pieces[key] = piece.Clone();
        }
    }

    public static void SyncLegacyChapaFields(DimensionConfiguratorSettings settings)
    {
        EnsureChapasInitialized(settings);

        settings.CozinhaPanelThicknessMm =
            settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm;
        settings.CozinhaBackThicknessMm =
            settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm;
        settings.CozinhaFrontThicknessMm =
            settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.PortasFrentes).ThicknessMm;

        settings.DormitorioPanelThicknessMm =
            settings.DormitorioChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm;
        settings.DormitorioBackThicknessMm =
            settings.DormitorioChapas.GetOrCreate(ChapaPieceKinds.Fundo).ThicknessMm;
        settings.DormitorioFrontThicknessMm =
            settings.DormitorioChapas.GetOrCreate(ChapaPieceKinds.PortasFrentes).ThicknessMm;
    }

    public static CategoryChapaSettings GetChapasForDefinition(
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings)
    {
        EnsureChapasInitialized(settings);
        return definition.Category == ModuleCategory.Dormitorio
            ? settings.DormitorioChapas
            : settings.CozinhaChapas;
    }

    public static float GetThickness(
        string chapaKind,
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings)
    {
        var chapas = GetChapasForDefinition(definition, settings);
        return chapas.GetOrCreate(chapaKind).ThicknessMm;
    }

    public static void ApplyToStructure(
        ModulationStructure structure,
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings)
    {
        var slot = DimensionConfiguratorService.ResolveSlot(definition);
        bool isSuperior = slot is ModuleDimensionSlot.CozinhaSuperiorBaixo
            or ModuleDimensionSlot.CozinhaSuperiorMedio
            or ModuleDimensionSlot.CozinhaSuperiorAlto
            or ModuleDimensionSlot.DormitorioSuperior;

        structure.PanelThicknessMm = GetThickness(ChapaPieceKinds.Lateral, definition, settings);
        structure.BackThicknessMm = definition.Category == ModuleCategory.Cozinha
            ? GetThickness(
                isSuperior ? ChapaPieceKinds.FundoSuperior : ChapaPieceKinds.FundoInferior,
                definition,
                settings)
            : GetThickness(ChapaPieceKinds.Fundo, definition, settings);
        structure.FrontThicknessMm = GetThickness(ChapaPieceKinds.PortasFrentes, definition, settings);
    }

    public static void ApplyToPieces(
        ModulationRules rules,
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings)
    {
        bool isCozinha = definition.Category == ModuleCategory.Cozinha;
        var slot = DimensionConfiguratorService.ResolveSlot(definition);
        bool isSuperior = slot is ModuleDimensionSlot.CozinhaSuperiorBaixo
            or ModuleDimensionSlot.CozinhaSuperiorMedio
            or ModuleDimensionSlot.CozinhaSuperiorAlto;

        foreach (var piece in rules.Pieces)
        {
            var kind = MapModulationRoleToChapaKind(piece.Role, isCozinha, isSuperior);
            if (kind == null)
                continue;

            if (piece.Thickness.Source is not (
                ModulationDimensionSource.PanelThickness
                or ModulationDimensionSource.BackThickness
                or ModulationDimensionSource.FrontThickness))
                continue;

            float thickness = GetThickness(kind, definition, settings);
            piece.Thickness = new ModulationDimensionBinding
            {
                Source = ModulationDimensionSource.Constant,
                ConstantMm = thickness
            };
        }
    }

    internal static string? MapModulationRoleToChapaKind(string role, bool isCozinha, bool isSuperior) =>
        role switch
        {
            "lateral" => ChapaPieceKinds.Lateral,
            "base-inferior" => ChapaPieceKinds.Base,
            "tampo-interno" => ChapaPieceKinds.Tampo,
            "fundo" => isCozinha
                ? isSuperior ? ChapaPieceKinds.FundoSuperior : ChapaPieceKinds.FundoInferior
                : ChapaPieceKinds.Fundo,
            "prateleira" => ChapaPieceKinds.Prateleira,
            "frente-porta" => ChapaPieceKinds.PortasFrentes,
            "frente-gaveta" => ChapaPieceKinds.FrenteGavInterna,
            _ => null
        };
}
