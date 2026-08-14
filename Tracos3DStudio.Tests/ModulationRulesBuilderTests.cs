using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModulationRulesBuilderTests
{
    [Fact]
    public void BuildFromEditorState_Gaveteiro_ZeraPortasEGeraVãos()
    {
        var state = new ModulationEditorState
        {
            DoorCount = 2,
            DrawerCount = 3,
            IncludeShelf = true
        };

        var rules = ModulationRulesBuilder.BuildFromEditorState(state);

        Assert.Equal(0, state.DoorCount);
        Assert.Equal(3, state.DrawerCount);
        Assert.Equal(3, rules.Structure.FrontBays.Count);
        Assert.All(rules.Structure.FrontBays, bay => Assert.Equal(ModulationFrontType.Drawer, bay.Type));
        Assert.Empty(rules.Structure.Shelves);
    }

    [Fact]
    public void ApplyToModule_SincronizaContagensERegras()
    {
        var module = new CustomModuleData
        {
            Id = "teste-mod",
            DisplayName = "Teste",
            DoorCount = 2
        };

        var state = new ModulationEditorState
        {
            DoorCount = 2,
            DrawerCount = 0,
            PanelThicknessMm = 15f,
            IncludeShelf = true,
            ShelfHeightFraction = 0.4f
        };

        ModulationRulesBuilder.ApplyToModule(module, state);

        Assert.Equal(2, module.DoorCount);
        Assert.NotNull(module.ModulationRules);
        Assert.Equal(15f, module.ModulationRules!.Structure.PanelThicknessMm);
        Assert.Single(module.ModulationRules.Structure.Shelves);
        Assert.Equal(0.4f, module.ModulationRules.Structure.Shelves[0].HeightFraction);
    }

    [Fact]
    public void FromModule_RestauraEspessurasDasRegrasExistentes()
    {
        var module = new CustomModuleData
        {
            Id = "m1",
            DoorCount = 2,
            ModulationRules = ModulationRulesPresets.CreateStandardBox(2, 0)
        };
        module.ModulationRules.Structure.PanelThicknessMm = 25f;

        var state = ModulationEditorState.FromModule(module);

        Assert.Equal(25f, state.PanelThicknessMm);
        Assert.True(state.IncludeShelf);
    }
}
