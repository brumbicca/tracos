namespace Tracos3DStudio;

public static class EletrosConfiguratorService
{
    public static void EnsureInitialized(DimensionConfiguratorSettings settings) =>
        EnsureInitialized(settings.CozinhaEletros);

    public static void EnsureInitialized(CozinhaEletrosSettings eletros)
    {
        foreach (var field in CozinhaEletrosSchema.Leaf.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                if (!eletros.Numeric.ContainsKey(field.Key))
                    eletros.Numeric[field.Key] = field.DefaultValue;
            }
            else if (!eletros.Choice.ContainsKey(field.Key))
            {
                eletros.Choice[field.Key] = field.DefaultOption;
            }
        }
    }
}
