namespace Tracos3DStudio;

/// <summary>
/// Dimensões resolvidas de uma instância para o motor paramétrico (V3.7c).
/// </summary>
public sealed class ModulationDimensionContext
{
    public required float ModuleWidth { get; init; }

    public required float ModuleHeight { get; init; }

    public required float ModuleDepth { get; init; }

    public required float PanelThickness { get; init; }

    public required float BackThickness { get; init; }

    public required float FrontThickness { get; init; }

    public required float FrontGap { get; init; }

    public float InnerWidth => Math.Max(0f, ModuleWidth - 2f * PanelThickness);

    public float InnerHeight => Math.Max(0f, ModuleHeight - 2f * PanelThickness);

    public float InnerDepth => Math.Max(0f, ModuleDepth - BackThickness);

    public static ModulationDimensionContext FromModule(
        ModuleInstance module,
        ModulationRules rules,
        float fallbackPanelThicknessMm,
        float fallbackBackThicknessMm)
    {
        var structure = rules.Structure;
        return new ModulationDimensionContext
        {
            ModuleWidth = module.Width,
            ModuleHeight = module.Height,
            ModuleDepth = module.Depth,
            PanelThickness = structure.PanelThicknessMm > 0f
                ? structure.PanelThicknessMm
                : fallbackPanelThicknessMm,
            BackThickness = structure.BackThicknessMm > 0f
                ? structure.BackThicknessMm
                : fallbackBackThicknessMm,
            FrontThickness = structure.FrontThicknessMm > 0f
                ? structure.FrontThicknessMm
                : 18f,
            FrontGap = structure.FrontGapMm >= 0f
                ? structure.FrontGapMm
                : 4f
        };
    }
}

public static class ModulationDimensionResolver
{
    public static float Resolve(ModulationDimensionBinding binding, ModulationDimensionContext context)
    {
        float baseValue = binding.Source switch
        {
            ModulationDimensionSource.Constant => binding.ConstantMm,
            ModulationDimensionSource.ModuleWidth => context.ModuleWidth,
            ModulationDimensionSource.ModuleHeight => context.ModuleHeight,
            ModulationDimensionSource.ModuleDepth => context.ModuleDepth,
            ModulationDimensionSource.InnerWidth => context.InnerWidth,
            ModulationDimensionSource.InnerHeight => context.InnerHeight,
            ModulationDimensionSource.InnerDepth => context.InnerDepth,
            ModulationDimensionSource.PanelThickness => context.PanelThickness,
            ModulationDimensionSource.BackThickness => context.BackThickness,
            ModulationDimensionSource.FrontThickness => context.FrontThickness,
            ModulationDimensionSource.FrontGap => context.FrontGap,
            _ => 0f
        };

        return Math.Max(0f, baseValue * binding.Scale + binding.OffsetMm);
    }
}
