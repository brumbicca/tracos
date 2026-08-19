using System.Globalization;

namespace Tracos3DStudio;

public static class FrentesPortasConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaFrentesPortas, settings.CozinhaDoorFrontGapMm);

    public static void EnsureDormitorioInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.DormitorioFrentesPortas, settings.DormitorioArmarioDoorFrontGapMm);

    public static void EnsureInitialized(CozinhaFrentesPortasSettings portas, float legacyDoorGapMm)
    {
        string inferioresEntreKey = MakeKey("inferiores", "entre-portas");
        bool seedInferioresFromLegacy = !portas.Choice.ContainsKey(inferioresEntreKey);

        foreach (var node in CozinhaFrentesPortasSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (field.Kind == BoxFieldKind.Numeric)
                {
                    if (!portas.Numeric.ContainsKey(key))
                        portas.Numeric[key] = field.DefaultValue;
                }
                else if (!portas.Choice.ContainsKey(key))
                {
                    if (key == inferioresEntreKey && seedInferioresFromLegacy)
                        continue;

                    portas.Choice[key] = field.DefaultOption;
                }
            }
        }

        if (seedInferioresFromLegacy)
        {
            portas.Choice[inferioresEntreKey] = ((int)Math.Round(legacyDoorGapMm))
                .ToString(CultureInfo.InvariantCulture);
        }
    }

    public static void SyncToLegacy(DimensionConfiguratorSettings settings) =>
        SyncToLegacy(settings.CozinhaFrentesPortas, settings);

    public static void SyncToLegacy(CozinhaFrentesPortasSettings portas, DimensionConfiguratorSettings settings)
    {
        if (portas.Choice.TryGetValue(MakeKey("inferiores", "entre-portas"), out var entre)
            && float.TryParse(entre, NumberStyles.Float, CultureInfo.InvariantCulture, out float gapInf))
            settings.CozinhaDoorFrontGapMm = gapInf;

        if (portas.Choice.TryGetValue(MakeKey("superiores", "entre-portas"), out var entreSup)
            && float.TryParse(entreSup, NumberStyles.Float, CultureInfo.InvariantCulture, out float gapSup))
            settings.CozinhaSuperiorDoorFrontGapMm = gapSup;

        if (portas.Choice.TryGetValue(MakeKey("despenseiros", "entre-portas"), out var entreDesp)
            && float.TryParse(entreDesp, NumberStyles.Float, CultureInfo.InvariantCulture, out float gapDesp))
            settings.CozinhaDespenseiroDoorFrontGapMm = gapDesp;
    }

    public static void ApplyInferioresToStructure(
        CozinhaFrentesPortasSettings portas,
        ModulationStructure structure)
    {
        structure.FrontSideGapMm = ReadChoiceMm(portas, "inferiores", "borda-lat", structure.FrontSideGapMm);
        structure.FrontTopGapMm = ReadChoiceMm(portas, "inferiores", "borda-sup", structure.FrontTopGapMm);
        structure.FrontBottomGapMm = ReadChoiceMm(portas, "inferiores", "borda-inf", structure.FrontBottomGapMm);
    }

    private static float ReadChoiceMm(
        CozinhaFrentesPortasSettings portas,
        string nodeId,
        string fieldKey,
        float fallback)
    {
        if (portas.Choice.TryGetValue(MakeKey(nodeId, fieldKey), out string? text) &&
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return float.IsFinite(value) ? value : fallback;

        return fallback;
    }

    public static string MakeKey(string nodeId, string fieldKey) => $"{nodeId}:{fieldKey}";
}
