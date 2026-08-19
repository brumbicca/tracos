using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class InferiorBoxApplicationTests
{
    [Fact]
    public void ConfiguradorInferior_MapeiaFixacoesSarrafosDivisoriasEPrateleiras()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorNumeric["ffd-afd"] = 7f;
        box.InferiorNumeric["fsl-asl"] = 6f;
        box.InferiorNumeric["fsfi-asf"] = 5f;
        box.InferiorNumeric["fsfi-afs"] = 4f;
        box.InferiorNumeric["fsfr-recuo"] = 9f;
        box.InferiorNumeric["fsfr-rebaixo"] = 11f;
        box.InferiorNumeric["fpfl-alf"] = 3f;
        box.InferiorNumeric["fpfl-afl"] = 2f;
        box.InferiorNumeric["div-recuo-fro"] = 21f;
        box.InferiorNumeric["div-recuo-tra-mov"] = 22f;
        box.InferiorNumeric["div-recuo-tra-fix"] = 23f;
        box.InferiorNumeric["div-rebaixo"] = 24f;
        box.InferiorNumeric["div-dim-dist"] = 25f;
        box.InferiorNumeric["prat-recuo-tra-mov"] = 13f;
        box.InferiorChoice["sar-tipo"] = "Inteiro";
        box.InferiorChoice["sar-seg"] = "Ambos";
        box.InferiorChoice["sar-formato"] = "Chanfrado";

        var definition = ModuleCatalog.GetRequired("balcao-3-portas");
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)!;
        var s = rules.Structure;

        Assert.Equal(7f, s.BackAdvanceOverDivisionMm);
        Assert.Equal(6f, s.SarrafoAdvanceOverLateralMm);
        Assert.Equal(5f, s.SarrafoAdvanceOverBackMm);
        Assert.Equal(4f, s.BackAdvanceOverSarrafoMm);
        Assert.Equal(9f, s.BackSarrafoRecessMm);
        Assert.Equal(11f, s.BackSarrafoLowerRecessMm);
        Assert.Equal(3f, s.LateralAdvanceOverFrontPanelMm);
        Assert.Equal(2f, s.FrontPanelAdvanceOverLateralMm);
        Assert.Equal(21f, s.DivisionFrontInsetMm);
        Assert.Equal(22f, s.DivisionMovableBackInsetMm);
        Assert.Equal(23f, s.DivisionFixedBackInsetMm);
        Assert.Equal(24f, s.DivisionBottomRecessMm);
        Assert.Equal(25f, s.DivisionSpacerWidthMm);
        Assert.True(s.SarrafoWhole);
        Assert.True(s.FrontSarrafoSegmented);
        Assert.True(s.BackSarrafoSegmented);
        Assert.True(s.SarrafoChamfered);
        Assert.All(s.Shelves, shelf => Assert.Equal(13f, shelf.BackInsetMm));
    }

    [Fact]
    public void BalcaoTresPortas_GeraDivisoriasComRecuosEDistanciadores()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var numeric = settings.CozinhaInferiorBox.InferiorNumeric;
        numeric["div-recuo-fro"] = 35f;
        numeric["div-recuo-tra-mov"] = 0f;
        numeric["div-recuo-tra-fix"] = 77f;
        numeric["div-rebaixo"] = 12f;
        numeric["div-dim-dist"] = 30f;
        numeric["ffd-afd"] = 6f;

        var definition = ModuleCatalog.GetRequired("balcao-3-portas");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(1200f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        var divisions = module.Mesh.Faces.Where(f => f.Label.StartsWith("Divisória", StringComparison.Ordinal)).ToList();
        var spacers = module.Mesh.Faces.Where(f => f.Label.StartsWith("Distanciador divisória", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(divisions);
        Assert.NotEmpty(spacers);
        float initialRear = divisions.SelectMany(f => f.Vertices).Min(v => v.Z);

        numeric["div-recuo-tra-mov"] = 24f;
        module.RebuildMesh(definition, settings);
        divisions = module.Mesh.Faces.Where(f => f.Label.StartsWith("Divisória", StringComparison.Ordinal)).ToList();

        // A divisória do 3P é móvel: alterar B desloca exatamente 24 mm;
        // o valor de C (77 mm acima) não participa dessa construção.
        Assert.Equal(initialRear + 24f,
            divisions.SelectMany(f => f.Vertices).Min(v => v.Z), 3);
        Assert.InRange(divisions.SelectMany(f => f.Vertices).Max(v => v.Z), 514f, 516f); // 550 - 35
        Assert.InRange(divisions.SelectMany(f => f.Vertices).Min(v => v.Y), 29f, 31f); // base 18 + rebaixo 12
    }

    [Fact]
    public void SarrafoInteiroChanfrado_GeraUmaPecaComArestasDiagonais()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorChoice["sar-formato"] = "Chanfrado";

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(800f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        var faces = module.Mesh.Faces.Where(f => f.Label == "Sarrafo inteiro chanfrado").ToList();
        Assert.NotEmpty(faces);
        Assert.Contains(faces, face => face.Vertices.Length == 3);
        Assert.Contains(faces.SelectMany(f => f.Vertices),
            v => v.X is > 47f and < 49f || v.X is > 751f and < 753f);
        Assert.DoesNotContain(module.Mesh.Faces, f => f.Label == "Sarrafo dianteiro" || f.Label == "Sarrafo traseiro");
    }

    [Fact]
    public void PrateleiraFixa_UsaRecuoTraseiroFixo()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["prat-recuo-tra-mov"] = 9f;
        settings.CozinhaInferiorBox.InferiorNumeric["prat-recuo-tra-fix"] = 27f;
        var structure = new ModulationStructure
        {
            Shelves = [new ModulationShelfRule { Id = "fixa", IsFixed = true }]
        };

        BoxAssemblyConfiguratorService.ApplyToStructure(
            structure, ModuleCatalog.GetRequired("balcao-2-portas"), settings);

        Assert.Equal(27f, structure.Shelves.Single().BackInsetMm);
    }

    [Fact]
    public void Decomposicao_RefleteSemFundoTravessasSarrafosEDivisorias()
    {
        var definition = ModuleCatalog.GetRequired("balcao-3-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorChoice["fundo-tipo"] = "Trav Horizontal";
        box.InferiorNumeric["fundo-dim-trav"] = 76f;
        box.InferiorChoice["sar-tipo"] = "Inteiro";
        box.InferiorNumeric["div-dim-dist"] = 28f;
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(1200f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f, settings);

        Assert.DoesNotContain(pieces, p => p.Name == "Fundo");
        Assert.Contains(pieces, p => p.Name == "Travessa de fundo horizontal" && p.Quantity == 2
                                     && MathF.Abs(p.WidthMm - 76f) < 0.1f);
        Assert.Contains(pieces, p => p.Name == "Sarrafo inteiro");
        Assert.Single(pieces, p => p.Name.StartsWith("Divisória ", StringComparison.Ordinal));
        Assert.Single(pieces, p => p.Name.StartsWith("Distanciador divisória", StringComparison.Ordinal));
        var shelves = pieces.Where(p => p.Name == "Prateleira").ToList();
        Assert.Equal(2, shelves.Count);
        Assert.NotEqual(shelves[0].LengthMm, shelves[1].LengthMm);

        box.InferiorChoice["fundo-tipo"] = "Sem fundo";
        pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f, settings);
        Assert.DoesNotContain(pieces, p => p.Name == "Fundo" || p.Name.StartsWith("Travessa de fundo", StringComparison.Ordinal));
    }

    [Fact]
    public void AplicarConfigurador_AtualizaExistentesNovasInsercoesEPersisteChaves()
    {
        var project = new Project();
        var existing = project.AddModule("balcao-3-portas", Vector3.Zero);
        Assert.Contains(existing.Mesh.Faces, f => f.Label == "Fundo");

        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Sem fundo";
        settings.CozinhaInferiorBox.InferiorNumeric["div-recuo-fro"] = 41f;
        settings.CozinhaInferiorBox.InferiorNumeric["cg-prof-trav-fun"] = 93f;
        DimensionConfiguratorService.SaveSettings(project, settings, persistGlobalProfile: false);
        DimensionConfiguratorService.ApplyToModules(project, settings,
            DimensionConfiguratorApplyScope.AllExistingAndNext, selectedModuleId: null);

        Assert.DoesNotContain(existing.Mesh.Faces, f => f.Label == "Fundo");
        Assert.Contains(existing.Mesh.Faces, f => f.Label.StartsWith("Divisória", StringComparison.Ordinal));

        var definition = ModuleCatalog.GetRequired("balcao-3-portas");
        var inserted = ModuleCatalog.CreateInstance(definition.Id, new Vector3(1500f, 0f, 0f));
        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        inserted.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        Assert.DoesNotContain(inserted.Mesh.Faces, f => f.Label == "Fundo");

        var loaded = DimensionConfiguratorService.GetSettings(project);
        Assert.Equal("Sem fundo", loaded.CozinhaInferiorBox.InferiorChoice["fundo-tipo"]);
        Assert.Equal(41f, loaded.CozinhaInferiorBox.InferiorNumeric["div-recuo-fro"]);
        Assert.Equal(93f, loaded.CozinhaInferiorBox.InferiorNumeric["cg-prof-trav-fun"]);
    }

    [Theory]
    [InlineData("Traseira", 30f, 550f)]
    [InlineData("Frente", 0f, 520f)]
    [InlineData("Centro", 15f, 535f)]
    public void FolgaEAlinhamentoLateral_AlteramProfundidadeDasLaterais(
        string alignment,
        float expectedMinZ,
        float expectedMaxZ)
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["lat-folga"] = 30f;
        settings.CozinhaInferiorBox.InferiorChoice["lat-alinhamento"] = alignment;
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(800f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        var laterals = module.Mesh.Faces
            .Where(f => f.Label is "Lateral esq." or "Lateral dir.")
            .SelectMany(f => f.Vertices).ToList();
        Assert.InRange(laterals.Min(v => v.Z), expectedMinZ - 0.5f, expectedMinZ + 0.5f);
        Assert.InRange(laterals.Max(v => v.Z), expectedMaxZ - 0.5f, expectedMaxZ + 0.5f);
    }
}
