namespace Tracos3DStudio;

public sealed class BudgetLineItem
{
    public int ItemNumber { get; init; }

    public int RepeatCount { get; init; } = 1;

    public string QuantityText { get; init; } = "1 UN";

    public string? Reference { get; init; }

    public string? ExternalModel { get; init; }

    public Guid? ModuleId { get; init; }

    public required string Description { get; init; }

    public string? DefinitionId { get; init; }

    public float WidthMm { get; init; }

    public float HeightMm { get; init; }

    public float DepthMm { get; init; }

    public string MaterialName { get; init; } = "";

    public decimal BasePrice { get; set; }

    public decimal MaterialAddOn { get; set; }

    public int Quantity { get; init; } = 1;

    public decimal TablePrice => BasePrice + MaterialAddOn;

    public decimal UnitPrice => TablePrice;

    public decimal Total { get; init; }

    public bool HasPrice { get; init; } = true;

    public string DimensionsText =>
        $"{WidthMm:0} × {HeightMm:0} × {DepthMm:0}";
}

public sealed class BudgetSection
{
    public required string Name { get; init; }

    public required IReadOnlyList<BudgetLineItem> Items { get; init; }

    public decimal Subtotal => Items.Sum(i => i.Total);
}

public sealed class BudgetSummary
{
    public required IReadOnlyList<BudgetLineItem> Items { get; init; }

    public required IReadOnlyList<BudgetSection> Sections { get; init; }

    public string? ClientCode { get; init; }

    public ClientCustomerType ClientCustomerType { get; init; } = ClientCustomerType.Individual;

    public string? ClientName { get; init; }

    public string? ClientPhone { get; init; }

    public string? ClientMobile { get; init; }

    public string? ClientEmail { get; init; }

    public string? ClientTaxId { get; init; }

    public string? ClientAddress { get; init; }

    public string? ClientAddressNumber { get; init; }

    public string? ClientAddressComplement { get; init; }

    public string? ClientNeighborhood { get; init; }

    public string? ClientDeliveryAddress { get; init; }

    public string? ClientCity { get; init; }

    public string? ClientState { get; init; }

    public string? ClientZip { get; init; }

    public string? ClientNotes { get; init; }

    public string ProjectName { get; init; } = "Projeto sem título";

    public string? WorkName { get; init; }

    public string EnvironmentTitle { get; init; } = "Cozinhas — Ambiente 3D";

    public string? CompanyDisplayName { get; init; }

    public string? LogoPath { get; init; }

    public int BudgetValidityDays { get; init; } = 30;

    public decimal BudgetDiscountPercent { get; init; }

    public string? BudgetPaymentTerms { get; init; }

    public string? BudgetSalesPerson { get; init; }

    public string? BudgetCommercialNotes { get; init; }

    public bool HasUnpricedItems { get; init; }

    public DateTime GetBudgetValidUntil(DateTime generatedAt) =>
        generatedAt.Date.AddDays(Math.Max(1, BudgetValidityDays));

    public decimal Subtotal => Sections.Sum(s => s.Subtotal);

    public decimal DiscountAmount =>
        BudgetDiscountPercent <= 0m
            ? 0m
            : Math.Round(Subtotal * BudgetDiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

    public decimal FinalTotal =>
        Math.Max(0m, Math.Round(Subtotal - DiscountAmount, 2, MidpointRounding.AwayFromZero));

    public decimal GrandTotal => Subtotal;
}
