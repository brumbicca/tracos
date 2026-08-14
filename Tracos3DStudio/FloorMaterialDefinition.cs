namespace Tracos3DStudio;

public sealed class FloorMaterialDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string ColorHex { get; init; } = "#E8E4DC";

    /// <summary>Cor alternada (rejunte, veio da madeira, etc.).</summary>
    public string AccentColorHex { get; init; } = "#D0CCC4";

    public FloorMaterialPattern Pattern { get; init; } = FloorMaterialPattern.Solid;

    /// <summary>Tamanho da peça em mm (porcelanato, tábua, etc.).</summary>
    public float TileSizeMm { get; init; } = 600f;

    public decimal PricePerSquareMeter { get; init; }
}
