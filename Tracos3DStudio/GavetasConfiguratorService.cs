namespace Tracos3DStudio;

public static class GavetasConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaGavetas);

    public static void EnsureDormitorioInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.DormitorioGavetas);

    public static void EnsureInitialized(CozinhaGavetasSettings gavetas)
    {
        MigrateSlideProfile(gavetas);
        foreach (var node in CozinhaGavetasSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (field.Kind == BoxFieldKind.Numeric)
                {
                    if (!gavetas.Numeric.ContainsKey(key))
                    {
                        gavetas.Numeric[key] = gavetas.Choice.TryGetValue(key, out string? legacy) &&
                                               float.TryParse(legacy,
                                                   System.Globalization.NumberStyles.Float,
                                                   System.Globalization.CultureInfo.InvariantCulture,
                                                   out float migrated)
                            ? migrated
                            : field.DefaultValue;
                    }
                }
                else if (!gavetas.Choice.ContainsKey(key))
                    gavetas.Choice[key] = field.DefaultOption;
            }
        }
    }

    private static void MigrateSlideProfile(CozinhaGavetasSettings gavetas)
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
