using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModulationParametricTests
{
    [Fact]
    public void ResizeLargura600Para800_RecalculaBaseInternaEFrentes()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module600 = CreateModule(definition, width: 600f);
        var module800 = CreateModule(definition, width: 800f);

        var pieces600 = ModuleDecompositionService.Decompose(module600, definition, 18f, 6f);
        var pieces800 = ModuleDecompositionService.Decompose(module800, definition, 18f, 6f);

        var base600 = pieces600.Single(p => p.Name == "Base inferior");
        var base800 = pieces800.Single(p => p.Name == "Base inferior");
        var frente600 = pieces600.First(p => p.Name.StartsWith("Frente porta", StringComparison.Ordinal));
        var frente800 = pieces800.First(p => p.Name.StartsWith("Frente porta", StringComparison.Ordinal));

        Assert.Equal(564f, base600.LengthMm, 1);
        Assert.Equal(764f, base800.LengthMm, 1);

        Assert.Equal(296f, frente600.LengthMm, 1);
        Assert.Equal(396f, frente800.LengthMm, 1);
    }

    [Fact]
    public void ModulationFrontLayout_DuasPortas_RespeitaFolgas()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var rects = ModulationFrontLayout.Layout(600f, 850f, rules.Structure);

        Assert.Equal(2, rects.Count);
        Assert.All(rects, rect => Assert.Equal(ModulationFrontType.Door, rect.Type));

        float doorWidth = rects[0].X2 - rects[0].X1;
        Assert.Equal(292f, doorWidth, 1);
        Assert.Equal(6f, rects[0].X1, 1);
        Assert.Equal(298f, rects[0].X2, 1);
    }

    [Fact]
    public void ModuleMeshBuilder_ComRegras_GeraFrentesPorVao()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module = CreateModule(definition, width: 800f);
        module.RebuildMesh(definition);

        int frontCount = module.Mesh.Faces.Count(f => f.Kind == FaceKind.ModuleFront);
        // 2 portas + faces do sarrafo dianteiro (que também usa ModuleFront).
        Assert.True(frontCount >= 2, $"Esperado >= 2 faces ModuleFront, mas obteve {frontCount}");
    }

    [Fact]
    public void ModuleMeshBuilder_Portas_FicamPorForaDaCaixaria()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module = CreateModule(definition, width: 800f, depth: 550f);
        module.RebuildMesh(definition);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Porta porta-1", out Vector3 min, out Vector3 max));

        Assert.Equal(550f, min.Z, 1);
        Assert.Equal(568f, max.Z, 1);
    }

    [Fact]
    public void ModulationDimensionResolver_UsaEspessurasDaEstrutura()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 1, drawerCount: 0, includeShelf: false);
        rules.Structure.PanelThicknessMm = 20f;
        rules.Structure.BackThicknessMm = 8f;

        var module = CreateModule(CreateDefinition(rules), width: 700f, height: 800f, depth: 500f);
        var context = ModulationDimensionContext.FromModule(module, rules, 18f, 6f);

        var lateralThickness = ModulationDimensionResolver.Resolve(
            rules.Pieces.Single(p => p.Role == "lateral").Thickness,
            context);

        Assert.Equal(20f, lateralThickness, 1);
        Assert.Equal(8f, ModulationDimensionResolver.Resolve(
            rules.Pieces.Single(p => p.Role == "fundo").Thickness,
            context));
    }

    /// <summary>
    /// Reproduz o bug reportado: -50 pela seta direita e depois -50 pela esquerda
    /// deve encolher 50 mm de cada lado, sem reverter o lado direito.
    /// </summary>
    [Fact]
    public void PartFaceOffset_SetasOpostasLargura_MantemAmbosLados()
    {
        var rules = ModulationRulesPresets.CreateStandardBox(doorCount: 2, drawerCount: 0);
        var definition = CreateDefinition(rules);
        var module = CreateModule(definition, width: 800f);
        module.RebuildMesh(definition);

        const string partLabel = "Porta porta-2";
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, partLabel, out Vector3 min0, out Vector3 max0));
        Assert.True(ModulePartDimensionService.TryComputeLocalDimensions(
            module, partLabel, out Vector3 dim0));

        var setaDireita = new PartHandle(PartHandleAxis.Width, Positive: true);
        var setaEsquerda = new PartHandle(PartHandleAxis.Width, Positive: false);

        Assert.True(ModulePartEditService.TryApplyFaceOffset(
            module, partLabel, setaDireita, -50f, out _));
        module.RebuildMesh(definition);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, partLabel, out Vector3 min1, out Vector3 max1));
        Assert.True(ModulePartDimensionService.TryComputeLocalDimensions(
            module, partLabel, out Vector3 dim1));

        Assert.Equal(dim0.X - 50f, dim1.X, 1);
        Assert.Equal(min0.X, min1.X, 1);
        Assert.Equal(max0.X - 50f, max1.X, 1);

        Assert.True(ModulePartEditService.TryApplyFaceOffset(
            module, partLabel, setaEsquerda, -50f, out _));
        module.RebuildMesh(definition);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, partLabel, out Vector3 min2, out Vector3 max2));
        Assert.True(ModulePartDimensionService.TryComputeLocalDimensions(
            module, partLabel, out Vector3 dim2));

        Assert.Equal(dim0.X - 100f, dim2.X, 1);
        Assert.Equal(max1.X, max2.X, 1);
        Assert.Equal(min1.X + 50f, min2.X, 1);

        var ov = module.PartOverrides[partLabel];
        Assert.Equal(-50f, ov.MaxXOffset, 1);
        Assert.Equal(-50f, ov.MinXOffset, 1);
    }

    private static ModuleDefinition CreateDefinition(ModulationRules rules) =>
        new()
        {
            Id = "balcao-regras-param",
            DisplayName = "Balcão paramétrico",
            DefaultWidth = 600f,
            DefaultHeight = 850f,
            DefaultDepth = 550f,
            MinWidth = 300f,
            MaxWidth = 1200f,
            MinHeight = 400f,
            MaxHeight = 2200f,
            MinDepth = 300f,
            MaxDepth = 800f,
            DoorCount = 2,
            ModulationRules = rules
        };

    private static ModuleInstance CreateModule(
        ModuleDefinition definition,
        float width,
        float? height = null,
        float? depth = null)
    {
        return new ModuleInstance
        {
            DefinitionId = definition.Id,
            InstanceDisplayName = definition.DisplayName,
            Width = width,
            Height = height ?? definition.DefaultHeight,
            Depth = depth ?? definition.DefaultDepth,
            Position = Vector3.Zero
        };
    }
}
