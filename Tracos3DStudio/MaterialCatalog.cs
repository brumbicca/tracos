namespace Tracos3DStudio;

public static class MaterialCatalog
{
    public const string DefaultMaterialId = "mdf-branco";

    private static readonly Dictionary<string, MaterialDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mdf-branco"] = new MaterialDefinition
        {
            Id = "mdf-branco",
            DisplayName = "MDF Branco",
            ColorHex = "#F5F5F0",
            PricingMode = MaterialPricingMode.FixedAddOn,
            PriceValue = 0m
        },
        ["mdf-madeirado"] = new MaterialDefinition
        {
            Id = "mdf-madeirado",
            DisplayName = "MDF Madeirado",
            ColorHex = "#C8A882",
            PricingMode = MaterialPricingMode.FixedAddOn,
            PriceValue = 180m
        },
        ["mdp-branco"] = new MaterialDefinition
        {
            Id = "mdp-branco",
            DisplayName = "MDP Branco",
            ColorHex = "#EFEFEA",
            PricingMode = MaterialPricingMode.FixedAddOn,
            PriceValue = -80m
        }
    };

    private static readonly Dictionary<string, MaterialDefinition> CustomDefinitions =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<MaterialDefinition> All
    {
        get
        {
            var all = new List<MaterialDefinition>(Definitions.Count + CustomDefinitions.Count);
            all.AddRange(Definitions.Values);
            all.AddRange(CustomDefinitions.Values);
            return all;
        }
    }

    public static void SetCustomMaterials(IReadOnlyList<MaterialDefinition> materials)
    {
        CustomDefinitions.Clear();

        foreach (var material in materials)
        {
            if (Definitions.ContainsKey(material.Id))
                continue;

            CustomDefinitions[material.Id] = material;
        }
    }

    public static MaterialDefinition GetRequired(string id) =>
        TryGet(id, out var definition) && definition != null
            ? definition
            : throw new KeyNotFoundException($"Material '{id}' não encontrado.");

    public static bool TryGet(string id, out MaterialDefinition? definition)
    {
        if (Definitions.TryGetValue(id, out definition))
            return true;

        return CustomDefinitions.TryGetValue(id, out definition);
    }

    public static MaterialDefinition GetDefault() => GetRequired(DefaultMaterialId);
}
