namespace Tracos3DStudio;

/// <summary>
/// Resolve slots de dimensão e aplica padrões do configurador (Promob — Configurador de Dimensões).
/// </summary>
public static class DimensionConfiguratorService
{
    private const float CozinhaSuperiorBaixoMaxHeightMm = 450f;
    private const float CozinhaSuperiorMedioMaxHeightMm = 850f;
    private const float DormitorioCriadoMaxHeightMm = 650f;

    public static DimensionConfiguratorSettings GetSettings(Project project)
    {
        DimensionConfiguratorSettings settings = project.Metadata.DimensionSettings?.Clone()
            ?? DimensionConfiguratorProfileStore.Load()?.Clone()
            ?? DimensionConfiguratorSettings.CreateDefault();

        InitializeNestedSettings(settings);
        return settings;
    }

    /// <summary>
    /// Copia o padrão salvo do Configurador de Dimensões para projetos novos sem configuração própria.
    /// </summary>
    public static void EnsureProjectSettings(Project project)
    {
        if (project.Metadata.DimensionSettings != null)
            return;

        var profile = DimensionConfiguratorProfileStore.Load();
        if (profile == null)
            return;

        project.Metadata.DimensionSettings = profile.Clone();
        InitializeNestedSettings(project.Metadata.DimensionSettings);
        SyncProjectChapasFromSettings(project, project.Metadata.DimensionSettings);
    }

    private static void InitializeNestedSettings(DimensionConfiguratorSettings settings)
    {
        ChapaConfiguratorService.EnsureChapasInitialized(settings);
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        EletrosConfiguratorService.EnsureInitialized(settings);
        FrentesPortasConfiguratorService.EnsureInitialized(settings);
        GavetasConfiguratorService.EnsureInitialized(settings);
        GavetasInternasConfiguratorService.EnsureInitialized(settings);
        CavaConfiguratorService.EnsureInitialized(settings);
    }

    public static void SaveSettings(Project project, DimensionConfiguratorSettings settings)
    {
        SaveSettings(project, settings, persistGlobalProfile: true);
    }

    /// <summary>
    /// Atualiza o padrão de engenharia do projeto (e opcionalmente o perfil global).
    /// Usar apenas para próximas inserções — não ao aplicar só em módulos existentes/selecionados.
    /// </summary>
    public static void SaveSettings(
        Project project,
        DimensionConfiguratorSettings settings,
        bool persistGlobalProfile)
    {
        ChapaConfiguratorService.SyncLegacyChapaFields(settings);
        BoxAssemblyConfiguratorService.SyncLegacyShelfFields(settings);
        FrentesPortasConfiguratorService.SyncToLegacy(settings);
        project.Metadata.DimensionSettings = settings.Clone();
        InitializeNestedSettings(project.Metadata.DimensionSettings);
        SyncProjectChapasFromSettings(project, settings);
        if (persistGlobalProfile)
            DimensionConfiguratorProfileStore.Save(settings);
    }

    /// <summary>
    /// Sincroniza espessuras globais do projeto com chapas de cozinha (padrão Promob — linha principal).
    /// </summary>
    public static void SyncProjectChapasFromSettings(Project project, DimensionConfiguratorSettings settings)
    {
        project.Metadata.PanelThicknessMm = settings.CozinhaPanelThicknessMm;
        project.Metadata.BackThicknessMm = settings.CozinhaBackThicknessMm;
    }

