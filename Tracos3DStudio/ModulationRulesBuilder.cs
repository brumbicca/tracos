namespace Tracos3DStudio;

/// <summary>
/// Estado editável do editor de modulação (V3.7b).
/// </summary>
public sealed class ModulationEditorState
{
    public int DoorCount { get; set; } = 2;

    public int DrawerCount { get; set; }

    public float PanelThicknessMm { get; set; } = 18f;

    public float BackThicknessMm { get; set; } = 6f;

    public float FrontThicknessMm { get; set; } = 18f;

    public float FrontGapMm { get; set; } = 4f;

    public bool IncludeShelf { get; set; } = true;

    public float ShelfHeightFraction { get; set; } = 0.5f;

    public static ModulationEditorState FromModule(CustomModuleData module)
    {
        var state = new ModulationEditorState
        {
            DoorCount = module.DoorCount,
            DrawerCount = module.DrawerCount
        };

        if (module.ModulationRules?.Structure is not { } structure)
            return state;

        state.PanelThicknessMm = structure.PanelThicknessMm;
        state.BackThicknessMm = structure.BackThicknessMm;
        state.FrontThicknessMm = structure.FrontThicknessMm;
        state.FrontGapMm = structure.FrontGapMm;
        state.IncludeShelf = structure.Shelves.Count > 0;
        if (structure.Shelves.Count > 0)
            state.ShelfHeightFraction = structure.Shelves[0].HeightFraction;

        return state;
    }

    public void NormalizeCounts()
    {
        DoorCount = Math.Max(0, DoorCount);
        DrawerCount = Math.Max(0, DrawerCount);

        if (DrawerCount > 0)
            DoorCount = 0;
        else if (DoorCount == 0 && DrawerCount == 0)
            DoorCount = 2;
    }
}

/// <summary>
/// Monta <see cref="ModulationRules"/> a partir do estado do editor.
/// </summary>
public static class ModulationRulesBuilder
{
    public static ModulationRules BuildFromEditorState(ModulationEditorState state)
    {
        state.NormalizeCounts();

        bool includeShelf = state.IncludeShelf && state.DoorCount > 0 && state.DrawerCount == 0;
        var rules = ModulationRulesPresets.CreateStandardBox(state.DoorCount, state.DrawerCount, includeShelf);

        rules.Structure.PanelThicknessMm = ClampThickness(state.PanelThicknessMm, 18f);
        rules.Structure.BackThicknessMm = ClampThickness(state.BackThicknessMm, 6f);
        rules.Structure.FrontThicknessMm = ClampThickness(state.FrontThicknessMm, 18f);
        rules.Structure.FrontGapMm = float.IsFinite(state.FrontGapMm) ? state.FrontGapMm : 4f;

        if (includeShelf)
        {
            float height = Math.Clamp(state.ShelfHeightFraction, 0.05f, 0.95f);
            rules.Structure.Shelves =
            [
                new ModulationShelfRule
                {
                    Id = "prateleira-1",
                    HeightFraction = height
                }
            ];
        }

        return rules;
    }

    public static void ApplyToModule(CustomModuleData module, ModulationEditorState state)
    {
        state.NormalizeCounts();
        module.DoorCount = state.DoorCount;
        module.DrawerCount = state.DrawerCount;
        module.ModulationRules = BuildFromEditorState(state);
    }

    private static float ClampThickness(float value, float fallback) =>
        value > 0f ? value : fallback;
}
