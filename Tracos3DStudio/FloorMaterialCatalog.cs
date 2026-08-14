namespace Tracos3DStudio;

public static class FloorMaterialCatalog
{
    public const string DefaultMaterialId = "porcelanato-branco";

    private static readonly Dictionary<string, FloorMaterialDefinition> Definitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["porcelanato-branco"] = new FloorMaterialDefinition
            {
                Id = "porcelanato-branco",
                DisplayName = "Porcelanato Branco 60×60",
                ColorHex = "#F0EEEA",
                AccentColorHex = "#D8D4CE",
                Pattern = FloorMaterialPattern.Tile,
                TileSizeMm = 600f,
                PricePerSquareMeter = 85m
            },
            ["porcelanato-cinza"] = new FloorMaterialDefinition
            {
                Id = "porcelanato-cinza",
                DisplayName = "Porcelanato Cinza 60×60",
                ColorHex = "#B8B4AE",
                AccentColorHex = "#9E9A94",
                Pattern = FloorMaterialPattern.Tile,
                TileSizeMm = 600f,
                PricePerSquareMeter = 95m
            },
            ["laminado-madeira"] = new FloorMaterialDefinition
            {
                Id = "laminado-madeira",
                DisplayName = "Laminado Madeira",
                ColorHex = "#C4A574",
                AccentColorHex = "#A88858",
                Pattern = FloorMaterialPattern.WoodPlank,
                TileSizeMm = 190f,
                PricePerSquareMeter = 72m
            },
            ["ceramica-bege"] = new FloorMaterialDefinition
            {
                Id = "ceramica-bege",
                DisplayName = "Cerâmica Bege 45×45",
                ColorHex = "#DDD5C8",
                AccentColorHex = "#C8BFB0",
                Pattern = FloorMaterialPattern.Tile,
                TileSizeMm = 450f,
                PricePerSquareMeter = 48m
            },
            ["cimento-queimado"] = new FloorMaterialDefinition
            {
                Id = "cimento-queimado",
                DisplayName = "Cimento Queimado",
                ColorHex = "#9A9590",
                AccentColorHex = "#8A8580",
                Pattern = FloorMaterialPattern.Solid,
                PricePerSquareMeter = 110m
            },
            ["vinil-cinza"] = new FloorMaterialDefinition
            {
                Id = "vinil-cinza",
                DisplayName = "Piso Vinílico Cinza",
                ColorHex = "#A8A4A0",
                AccentColorHex = "#989490",
                Pattern = FloorMaterialPattern.Solid,
                PricePerSquareMeter = 65m
            }
        };

    public static IReadOnlyCollection<FloorMaterialDefinition> All => Definitions.Values;

    public static FloorMaterialDefinition GetRequired(string id) =>
        TryGet(id, out var definition) && definition != null
            ? definition
            : throw new KeyNotFoundException($"Material de piso '{id}' não encontrado.");

    public static bool TryGet(string id, out FloorMaterialDefinition? definition) =>
        Definitions.TryGetValue(id, out definition);

    public static FloorMaterialDefinition GetDefault() => GetRequired(DefaultMaterialId);
}