    public static ModuleDimensionSlot ResolveSlot(ModuleDefinition definition)
    {
        if (definition.IsDecorativePanel)
            return ModuleDimensionSlot.Painel;

        if (definition.Category == ModuleCategory.Dormitorio)
        {
            if (definition.IsWallMounted)
                return ModuleDimensionSlot.DormitorioSuperior;

            if (definition.DrawerCount > 0 && definition.DoorCount == 0)
            {
                return definition.DefaultHeight <= DormitorioCriadoMaxHeightMm
                    ? ModuleDimensionSlot.DormitorioCriado
                    : ModuleDimensionSlot.DormitorioBancada;
            }

            return ModuleDimensionSlot.DormitorioArmario;
        }

        if (definition.Category == ModuleCategory.Cozinha)
        {
            if (definition.IsWallMounted)
                return ResolveCozinhaSuperiorSlot(definition);

            if (definition.Id.Contains("ilha", StringComparison.OrdinalIgnoreCase))
                return ModuleDimensionSlot.CozinhaIlha;

            if (string.Equals(
                    definition.LibraryGroup,
                    ModuleLibraryHierarchy.GroupAltos,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    definition.LibraryGroup,
                    "Despenseiros",
                    StringComparison.OrdinalIgnoreCase))
                return ModuleDimensionSlot.CozinhaDespenseiro;

            return ModuleDimensionSlot.CozinhaInferior;
        }

        return ModuleDimensionSlot.CozinhaInferior;
    }

    internal static ModuleDimensionSlot ResolveCozinhaSuperiorSlot(ModuleDefinition definition)
    {
        if (definition.DefaultHeight <= CozinhaSuperiorBaixoMaxHeightMm)
            return ModuleDimensionSlot.CozinhaSuperiorBaixo;

        if (definition.DefaultHeight <= CozinhaSuperiorMedioMaxHeightMm)
            return ModuleDimensionSlot.CozinhaSuperiorMedio;

        return ModuleDimensionSlot.CozinhaSuperiorAlto;
    }

    public static (float Height, float Depth) GetSlotDefaults(
        DimensionConfiguratorSettings settings,
        ModuleDimensionSlot slot) =>
        slot switch
        {
            ModuleDimensionSlot.CozinhaInferior =>
                (settings.CozinhaInferiorHeightMm, settings.CozinhaInferiorDepthMm),
            ModuleDimensionSlot.CozinhaSuperiorBaixo =>
                (settings.CozinhaSuperiorBaixoHeightMm, settings.CozinhaSuperiorDepthMm),
            ModuleDimensionSlot.CozinhaSuperiorMedio =>
                (settings.CozinhaSuperiorHeightMm, settings.CozinhaSuperiorDepthMm),
            ModuleDimensionSlot.CozinhaSuperiorAlto =>
                (settings.CozinhaSuperiorAltoHeightMm, settings.CozinhaSuperiorDepthMm),
            ModuleDimensionSlot.CozinhaDespenseiro =>
                (settings.CozinhaDespenseiroHeightMm, settings.CozinhaDespenseiroDepthMm),
            ModuleDimensionSlot.CozinhaIlha =>
                (settings.CozinhaInferiorHeightMm, settings.CozinhaIlhaDepthMm),
            ModuleDimensionSlot.DormitorioArmario =>
                (settings.DormitorioArmarioHeightMm, settings.DormitorioArmarioDepthMm),
            ModuleDimensionSlot.DormitorioBancada =>
                (settings.DormitorioBancadaHeightMm, settings.DormitorioBancadaDepthMm),
            ModuleDimensionSlot.DormitorioCriado =>
                (settings.DormitorioCriadoHeightMm, settings.DormitorioCriadoDepthMm),
            ModuleDimensionSlot.DormitorioSuperior =>
                (settings.DormitorioSuperiorHeightMm, settings.DormitorioSuperiorDepthMm),
            ModuleDimensionSlot.Painel =>
                (settings.PainelHeightMm, settings.PainelThicknessMm),
            _ => (850f, 550f)
        };

    public static (float Width, float Height, float Depth) ResolveInsertionDimensions(
        Project project,
        ModuleDefinition definition) =>
        ResolveInsertionDimensions(definition, GetSettings(project));

