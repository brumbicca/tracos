namespace Tracos3DStudio;

public static class GavetasInternasConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaGavetasInternas);

    public static void EnsureInitialized(CozinhaGavetasInternasSettings gavetasInternas)
    {
        MigrateSlideProfile(gavetasInternas);
        foreach (var node in CozinhaGavetasInternasSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (field.Kind == BoxFieldKind.Numeric)
                {
                    if (!gavetasInternas.Numeric.ContainsKey(key))
                    {
                        gavetasInternas.Numeric[key] = gavetasInternas.Choice.TryGetValue(key, out string? legacy) &&
                                                       float.TryParse(legacy,
                                                           System.Globalization.NumberStyles.Float,
                                                           System.Globalization.CultureInfo.InvariantCulture,
                                                           out float migrated)
                            ? migrated
                            : field.DefaultValue;
                    }
                }
                else if (!gavetasInternas.Choice.ContainsKey(key))
                    gavetasInternas.Choice[key] = field.DefaultOption;
            }
        }
    }

    private static void MigrateSlideProfile(CozinhaGavetasInternasSettings gavetas)
    {
        string legacyKey = MakeKey("folgas", "folg-cor");
        string telescopicKey = MakeKey("folgas", "folg-cor-tel");
        if (gavetas.Numeric.ContainsKey(telescopicKey))
            return;

        if (gavetas.Numeric.TryGetValue(legacyKey, out float numeric) && float.IsFinite(numeric))
            gavetas.Numeric[telescopicKey] = numeric;
        else if (gavetas.Choice.TryGetValue(legacyKey, out string? raw) &&
                 float.TryParse(raw, System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture, out float migrated) &&
                 float.IsFinite(migrated))
            gavetas.Numeric[telescopicKey] = migrated;
    }

    public static string MakeKey(string nodeId, string fieldKey) => $"{nodeId}:{fieldKey}";
}
