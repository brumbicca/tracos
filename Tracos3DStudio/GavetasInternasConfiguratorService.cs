namespace Tracos3DStudio;

public static class GavetasInternasConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaGavetasInternas);

    public static void EnsureInitialized(CozinhaGavetasInternasSettings gavetasInternas)
    {
        foreach (var node in CozinhaGavetasInternasSchema.AllNodes())
        {
            foreach (var field in node.Fields)
            {
                string key = MakeKey(node.Id, field.Key);
                if (!gavetasInternas.Choice.ContainsKey(key))
                    gavetasInternas.Choice[key] = field.DefaultOption;
            }
        }
    }

    public static string MakeKey(string nodeId, string fieldKey) => $"{nodeId}:{fieldKey}";
}