    public static (float Width, float Height, float Depth) ResolveInsertionDimensions(
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        settings ??= DimensionConfiguratorSettings.CreateDefault();
        var slot = ResolveSlot(definition);
        var (height, depth) = GetSlotDefaults(settings, slot);

        // O canto oblíquo nasce quadrado, como o Canto L de referência.
        // A profundidade padrão da cozinha continua definindo o comprimento
        // das duas laterais; a engenharia do módulo pode alterar livremente
        // a largura e a profundidade externas depois da inserção.
        if (definition.ShapeKind == ModuleShapeKind.Oblique)
            depth = definition.DefaultDepth;

        float width = slot == ModuleDimensionSlot.Painel
            ? settings.PainelWidthMm
            : definition.DefaultWidth;

        width = ModuleDimensionClamp.ClampForFreeEdit(width, settings.MaxWidthMm);
        height = ModuleDimensionClamp.ClampForFreeEdit(height, settings.MaxHeightMm);
        depth = ModuleDimensionClamp.ClampForFreeEdit(depth, settings.MaxDepthMm);

        return (width, height, depth);
    }

    /// <summary>
    /// Regras efetivas com chapas, folgas e recuos do configurador (V3.7f Fase 2).
    /// </summary>
    public static ModulationRules? CreateEffectiveRules(
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        if (definition.IsDecorativePanel)
            return null;

        settings ??= DimensionConfiguratorSettings.CreateDefault();
        ChapaConfiguratorService.EnsureChapasInitialized(settings);
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);

