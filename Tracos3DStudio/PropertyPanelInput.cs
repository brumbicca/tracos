using System.Globalization;

namespace Tracos3DStudio;

public static class PropertyPanelInput
{
    public static bool TryParseMm(string? text, out float value)
    {
        value = 0f;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string normalized = text.Trim().Replace(",", ".");

        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryReadWallDimensions(
        string? lengthText,
        string? heightText,
        string? thicknessText,
        out float length,
        out float height,
        out float thickness)
    {
        length = height = thickness = 0f;

        return TryParseMm(lengthText, out length) &&
               TryParseMm(heightText, out height) &&
               TryParseMm(thicknessText, out thickness);
    }

    public static void ApplyWallDimensions(WallSegment wall, float length, float height, float thickness)
    {
        wall.Height = height;
        wall.Thickness = thickness;
        wall.End = wall.Start + wall.Direction * length;
    }

    public static void ApplyModuleDimensions(
        ModuleInstance module,
        ModuleDefinition definition,
        float width,
        float height,
        float depth,
        DimensionConfiguratorSettings? dimensionSettings = null,
        float? cornerMedidaA = null,
        float? cornerMedidaB = null,
        float? cornerLarguraA = null,
        float? cornerLarguraB = null)
    {
        bool isCornerL = definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight
                         || module.CornerL != null;

        if (isCornerL)
        {
            ApplyCornerLPanelDimensions(
                module,
                definition,
                height,
                cornerLarguraA ?? width,
                cornerLarguraB ?? depth,
                cornerMedidaA,
                cornerMedidaB,
                dimensionSettings);
            return;
        }

        module.SetDimensions(
            width,
            height,
            depth,
            definition,
            dimensionSettings,
            respectCatalogLimits: false,
            syncCornerArmDepthFromDepth: true);
    }

    /// <summary>
    /// Canto L: Largura A/B e Medida A/B aplicadas de forma independente (valores lógicos Cd/Ce e Pd/Pe).
    /// </summary>
    public static void ApplyCornerLPanelDimensions(
        ModuleInstance module,
        ModuleDefinition definition,
        float height,
        float larguraA,
        float larguraB,
        float? medidaA,
        float? medidaB,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        bool leftHand = module.CornerL?.IsLeftHand ??
                        (definition.ShapeKind == ModuleShapeKind.CornerLLeft ||
                         definition.Id.Contains("-esq-", StringComparison.OrdinalIgnoreCase));
        var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();

        module.Height = ModuleDimensionClamp.ClampForFreeEdit(height, settings.MaxHeightMm);

        module.CornerL ??= CornerLParams.FromModuleDefaults(
            MathF.Max(larguraA, larguraB),
            definition.DefaultDepth,
            module.Height,
            18f,
            leftHand);
        module.CornerL.IsLeftHand = leftHand;
        module.CornerL.ApplyEnvelopeLengths(larguraA, larguraB, module.Height);

        if (medidaA is > 0f && medidaB is > 0f)
            module.CornerL.ApplyArmDepths(medidaA.Value, medidaB.Value);

        module.RebuildMesh(definition, dimensionSettings);
    }

    public static bool TryReadPosition(
        string? xText,
        string? yText,
        string? zText,
        out OpenTK.Mathematics.Vector3 position)
    {
        position = OpenTK.Mathematics.Vector3.Zero;

        if (!TryParseMm(xText, out float x) ||
            !TryParseMm(yText, out float y) ||
            !TryParseMm(zText, out float z))
            return false;

        position = new OpenTK.Mathematics.Vector3(x, y, z);
        return true;
    }

    public static void ApplyModulePosition(
        ModuleInstance module,
        ModuleDefinition definition,
        OpenTK.Mathematics.Vector3 position)
    {
        module.Position = position;
        module.RebuildMesh(definition);
    }

    public static float Rotate90Degrees(float currentDegrees) =>
        (currentDegrees + 90f) % 360f;
}
