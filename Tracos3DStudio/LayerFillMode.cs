namespace Tracos3DStudio;

/// <summary>Modo de preenchimento por camada (Promob — Exibir → Camadas, item 10).</summary>
public enum LayerFillMode
{
    /// <summary>Renderização padrão do projeto.</summary>
    Default,

    /// <summary>Preenchimento semitransparente — útil para camadas de referência.</summary>
    Ghost,

    /// <summary>Somente contorno/arestas, sem preenchimento sólido.</summary>
    OutlineOnly
}

public static class LayerFillModeCatalog
{
    public static IReadOnlyList<(LayerFillMode Mode, string DisplayName)> GetOptions() =>
    [
        (LayerFillMode.Default, "Padrão"),
        (LayerFillMode.Ghost, "Fantasma"),
        (LayerFillMode.OutlineOnly, "Contorno")
    ];

    public static string GetDisplayName(LayerFillMode mode) =>
        GetOptions().FirstOrDefault(o => o.Mode == mode).DisplayName ?? mode.ToString();

    public static bool ShouldDrawSolid(LayerFillMode mode) =>
        mode != LayerFillMode.OutlineOnly;

    public static float ResolveSolidAlpha(LayerFillMode mode, float baseAlpha) =>
        mode switch
        {
            LayerFillMode.Ghost => MathF.Min(baseAlpha, 0.28f),
            _ => baseAlpha
        };

    public static bool ShouldDrawSurfaceMaterials(LayerFillMode mode) =>
        mode == LayerFillMode.Default;
}