        var baseRules = definition.ModulationRules is { Pieces.Count: > 0 } existing
            ? CloneRules(existing)
            : ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount);

        if (settings != null)
        {
            ApplyStructureOverlay(baseRules.Structure, definition, settings);
            ChapaConfiguratorService.ApplyToStructure(baseRules.Structure, definition, settings);
            ChapaConfiguratorService.ApplyToPieces(baseRules, definition, settings);
            BoxAssemblyConfiguratorService.ApplyToStructure(baseRules.Structure, definition, settings);
            BalconyModuleBuilder.ApplyStructureRules(definition, baseRules.Structure);
            BoxAssemblyConfiguratorService.ApplyToPieces(baseRules, definition, settings);
        }

        return baseRules;
    }

    public static void ApplyToModules(
        Project project,
        DimensionConfiguratorSettings settings,
        DimensionConfiguratorApplyScope scope,
        Guid? selectedModuleId,
        IReadOnlyCollection<Guid>? selectedModuleIds = null)
    {
        if (scope == DimensionConfiguratorApplyScope.NextInsertionsOnly)
            return;

        bool all = scope == DimensionConfiguratorApplyScope.AllExistingAndNext;
        HashSet<Guid>? selectedSet = null;

        if (!all)
        {
            if (selectedModuleIds is { Count: > 0 })
                selectedSet = selectedModuleIds.ToHashSet();
            else if (selectedModuleId.HasValue)
                selectedSet = [selectedModuleId.Value];
            else
                return;
        }

        foreach (var module in project.Modules)
        {
            if (!all && (selectedSet == null || !selectedSet.Contains(module.Id)))
                continue;

            if (!SceneModuleVisibilityService.IsEditable(module))
                continue;

            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            var (width, height, depth) = ResolveInsertionDimensions(definition, settings);

            if (ResolveSlot(definition) != ModuleDimensionSlot.Painel)
                width = module.Width;

            // Mesmo critério da inserção (ModuleInsertDropService): valores livres do configurador.
            module.SetDimensions(width, height, depth, definition, settings, respectCatalogLimits: false);
        }
    }

    internal static void ApplyStructureOverlay(
        ModulationStructure structure,
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings)
    {
        var slot = ResolveSlot(definition);

        switch (slot)
        {
            case ModuleDimensionSlot.CozinhaInferior:
                structure.PanelThicknessMm = settings.CozinhaPanelThicknessMm;
                structure.BackThicknessMm = settings.CozinhaBackThicknessMm;
                structure.FrontThicknessMm = settings.CozinhaFrontThicknessMm;
                structure.FrontGapMm = definition.DrawerCount > 0
                    ? settings.CozinhaDrawerFrontGapMm
                    : settings.CozinhaDoorFrontGapMm;
                break;

            case ModuleDimensionSlot.CozinhaSuperiorBaixo:
            case ModuleDimensionSlot.CozinhaSuperiorMedio:
            case ModuleDimensionSlot.CozinhaSuperiorAlto:
                structure.PanelThicknessMm = settings.CozinhaPanelThicknessMm;
                structure.BackThicknessMm = settings.CozinhaBackThicknessMm;
                structure.FrontThicknessMm = settings.CozinhaFrontThicknessMm;
                structure.FrontGapMm = settings.CozinhaSuperiorDoorFrontGapMm;
                break;

            case ModuleDimensionSlot.CozinhaDespenseiro:
            case ModuleDimensionSlot.CozinhaIlha:
                structure.PanelThicknessMm = settings.CozinhaPanelThicknessMm;
                structure.BackThicknessMm = settings.CozinhaBackThicknessMm;
                structure.FrontThicknessMm = settings.CozinhaFrontThicknessMm;
                structure.FrontGapMm = settings.CozinhaDespenseiroDoorFrontGapMm;
                break;

            case ModuleDimensionSlot.DormitorioArmario:
            case ModuleDimensionSlot.DormitorioBancada:
            case ModuleDimensionSlot.DormitorioCriado:
            case ModuleDimensionSlot.DormitorioSuperior:
                structure.PanelThicknessMm = settings.DormitorioPanelThicknessMm;
                structure.BackThicknessMm = settings.DormitorioBackThicknessMm;
                structure.FrontThicknessMm = settings.DormitorioFrontThicknessMm;
                structure.FrontGapMm = definition.DrawerCount > 0
                    ? settings.DormitorioDrawerFrontGapMm
                    : settings.DormitorioArmarioDoorFrontGapMm;
                break;
        }
    }

    private static ModulationRules CloneRules(ModulationRules source)
    {
        var clone = new ModulationRules
        {
            RulesVersion = source.RulesVersion,
            TemplateKind = source.TemplateKind,
            Structure = new ModulationStructure
            {
                PanelThicknessMm = source.Structure.PanelThicknessMm,
                BackThicknessMm = source.Structure.BackThicknessMm,
                FrontThicknessMm = source.Structure.FrontThicknessMm,
                FrontGapMm = source.Structure.FrontGapMm,
                BackPanelType = source.Structure.BackPanelType,
                BackPanelLayout = source.Structure.BackPanelLayout,
                BackRecessMm = source.Structure.BackRecessMm,
                BackHeightRecessMm = source.Structure.BackHeightRecessMm,
                BackUpperRailOffsetMm = source.Structure.BackUpperRailOffsetMm,
                BackLowerRailOffsetMm = source.Structure.BackLowerRailOffsetMm,
                BackSupportRailCount = source.Structure.BackSupportRailCount,
                BackSupportRailWidthMm = source.Structure.BackSupportRailWidthMm,
                BackAdvanceOverBaseMm = source.Structure.BackAdvanceOverBaseMm,
                BaseAdvanceOverBackMm = source.Structure.BaseAdvanceOverBackMm,
                BaseRecessMm = source.Structure.BaseRecessMm,
                BackAdvanceOverLateralMm = source.Structure.BackAdvanceOverLateralMm,
                LateralAdvanceOverBackMm = source.Structure.LateralAdvanceOverBackMm,
                BackAdvanceOverDivisionMm = source.Structure.BackAdvanceOverDivisionMm,
                SarrafoHeightMm = source.Structure.SarrafoHeightMm,
                SarrafoThicknessMm = source.Structure.SarrafoThicknessMm,
                LateralBaseOverlapMm = source.Structure.LateralBaseOverlapMm,
                BaseAdvanceOverLateralMm = source.Structure.BaseAdvanceOverLateralMm,
                LateralBottomRecessMm = source.Structure.LateralBottomRecessMm,
                LateralDepthGapMm = source.Structure.LateralDepthGapMm,
                LateralDepthAlignment = source.Structure.LateralDepthAlignment,
                CrossRailWidthMm = source.Structure.CrossRailWidthMm,
                SarrafoVisible = source.Structure.SarrafoVisible,
                FrontSarrafoIsVertical = source.Structure.FrontSarrafoIsVertical,
                BackSarrafoIsVertical = source.Structure.BackSarrafoIsVertical,
                FrontSarrafoVisible = source.Structure.FrontSarrafoVisible,
                BackSarrafoVisible = source.Structure.BackSarrafoVisible,
                SarrafoWhole = source.Structure.SarrafoWhole,
                FrontSarrafoSegmented = source.Structure.FrontSarrafoSegmented,
                BackSarrafoSegmented = source.Structure.BackSarrafoSegmented,
                SarrafoChamfered = source.Structure.SarrafoChamfered,
                SarrafoAdvanceOverLateralMm = source.Structure.SarrafoAdvanceOverLateralMm,
                SarrafoAdvanceOverBackMm = source.Structure.SarrafoAdvanceOverBackMm,
                BackAdvanceOverSarrafoMm = source.Structure.BackAdvanceOverSarrafoMm,
                BackSarrafoRecessMm = source.Structure.BackSarrafoRecessMm,
                BackSarrafoLowerRecessMm = source.Structure.BackSarrafoLowerRecessMm,
                LateralAdvanceOverFrontPanelMm = source.Structure.LateralAdvanceOverFrontPanelMm,
                FrontPanelAdvanceOverLateralMm = source.Structure.FrontPanelAdvanceOverLateralMm,
                DivisionFrontInsetMm = source.Structure.DivisionFrontInsetMm,
                DivisionMovableBackInsetMm = source.Structure.DivisionMovableBackInsetMm,
                DivisionFixedBackInsetMm = source.Structure.DivisionFixedBackInsetMm,
                DivisionBottomRecessMm = source.Structure.DivisionBottomRecessMm,
                DivisionSpacerWidthMm = source.Structure.DivisionSpacerWidthMm,
                SarrafoTraseiroHeightMm = source.Structure.SarrafoTraseiroHeightMm,
                SarrafoDianteiroRecessMm = source.Structure.SarrafoDianteiroRecessMm,
                FrontSideGapMm = source.Structure.FrontSideGapMm,
                FrontTopGapMm = source.Structure.FrontTopGapMm,
                FrontBottomGapMm = source.Structure.FrontBottomGapMm,
                FrontBays = source.Structure.FrontBays
                    .Select(b => new ModulationFrontBay
                    {
                        Id = b.Id,
                        Type = b.Type,
                        WidthFraction = b.WidthFraction,
                        HeightFraction = b.HeightFraction,
                        StackCount = b.StackCount
                    })
                    .ToList(),
                Shelves = source.Structure.Shelves
                    .Select(s => new ModulationShelfRule
                    {
                        Id = s.Id,
                        HeightFraction = s.HeightFraction,
                        DepthInsetMm = s.DepthInsetMm,
                        WidthInsetMm = s.WidthInsetMm,
                        BackInsetMm = s.BackInsetMm,
                        IsFixed = s.IsFixed
                    })
                    .ToList(),
                Divisions = source.Structure.Divisions
                    .Select(d => new ModulationDivisionRule
                    {
                        Id = d.Id,
                        WidthFraction = d.WidthFraction,
                        IsFixed = d.IsFixed
                    })
                    .ToList()
            },
            Pieces = source.Pieces
                .Select(p => new ModulationPieceRule
                {
                    Id = p.Id,
                    Role = p.Role,
                    Name = p.Name,
                    Length = CloneBinding(p.Length),
                    Width = CloneBinding(p.Width),
                    Thickness = CloneBinding(p.Thickness),
                    Quantity = p.Quantity,
                    EdgeBanding = p.EdgeBanding == null
                        ? null
                        : new ModulationEdgeBanding
                        {
                            Front = p.EdgeBanding.Front,
                            Back = p.EdgeBanding.Back,
                            Top = p.EdgeBanding.Top,
                            Bottom = p.EdgeBanding.Bottom
                        },
                    DrillingPattern = p.DrillingPattern
                })
                .ToList()
        };

        return clone;
    }

    private static ModulationDimensionBinding CloneBinding(ModulationDimensionBinding binding) =>
        new()
        {
            Source = binding.Source,
            ConstantMm = binding.ConstantMm,
            OffsetMm = binding.OffsetMm,
            Scale = binding.Scale
        };
}
