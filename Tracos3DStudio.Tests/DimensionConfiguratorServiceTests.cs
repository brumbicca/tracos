using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class DimensionConfiguratorServiceTests
{
    [Theory]
    [InlineData("balcao-2-portas", ModuleDimensionSlot.CozinhaInferior)]
    [InlineData("aereo", ModuleDimensionSlot.CozinhaSuperiorMedio)]
    [InlineData("guarda-roupa-2p", ModuleDimensionSlot.DormitorioArmario)]
    [InlineData("criado-mudo", ModuleDimensionSlot.DormitorioCriado)]
    [InlineData("comoda-4g", ModuleDimensionSlot.DormitorioBancada)]
    [InlineData("painel-liso", ModuleDimensionSlot.Painel)]
    public void ResolveSlot_MapeiaModulosBuiltIn(string definitionId, ModuleDimensionSlot expected)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);
        Assert.Equal(expected, DimensionConfiguratorService.ResolveSlot(definition));
    }

    [Theory]
    [InlineData(350f, ModuleDimensionSlot.CozinhaSuperiorBaixo)]
    [InlineData(720f, ModuleDimensionSlot.CozinhaSuperiorMedio)]
    [InlineData(1050f, ModuleDimensionSlot.CozinhaSuperiorAlto)]
    public void ResolveSlot_Aereo_ClassificaFaixaSuperiorPorAlturaPadrao(float defaultHeight, ModuleDimensionSlot expected)
    {
        var definition = new ModuleDefinition
        {
            Id = "test-aereo",
            DisplayName = "Test",
            Category = ModuleCategory.Cozinha,
            DefaultWidth = 800f,
            DefaultHeight = defaultHeight,
            DefaultDepth = 350f,
            MinWidth = 300f,
            MaxWidth = 1200f,
            MinHeight = 300f,
            MaxHeight = 1200f,
            MinDepth = 250f,
            MaxDepth = 450f,
            IsWallMounted = true
        };

        Assert.Equal(expected, DimensionConfiguratorService.ResolveSlot(definition));
    }

    [Fact]
    public void ResolveInsertionDimensions_CozinhaInferior_UsaConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 900f;
        settings.CozinhaInferiorDepthMm = 600f;

        var (width, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(800f, width, 1);
        Assert.Equal(900f, height, 1);
        Assert.Equal(600f, depth, 1);
    }

    [Theory]
    [InlineData("canto-l-2p-esq-950")]
    [InlineData("canto-l-2p-dir-950")]
    public void ResolveInsertionDimensions_CantoL_UsaInferioresDoConfigurador(string definitionId)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 670f;
        settings.CozinhaInferiorDepthMm = 580f;

        var (width, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(950f, width, 1);
        Assert.Equal(670f, height, 1);
        Assert.Equal(580f, depth, 1);
        Assert.Equal(ModuleDimensionSlot.CozinhaInferior, DimensionConfiguratorService.ResolveSlot(definition));
    }

    [Fact]
    public void ResolveInsertionDimensions_CantoObliquo_NasceQuadrado800x800()
    {
        var definition = ModuleCatalog.GetRequired("canto-obliquo-1p-900");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 850f;
        settings.CozinhaInferiorDepthMm = 550f;

        var (width, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(800f, width, 1);
        Assert.Equal(850f, height, 1);
        Assert.Equal(800f, depth, 1);
    }

    [Fact]
    public void ResolveInsertionDimensions_AereoMedio_UsaSuperiorMedio()
    {
        var definition = ModuleCatalog.GetRequired("aereo");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaSuperiorHeightMm = 700f;
        settings.CozinhaSuperiorDepthMm = 320f;

        var (_, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(700f, height, 1);
        Assert.Equal(320f, depth, 1);
    }

    [Fact]
    public void ResolveInsertionDimensions_Comoda_UsaBancada()
    {
        var definition = ModuleCatalog.GetRequired("comoda-4g");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.DormitorioBancadaHeightMm = 900f;
        settings.DormitorioBancadaDepthMm = 500f;

        var (_, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(900f, height, 1);
        Assert.Equal(500f, depth, 1);
    }

    [Fact]
    public void ResolveInsertionDimensions_Painel_UsaLarguraDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("painel-liso");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.PainelWidthMm = 1200f;
        settings.PainelHeightMm = 2400f;
        settings.PainelThicknessMm = 18f;

        var (width, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);

        Assert.Equal(1200f, width, 1);
        Assert.Equal(2400f, height, 1);
        Assert.Equal(18f, depth, 1);
    }

    [Fact]
    public void ApplyToModules_Existentes_AtualizaAlturaSemMudarLargura()
    {
        var project = new Project();
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = project.AddModule("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        module.SetDimensions(1000f, 850f, 550f, definition);

        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 720f;
        settings.CozinhaInferiorDepthMm = 500f;

        DimensionConfiguratorService.ApplyToModules(
            project,
            settings,
            DimensionConfiguratorApplyScope.AllExistingAndNext,
            selectedModuleId: null);

        Assert.Equal(1000f, module.Width, 1);
        Assert.Equal(720f, module.Height, 1);
        Assert.Equal(500f, module.Depth, 1);
    }

    [Fact]
    public void ApplyToModules_SelectedAndNext_SoAfetaModuloSelecionado()
    {
        var project = new Project();
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var selected = project.AddModule("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        var other = project.AddModule("balcao-2-portas", new OpenTK.Mathematics.Vector3(1000f, 0f, 0f));
        selected.SetDimensions(1000f, 850f, 550f, definition);
        other.SetDimensions(1000f, 850f, 550f, definition);

        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 720f;
        settings.CozinhaInferiorDepthMm = 500f;

        DimensionConfiguratorService.ApplyToModules(
            project,
            settings,
            DimensionConfiguratorApplyScope.SelectedAndNext,
            selectedModuleId: selected.Id);

        Assert.Equal(720f, selected.Height, 1);
        Assert.Equal(500f, selected.Depth, 1);
        Assert.Equal(850f, other.Height, 1);
        Assert.Equal(550f, other.Depth, 1);
    }

    [Fact]
    public void ApplyToModules_Selected_NaoAlteraPadraoDoProjeto()
    {
        var project = new Project();
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var selected = project.AddModule("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        selected.SetDimensions(1000f, 850f, 550f, definition);

        var projectDefault = DimensionConfiguratorSettings.CreateDefault();
        projectDefault.CozinhaInferiorHeightMm = 850f;
        projectDefault.CozinhaInferiorBox.InferiorNumeric["sar-prof-fro"] = 150f;
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(projectDefault);
        DimensionConfiguratorService.SaveSettings(project, projectDefault);

        var applyOnly = projectDefault.Clone();
        applyOnly.CozinhaInferiorBox.InferiorNumeric["sar-prof-fro"] = 75f;
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(applyOnly);

        DimensionConfiguratorService.ApplyToModules(
            project,
            applyOnly,
            DimensionConfiguratorApplyScope.SelectedAndNext,
            selectedModuleId: selected.Id);

        // Altura externa do módulo não muda ao alterar só sar-prof-fro; o padrão do projeto permanece 150.
        Assert.Equal(850f, selected.Height, 1);
        var reloaded = DimensionConfiguratorService.GetSettings(project);
        Assert.Equal(850f, reloaded.CozinhaInferiorHeightMm, 1);
        Assert.Equal(150f, reloaded.CozinhaInferiorBox.InferiorNumeric["sar-prof-fro"], 1);
    }

    [Fact]
    public void Settings_RoundTrip_NoProjeto()
    {
        var project = new Project();
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 777f;
        settings.CozinhaDespenseiroHeightMm = 1400f;
        settings.DormitorioBancadaHeightMm = 880f;

        DimensionConfiguratorService.SaveSettings(project, settings);
        var loaded = DimensionConfiguratorService.GetSettings(project);

        Assert.Equal(777f, loaded.CozinhaInferiorHeightMm, 1);
        Assert.Equal(1400f, loaded.CozinhaDespenseiroHeightMm, 1);
        Assert.Equal(880f, loaded.DormitorioBancadaHeightMm, 1);
    }

    [Fact]
    public void SaveSettings_SincronizaChapasNoMetadata()
    {
        var project = new Project();
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm = 15f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm = 8f;

        DimensionConfiguratorService.SaveSettings(project, settings);

        Assert.Equal(15f, project.Metadata.PanelThicknessMm, 1);
        Assert.Equal(8f, project.Metadata.BackThicknessMm, 1);
    }

    [Fact]
    public void CreateEffectiveRules_CozinhaChapas_AplicaNaEstrutura()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm = 25f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm = 8f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.PortasFrentes).ThicknessMm = 18f;
        settings.CozinhaDoorFrontGapMm = 6f;
        settings.CozinhaInferiorShelfDepthInsetMm = 30f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);

        Assert.NotNull(rules);
        Assert.Equal(25f, rules!.Structure.PanelThicknessMm, 1);
        Assert.Equal(8f, rules.Structure.BackThicknessMm, 1);
        Assert.Equal(6f, rules.Structure.FrontGapMm, 1);
        Assert.All(rules.Structure.Shelves, s => Assert.Equal(30f, s.DepthInsetMm, 1));
    }

    [Fact]
    public void Decompose_ComChapaLateralCustomizada_UsaEspessuraDaPeca()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = new ModuleInstance
        {
            DefinitionId = definition.Id,
            Width = 800f,
            Height = 850f,
            Depth = 550f,
            Position = OpenTK.Mathematics.Vector3.Zero
        };

        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm = 25f;

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f, settings);
        var lateral = pieces.Single(p => p.Name == "Lateral");

        Assert.Equal(25f, lateral.ThicknessMm, 1);
    }

    [Fact]
    public void CreateEffectiveRules_ChapaFundoInferior_DiferenteDaLateral()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm = 18f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm = 8f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);
        var fundo = rules!.Pieces.Single(p => p.Role == "fundo");

        Assert.Equal(8f, fundo.Thickness.ConstantMm, 1);
    }

    [Fact]
    public void ChapaSettings_RoundTrip_NoProjeto()
    {
        var project = new Project();
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Prateleira).ThicknessMm = 15f;

        DimensionConfiguratorService.SaveSettings(project, settings);
        var loaded = DimensionConfiguratorService.GetSettings(project);

        Assert.Equal(15f, loaded.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Prateleira).ThicknessMm, 1);
        Assert.Equal(18f, loaded.CozinhaPanelThicknessMm, 1);
    }

    [Fact]
    public void CreateEffectiveRules_BoxAssembly_AplicaTipoFundoESarrafo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.BackPanelType = BoxBackPanelType.EncaixadoSarrafoHorizontal;
        settings.CozinhaInferiorBox.BackRecessMm = 10f;
        settings.CozinhaInferiorBox.SarrafoHeightMm = 80f;
        settings.CozinhaInferiorBox.SarrafoThicknessMm = 18f;
        settings.CozinhaInferiorBox.LateralBaseOverlapMm = 5f;
        settings.CozinhaInferiorBox.ShelfDepthInsetMm = 30f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);

        Assert.NotNull(rules);
        Assert.Equal(BoxBackPanelType.EncaixadoSarrafoHorizontal, rules!.Structure.BackPanelType);
        Assert.Equal(10f, rules.Structure.BackRecessMm, 1);
        Assert.Equal(80f, rules.Structure.SarrafoHeightMm, 1);
        Assert.Equal(5f, rules.Structure.LateralBaseOverlapMm, 1);
        Assert.All(rules.Structure.Shelves, s => Assert.Equal(30f, s.DepthInsetMm, 1));
        Assert.Contains(rules.Pieces, p => p.Role == "sarrafo");
    }

    [Fact]
    public void CreateEffectiveRules_AplicaAvancoFundoLateralEBaseDoInferior()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 9f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-afb"] = 7f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-abf"] = 3f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-rec-base"] = 5f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);

        Assert.NotNull(rules);
        Assert.Equal(9f, rules!.Structure.BackAdvanceOverLateralMm, 1);
        Assert.Equal(7f, rules.Structure.BackAdvanceOverBaseMm, 1);
        Assert.Equal(3f, rules.Structure.BaseAdvanceOverBackMm, 1);
        Assert.Equal(5f, rules.Structure.BaseRecessMm, 1);
    }

    [Fact]
    public void CreateEffectiveRules_BoxAssembly_Pregado_NaoAdicionaSarrafo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.BackPanelType = BoxBackPanelType.Pregado;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);

        Assert.NotNull(rules);
        Assert.Equal(BoxBackPanelType.Pregado, rules!.Structure.BackPanelType);
        Assert.DoesNotContain(rules.Pieces, p => p.Role == "sarrafo");
    }

    [Fact]
    public void BoxAssemblySettings_RoundTrip_NoProjeto()
    {
        var project = new Project();
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.BackPanelType = BoxBackPanelType.RebaixadoSarrafoVertical;
        settings.CozinhaInferiorBox.BackRecessMm = 12f;
        settings.CozinhaInferiorBox.ShelfDepthInsetMm = 28f;

        DimensionConfiguratorService.SaveSettings(project, settings);
        var loaded = DimensionConfiguratorService.GetSettings(project);

        Assert.Equal(BoxBackPanelType.RebaixadoSarrafoVertical, loaded.CozinhaInferiorBox.BackPanelType);
        Assert.Equal(12f, loaded.CozinhaInferiorBox.BackRecessMm, 1);
        Assert.Equal(28f, loaded.CozinhaInferiorShelfDepthInsetMm, 1);
    }

    [Fact]
    public void EnsureBoxInitialized_MigraPrateleiraLegada()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorShelfDepthInsetMm = 35f;

        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);

        Assert.Equal(35f, settings.CozinhaInferiorBox.ShelfDepthInsetMm, 1);
    }

    // — Overlay 3D (V3.7f) —

    [Fact]
    public void CreateEffectiveRules_FundoDimTrav_MapeiaCrossRailWidth()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-dim-trav"] = 50f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.Equal(50f, rules.Structure.CrossRailWidthMm, 1);
    }

    [Theory]
    [InlineData("Inteiro", BoxBackPanelLayout.Inteiro)]
    [InlineData("Rebaixado", BoxBackPanelLayout.Rebaixado)]
    [InlineData("Trav Vertical", BoxBackPanelLayout.TravessaVertical)]
    [InlineData("Trav Horizontal", BoxBackPanelLayout.TravessaHorizontal)]
    [InlineData("Sem fundo", BoxBackPanelLayout.SemFundo)]
    public void CreateEffectiveRules_TipoFundo_MapeiaLayoutConstrutivo(
        string tipo,
        BoxBackPanelLayout expected)
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = tipo;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.Equal(expected, rules.Structure.BackPanelLayout);
    }

    [Fact]
    public void CreateEffectiveRules_Inferior_MapeiaFolgasExternasDasFrentes()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-lat")] = "3";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-sup")] = "5";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-inf")] = "7";

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.Equal(3f, rules.Structure.FrontSideGapMm, 1);
        Assert.Equal(5f, rules.Structure.FrontTopGapMm, 1);
        Assert.Equal(7f, rules.Structure.FrontBottomGapMm, 1);
    }

    [Fact]
    public void CreateEffectiveRules_SarTipoSemSarrafo_OcultaSarrafo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Sem sarrafo";

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.False(rules.Structure.SarrafoVisible);
        Assert.DoesNotContain(rules.Pieces, p => p.Role == "sarrafo");
    }

    [Fact]
    public void CreateEffectiveRules_SarTipoFrontal_MantemSarrafoVisivel()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Frontal";

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.True(rules.Structure.SarrafoVisible);
    }

    [Fact]
    public void SyncInferiorToLegacy_SarSentTraVertical_DefineEncaixadoVertical()
    {
        var box = new BoxAssemblySectionSettings();
        box.InferiorChoice["fundo-tipo"] = "Inteiro";
        box.InferiorChoice["sar-sent-tra"] = "Vertical";

        BoxAssemblyConfiguratorService.SyncInferiorToLegacy(box);

        Assert.Equal(BoxBackPanelType.EncaixadoSarrafoVertical, box.BackPanelType);
    }

    [Fact]
    public void SyncInferiorToLegacy_SarSentTraHorizontal_MantemEncaixadoHorizontal()
    {
        var box = new BoxAssemblySectionSettings();
        box.InferiorChoice["fundo-tipo"] = "Inteiro";
        box.InferiorChoice["sar-sent-tra"] = "Horizontal";

        BoxAssemblyConfiguratorService.SyncInferiorToLegacy(box);

        Assert.Equal(BoxBackPanelType.EncaixadoSarrafoHorizontal, box.BackPanelType);
    }

    [Fact]
    public void SyncInferiorToLegacy_SarSentFroVertical_NaoAlteraBackPanel_MasFrontSarrafoIsVertical()
    {
        // sar-sent-fro controla o sarrafo DIANTEIRO via FrontSarrafoIsVertical,
        // e NÃO deve mais alterar o BackPanelType (que agora usa sar-sent-tra).
        var box = new BoxAssemblySectionSettings();
        box.InferiorChoice["fundo-tipo"] = "Inteiro";
        box.InferiorChoice["sar-sent-fro"] = "Vertical";

        BoxAssemblyConfiguratorService.SyncInferiorToLegacy(box);

        // BackPanelType não muda por sar-sent-fro — permanece Horizontal (default de "Inteiro").
        Assert.Equal(BoxBackPanelType.EncaixadoSarrafoHorizontal, box.BackPanelType);
    }

    [Fact]
    public void CreateEffectiveRules_Aereo_UsaGapSuperior()
    {
        var definition = ModuleCatalog.GetRequired("aereo");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaSuperiorDoorFrontGapMm = 7f;

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;

        Assert.Equal(7f, rules.Structure.FrontGapMm, 1);
    }

    [Fact]
    public void FrentesPortas_SyncToLegacy_PropagaGapSuperiorEDespenseiro()
    {
        var portas = new CozinhaFrentesPortasSettings();
        FrentesPortasConfiguratorService.EnsureInitialized(portas, 4f);

        portas.Choice[FrentesPortasConfiguratorService.MakeKey("superiores", "entre-portas")] = "7";
        portas.Choice[FrentesPortasConfiguratorService.MakeKey("despenseiros", "entre-portas")] = "3";

        var settings = DimensionConfiguratorSettings.CreateDefault();
        FrentesPortasConfiguratorService.SyncToLegacy(portas, settings);

        Assert.Equal(7f, settings.CozinhaSuperiorDoorFrontGapMm, 1);
        Assert.Equal(3f, settings.CozinhaDespenseiroDoorFrontGapMm, 1);
    }

    [Fact]
    public void EnsureProjectSettings_ProjetoNovo_CarregaPadraoDoConfigurador()
    {
        var profile = DimensionConfiguratorSettings.CreateDefault();
        profile.CozinhaInferiorHeightMm = 712f;
        DimensionConfiguratorProfileStore.Save(profile);

        var project = new Project();
        DimensionConfiguratorService.EnsureProjectSettings(project);

        Assert.NotNull(project.Metadata.DimensionSettings);
        Assert.Equal(712f, project.Metadata.DimensionSettings!.CozinhaInferiorHeightMm, 1);
    }

    [Fact]
    public void ResolveInsertionDimensions_ProjetoSemMetadata_UsaPadraoDoConfigurador()
    {
        var profile = DimensionConfiguratorSettings.CreateDefault();
        profile.CozinhaInferiorHeightMm = 670f;
        profile.CozinhaInferiorDepthMm = 520f;
        DimensionConfiguratorProfileStore.Save(profile);

        var project = new Project();
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var (_, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(project, definition);

        Assert.Equal(670f, height, 1);
        Assert.Equal(520f, depth, 1);
    }

    [Fact]
    public void SaveSettings_PersistePadraoDoConfiguradorParaNovosProjetos()
    {
        var projectA = new Project();
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 888f;
        DimensionConfiguratorService.SaveSettings(projectA, settings);

        var projectB = new Project();
        DimensionConfiguratorService.EnsureProjectSettings(projectB);

        Assert.Equal(888f, projectB.Metadata.DimensionSettings!.CozinhaInferiorHeightMm, 1);
    }

    [Fact]
    public void ProfileStore_RoundTrip_PreservaMontagemCaixa()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Frontal";
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 14f;

        DimensionConfiguratorProfileStore.Save(settings);
        var loaded = DimensionConfiguratorProfileStore.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Frontal", loaded!.CozinhaInferiorBox.InferiorChoice["sar-tipo"]);
        Assert.Equal(14f, loaded.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"], 1);
    }

    [Fact]
    public void ApplyPlacement_ComConfigurador_MantemEngenhariaNaMalha()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.Lateral).ThicknessMm = 25f;
        ChapaConfiguratorService.SyncLegacyChapaFields(settings);

        var module = new ModuleInstance
        {
            DefinitionId = definition.Id,
            Width = 800f,
            Height = 850f,
            Depth = 550f,
            Position = new OpenTK.Mathematics.Vector3(0f, 0f, 150f)
        };

        module.SetDimensions(800f, 850f, 550f, definition, settings, respectCatalogLimits: false);
        var piecesBefore = ModuleDecompositionService.Decompose(module, definition, 25f, 6f, settings);
        Assert.Equal(25f, piecesBefore.Single(p => p.Name == "Lateral").ThicknessMm, 1);

        module.ApplyPlacement(
            new OpenTK.Mathematics.Vector3(100f, 0f, 150f),
            0f,
            definition,
            dimensionSettings: settings);

        var piecesAfter = ModuleDecompositionService.Decompose(module, definition, 25f, 6f, settings);
        Assert.Equal(25f, piecesAfter.Single(p => p.Name == "Lateral").ThicknessMm, 1);
    }

    [Fact]
    public void ApplyPlacement_SemSettings_ReusaEngenhariaEmCache()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Ambos";
        settings.CozinhaInferiorBox.InferiorNumeric["sar-prof-fro"] = 150f;
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);

        var module = new ModuleInstance
        {
            DefinitionId = definition.Id,
            Width = 800f,
            Height = 850f,
            Depth = 550f,
            Position = OpenTK.Mathematics.Vector3.Zero
        };

        module.SetDimensions(800f, 850f, 550f, definition, settings, respectCatalogLimits: false);
        int sarrafosBefore = module.Mesh.Faces.Count(f => f.Label.Contains("Sarrafo", StringComparison.Ordinal));

        module.ApplyPlacement(
            new OpenTK.Mathematics.Vector3(200f, 0f, 150f),
            0f,
            definition);

        int sarrafosAfter = module.Mesh.Faces.Count(f => f.Label.Contains("Sarrafo", StringComparison.Ordinal));
        Assert.Equal(sarrafosBefore, sarrafosAfter);
        Assert.True(sarrafosAfter >= 2);
    }

    [Fact]
    public void Configurador_NumericosAssinados_PreservaRecuosEAvancosNegativos()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;

        box.InferiorNumeric["fundo-recuo"] = -12f;
        box.InferiorNumeric["sar-recuo-fro"] = -25f;
        box.InferiorNumeric["prat-recuo"] = -30f;
        BoxAssemblyConfiguratorService.SyncInferiorToLegacy(box);

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);

        Assert.NotNull(rules);
        Assert.Equal(-12f, rules!.Structure.BackRecessMm, 1);
        Assert.Equal(-25f, rules.Structure.SarrafoDianteiroRecessMm, 1);
        Assert.All(rules.Structure.Shelves, shelf => Assert.Equal(-30f, shelf.DepthInsetMm, 1));
        Assert.True(BoxAssemblyInferiorSchema.AllowsNegative("fundo-recuo"));
        Assert.True(BoxAssemblyInferiorSchema.AllowsNegative("sar-recuo-fro"));
    }

    [Fact]
    public void FrentesPortas_ValoresNegativos_ExpandemLayoutParaForaDaCaixa()
    {
        var portas = new CozinhaFrentesPortasSettings();
        FrentesPortasConfiguratorService.EnsureInitialized(portas, 4f);
        portas.Choice[FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-lat")] = "-18";
        portas.Choice[FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-inf")] = "-10";

        var structure = ModulationRulesPresets.CreateStandardBox(2, 0).Structure;
        FrentesPortasConfiguratorService.ApplyInferioresToStructure(portas, structure);
        var fronts = ModulationFrontLayout.Layout(800f, 850f, structure);

        Assert.NotEmpty(fronts);
        Assert.Equal(-18f, fronts[0].X1, 1);
        Assert.Equal(-10f, fronts[0].Y1, 1);
    }
}
