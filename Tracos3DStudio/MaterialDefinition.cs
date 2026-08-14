namespace Tracos3DStudio;

public enum MaterialPricingMode
{
    FixedAddOn,
    PerSquareMeter
}

public sealed class MaterialDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string ColorHex { get; init; } = "#FFFFFF";

    public MaterialPricingMode PricingMode { get; init; } = MaterialPricingMode.FixedAddOn;

    /// <summary>Valor adicional fixo por módulo ou R$/m² conforme o modo.</summary>
    public decimal PriceValue { get; init; }
}
