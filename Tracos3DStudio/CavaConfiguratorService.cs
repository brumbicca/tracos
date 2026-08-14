namespace Tracos3DStudio;

public static class CavaConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaCava);

    public static void EnsureInitialized(CozinhaCavaSettings cava)
    {
        foreach (var node in CozinhaCavaSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (field.Kind == BoxFieldKind.Numeric)
                {
                    if (!cava.Numeric.ContainsKey(key))
                        cava.Numeric[key] = field.DefaultValue;
                }
                else if (!cava.Choice.ContainsKey(key))
                {
                    cava.Choice[key] = field.DefaultOption;
                }
            }
        }
    }

    public static string MakeKey(string nodeId, string fieldKey) => $"{nodeId}:{fieldKey}";
}
