using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>V3.7d — usinagem/fita por template (.tracos-lib).</summary>
public sealed class ModulationMachiningTests
{
    [Fact]
    public void Decompose_ComRegras_PropagaFitaEFurosPorPeca()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module = CreateModule(definition);

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f);

        var lateral = pieces.Single(p => p.Name == "Lateral");
        Assert.NotNull(lateral.EdgeBandingSpec);
        Assert.Equal("Frente + topo", EdgeBandService.ComputeEdgeBand(lateral));
        Assert.Equal(ModulationDrillingPattern.Lateral, lateral.DrillingPattern);

        var fundo = pieces.Single(p => p.Name == "Fundo");
        Assert.Null(EdgeBandService.ComputeEdgeBand(fundo));
        Assert.Equal(ModulationDrillingPattern.None, fundo.DrillingPattern);

        var porta = pieces.First(p => p.Name.StartsWith("Frente porta", StringComparison.Ordinal));
        Assert.Equal("4 lados", EdgeBandService.ComputeEdgeBand(porta));
        Assert.Equal(ModulationDrillingPattern.HingeDoor, porta.DrillingPattern);
    }

    [Fact]
    public void Decompose_ComRegras_FitaEFurosCalculadosCorretamente()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module = CreateModule(definition);

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f);

        var lateral = pieces.Single(p => p.Name == "Lateral");
        var lateralHoles = CabinetDrillingService.Calculate(lateral);
        Assert.Equal("Frente + topo", EdgeBandService.ComputeEdgeBand(lateral));
        Assert.NotEmpty(lateralHoles);
        Assert.All(lateralHoles, h => Assert.Equal(DrillHoleKind.MinifixCam, h.Kind));

        var porta = pieces.First(p => p.Name.StartsWith("Frente porta", StringComparison.Ordinal));
        var portaWithHoles = porta.WithHoles(CabinetDrillingService.Calculate(porta));
        Assert.Equal("4 lados", EdgeBandService.ComputeEdgeBand(porta));
        Assert.Contains(portaWithHoles.Holes, h => h.Kind == DrillHoleKind.HingeCup);

        var fundo = pieces.Single(p => p.Name == "Fundo");
        Assert.Null(EdgeBandService.ComputeEdgeBand(fundo));
        Assert.Empty(CabinetDrillingService.Calculate(fundo));
    }

    [Fact]
    public void RoundTrip_EdgeBandingEDrillingPattern_PreservaNoTracosLib()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 1, drawerCount: 0, includeShelf: false);
        var document = new LibraryDocument
        {
            Modules =
            [
                new CustomModuleData
                {
                    Id = "machining-demo",
                    DisplayName = "Demo usinagem",
                    ModulationRules = rules
                }
            ]
        };

        var path = Path.Combine(Path.GetTempPath(), $"lib-v37d-{Guid.NewGuid()}{LibraryPersistence.FileExtension}");

        try
        {
            LibraryPersistence.SaveToFile(document, path);
            var loaded = LibraryPersistence.LoadFromFile(path);
            var lateral = loaded.Modules[0].ModulationRules!.Pieces.Single(p => p.Role == "lateral");

            Assert.NotNull(lateral.EdgeBanding);
            Assert.True(lateral.EdgeBanding!.Front);
            Assert.True(lateral.EdgeBanding.Top);
            Assert.Equal(ModulationDrillingPattern.Lateral, lateral.DrillingPattern);

            var fundo = loaded.Modules[0].ModulationRules!.Pieces.Single(p => p.Role == "fundo");
            Assert.Equal(ModulationDrillingPattern.None, fundo.DrillingPattern);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void DrillingPattern_None_SemFurosMesmoComNomeDeLateral()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Teste",
            Name = "Lateral",
            LengthMm = 550,
            WidthMm = 850,
            ThicknessMm = 18,
            MaterialName = "MDF Branco",
            DrillingPattern = ModulationDrillingPattern.None
        };

        Assert.Empty(CabinetDrillingService.Calculate(piece));
    }

    [Fact]
    public void LoadFixture_V37d_PossuiUsinagemPorPeca()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "modulacao-balcao-regras.tracos-lib");
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path), $"Fixture não encontrado: {path}");

        var loaded = LibraryPersistence.LoadFromFile(path);
        var rules = Assert.Single(loaded.Modules).ModulationRules;
        Assert.NotNull(rules);

        var lateral = rules!.Pieces.Single(p => p.Id == "lateral");
        Assert.Equal(ModulationDrillingPattern.Lateral, lateral.DrillingPattern);
        Assert.NotNull(lateral.EdgeBanding);
    }

    private static ModuleDefinition CreateDefinition(ModulationRules rules) =>
        new()
        {
            Id = "test-machining",
            DisplayName = "Teste usinagem",
            DefaultWidth = 800f,
            DefaultHeight = 850f,
            DefaultDepth = 550f,
            DoorCount = 2,
            ModulationRules = rules
        };

    private static ModuleInstance CreateModule(ModuleDefinition definition) =>
        new()
        {
            DefinitionId = definition.Id,
            InstanceDisplayName = definition.DisplayName,
            Width = 800f,
            Height = 850f,
            Depth = 550f,
            Position = OpenTK.Mathematics.Vector3.Zero
        };
}
