namespace Tracos3DStudio;

public sealed class LibraryDocument
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Name { get; set; } = "Biblioteca da marcenaria";

    public string? CompanyDisplayName { get; set; }

    public string? BudgetLogoPath { get; set; }

    public List<CustomModuleData> Modules { get; set; } = new();

    public List<CustomMaterialData> Materials { get; set; } = new();

    public Dictionary<string, decimal>? ModulePrices { get; set; }
}

public sealed class CustomModuleData
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public ModuleCategory Category { get; set; } = ModuleCategory.Cozinha;

    public string LibraryGroup { get; set; } = "";

    public string LibrarySubGroup { get; set; } = "";

    public float DefaultWidth { get; set; } = 800f;

    public float DefaultHeight { get; set; } = 850f;

    public float DefaultDepth { get; set; } = 550f;

    public float MinWidth { get; set; }

    public float MaxWidth { get; set; }

    public float MinHeight { get; set; }

    public float MaxHeight { get; set; }

    public float MinDepth { get; set; }

    public float MaxDepth { get; set; }

    public int DoorCount { get; set; }

    public int DrawerCount { get; set; }

    public bool IsWallMounted { get; set; }

    public ModulationRules? ModulationRules { get; set; }

    public ModuleDefinition ToDefinition() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Category = Category,
        LibraryGroup = LibraryGroup,
        LibrarySubGroup = LibrarySubGroup,
        DefaultWidth = DefaultWidth,
        DefaultHeight = DefaultHeight,
        DefaultDepth = DefaultDepth,
        MinWidth = MinWidth > 0 ? MinWidth : DefaultWidth * 0.5f,
        MaxWidth = MaxWidth > 0 ? MaxWidth : DefaultWidth * 1.5f,
        MinHeight = MinHeight > 0 ? MinHeight : DefaultHeight * 0.7f,
        MaxHeight = MaxHeight > 0 ? MaxHeight : DefaultHeight * 1.2f,
        MinDepth = MinDepth > 0 ? MinDepth : DefaultDepth * 0.8f,
        MaxDepth = MaxDepth > 0 ? MaxDepth : DefaultDepth * 1.2f,
        DoorCount = DoorCount,
        DrawerCount = DrawerCount,
        IsWallMounted = IsWallMounted,
        ModulationRules = ModulationRules
    };

    public static CustomModuleData FromDefinition(ModuleDefinition definition) => new()
    {
        Id = definition.Id,
        DisplayName = definition.DisplayName,
        Category = definition.Category,
        LibraryGroup = definition.LibraryGroup,
        LibrarySubGroup = definition.LibrarySubGroup,
        DefaultWidth = definition.DefaultWidth,
        DefaultHeight = definition.DefaultHeight,
        DefaultDepth = definition.DefaultDepth,
        MinWidth = definition.MinWidth,
        MaxWidth = definition.MaxWidth,
        MinHeight = definition.MinHeight,
        MaxHeight = definition.MaxHeight,
        MinDepth = definition.MinDepth,
        MaxDepth = definition.MaxDepth,
        DoorCount = definition.DoorCount,
        DrawerCount = definition.DrawerCount,
        IsWallMounted = definition.IsWallMounted,
        ModulationRules = definition.ModulationRules
    };
}

public sealed class CustomMaterialData
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string ColorHex { get; set; } = "#FFFFFF";

    public MaterialPricingMode PricingMode { get; set; } = MaterialPricingMode.FixedAddOn;

    public decimal PriceValue { get; set; }
}
