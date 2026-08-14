namespace Tracos3DStudio;

public static class GavetasConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaGavetas);

    public static void EnsureDormitorioInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.DormitorioGavetas);

    public static void EnsureInitialized(CozinhaGavetasSettings gavetas)
    {
        foreach (var node in CozinhaGavetasSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (!gavetas.Choice.ContainsKey(key))
                    gavetas.Choice[key] = field.DefaultOption;
            }
        }
    }

    public static string MakeKey(string nodeId, string fieldKey) => $"{nodeId}:{fieldKey}";
}
