using System.Globalization;

namespace Tracos3DStudio;

public static class BudgetService
{
    private const decimal DefaultPartPricePerM2 = 60m;

    private static readonly Dictionary<string, decimal> DefaultBasePrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["balcao-2-portas"] = 1280m,
        ["balcao-3-portas"] = 1680m,
        ["gaveteiro"] = 1420m,
        ["aereo"] = 980m,
        ["guarda-roupa-2p"] = 2480m,
        ["criado-mudo"] = 680m,
        ["comoda-4g"] = 1180m,
        ["painel-liso"] = 420m,
        ["painel-canaletado"] = 580m,
        ["painel-ripado"] = 640m
    };

    public static decimal GetDefaultBasePrice(string definitionId) =>
        DefaultBasePrices.TryGetValue(definitionId, out decimal price) ? price : 900m;

    public static BudgetSummary Build(Project project)
    {
        var moduleItems = new List<BudgetLineItem>();
        var partItems = new List<BudgetLineItem>();
        int itemNumber = 0;

        var dimensionSettings = DimensionConfiguratorService.GetSettings(project);

        foreach (var module in project.Modules)
        {
            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            var material = ResolveMaterial(module);
            var moduleLine = CreateModuleLine(project, module, definition, material, ref itemNumber);
            moduleItems.Add(moduleLine);

            foreach (var piece in ModuleDecompositionService.Decompose(
                         module,
                         definition,
                         project.Metadata.PanelThicknessMm,
                         project.Metadata.BackThicknessMm,
                         dimensionSettings))
            {
                partItems.Add(CreatePartLine(piece, material, ref itemNumber));
            }
        }

        var sections = new List<BudgetSection>();

        if (moduleItems.Count > 0)
        {
            sections.Add(new BudgetSection
            {
                Name = "— Módulos",
                Items = moduleItems
            });
        }

        if (partItems.Count > 0)
        {
            sections.Add(new BudgetSection
            {
                Name = "— Peças",
                Items = partItems
            });
        }

        bool hasUnpriced = moduleItems.Any(i => !i.HasPrice) || partItems.Any(i => !i.HasPrice);

        return new BudgetSummary
        {
            Items = moduleItems,
            Sections = sections,
            ClientCode = project.Metadata.ClientCode,
            ClientCustomerType = project.Metadata.ClientCustomerType,
            ClientName = project.Metadata.ClientName,
            ClientPhone = project.Metadata.ClientPhone,
            ClientMobile = project.Metadata.ClientMobile,
            ClientEmail = project.Metadata.ClientEmail,
            ClientTaxId = project.Metadata.ClientTaxId,
            ClientAddress = project.Metadata.ClientAddress,
            ClientAddressNumber = project.Metadata.ClientAddressNumber,
            ClientAddressComplement = project.Metadata.ClientAddressComplement,
            ClientNeighborhood = project.Metadata.ClientNeighborhood,
            ClientDeliveryAddress = project.Metadata.ClientDeliveryAddress,
            ClientCity = project.Metadata.ClientCity,
            ClientState = project.Metadata.ClientState,
            ClientZip = project.Metadata.ClientZip,
            ClientNotes = project.Metadata.ClientNotes,
            ProjectName = project.Metadata.Name,
            WorkName = project.Metadata.WorkName,
            EnvironmentTitle = project.Metadata.GetEnvironmentDisplayTitle(),
            CompanyDisplayName = LibraryState.CompanyDisplayName ?? LibraryState.LibraryName,
            LogoPath = LibraryState.BudgetLogoPath,
            HasUnpricedItems = hasUnpriced,
            BudgetValidityDays = project.Metadata.BudgetValidityDays > 0
                ? project.Metadata.BudgetValidityDays
                : 30,
            BudgetDiscountPercent = ClampDiscountPercent(project.Metadata.BudgetDiscountPercent),
            BudgetPaymentTerms = project.Metadata.BudgetPaymentTerms,
            BudgetSalesPerson = project.Metadata.BudgetSalesPerson,
            BudgetCommercialNotes = project.Metadata.BudgetCommercialNotes
        };
    }

    private static decimal ClampDiscountPercent(decimal value) =>
        value switch
        {
            < 0m => 0m,
            > 100m => 100m,
            _ => value
        };

    private static BudgetLineItem CreateModuleLine(
        Project project,
        ModuleInstance module,
        ModuleDefinition definition,
        MaterialDefinition material,
        ref int itemNumber)
    {
        decimal basePrice = project.Metadata.TryGetModulePrice(module.DefinitionId, module.Id, out decimal custom)
            ? custom
            : LibraryState.TryGetModulePrice(module.DefinitionId, out decimal libraryPrice)
                ? libraryPrice
                : GetDefaultBasePrice(module.DefinitionId);

        decimal materialAddOn = material.PricingMode switch
        {
            MaterialPricingMode.PerSquareMeter =>
                (decimal)(module.Width * module.Height / 1_000_000f) * material.PriceValue,
            _ => material.PriceValue
        };

        decimal tablePrice = basePrice + materialAddOn;
        bool hasPrice = basePrice > 0m;

        itemNumber++;

        return new BudgetLineItem
        {
            ItemNumber = itemNumber,
            RepeatCount = 1,
            QuantityText = "1 UN",
            Reference = BuildModuleReference(module, definition, material),
            ExternalModel = ExtractExternalModel(material.DisplayName),
            ModuleId = module.Id,
            Description = ModuleInstanceNamingService.GetEffectiveDisplayName(module),
            DefinitionId = module.DefinitionId,
            WidthMm = module.Width,
            HeightMm = module.Height,
            DepthMm = module.Depth,
            MaterialName = material.DisplayName,
            BasePrice = basePrice,
            MaterialAddOn = materialAddOn,
            Quantity = 1,
            Total = tablePrice,
            HasPrice = hasPrice
        };
    }

    private static BudgetLineItem CreatePartLine(
        PartPiece piece,
        MaterialDefinition material,
        ref int itemNumber)
    {
        float areaM2 = piece.LengthMm * piece.WidthMm * piece.Quantity / 1_000_000f;
        decimal pricePerM2 = GetPartPricePerM2(material);
        decimal total = (decimal)areaM2 * pricePerM2;
        bool hasPrice = pricePerM2 > 0m;

        itemNumber++;

        return new BudgetLineItem
        {
            ItemNumber = itemNumber,
            RepeatCount = piece.Quantity,
            QuantityText = FormatAreaQuantity(areaM2),
            Reference = BuildPartReference(piece, material),
            ExternalModel = ExtractExternalModel(material.DisplayName),
            Description = FormatPartDescription(piece),
            WidthMm = piece.LengthMm,
            HeightMm = piece.WidthMm,
            DepthMm = piece.ThicknessMm,
            MaterialName = piece.MaterialName,
            BasePrice = pricePerM2,
            MaterialAddOn = 0m,
            Quantity = piece.Quantity,
            Total = total,
            HasPrice = hasPrice
        };
    }

    private static MaterialDefinition ResolveMaterial(ModuleInstance module) =>
        MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null
            ? mat
            : MaterialCatalog.GetDefault();

    private static decimal GetPartPricePerM2(MaterialDefinition material) =>
        material.PricingMode switch
        {
            MaterialPricingMode.PerSquareMeter when material.PriceValue > 0m => material.PriceValue,
            _ when material.PriceValue > 0m => material.PriceValue,
            _ => DefaultPartPricePerM2
        };

    private static string FormatAreaQuantity(float areaM2) =>
        $"{areaM2.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"))} M2";

    private static string FormatPartDescription(PartPiece piece) =>
        $"{piece.Name} — {piece.ModuleName}";

    private static string ExtractExternalModel(string materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName))
            return "-";

        int space = materialName.IndexOf(' ');
        return space > 0 ? materialName[..space] : materialName;
    }

    private static string BuildModuleReference(
        ModuleInstance module,
        ModuleDefinition definition,
        MaterialDefinition material) =>
        $"1.{SanitizeReferenceToken(definition.Id)}.{module.Height:0}.{SanitizeReferenceToken(material.DisplayName)}";

    private static string BuildPartReference(PartPiece piece, MaterialDefinition material) =>
        $"1.{piece.ThicknessMm:0}.{SanitizeReferenceToken(material.DisplayName)}.MDF";

    private static string SanitizeReferenceToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Item";

        var chars = value
            .Where(char.IsLetterOrDigit)
            .Take(12)
            .ToArray();

        return chars.Length > 0 ? new string(chars) : "Item";
    }
}
