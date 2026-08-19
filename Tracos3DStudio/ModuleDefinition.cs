namespace Tracos3DStudio;

/// <summary>
/// Template de módulo da biblioteca (catálogo fixo).
/// </summary>
public sealed class ModuleDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public ModuleCategory Category { get; init; } = ModuleCategory.Cozinha;

    /// <summary>Nível 2 da biblioteca Promob (ex.: Inferiores, Superiores).</summary>
    public string LibraryGroup { get; init; } = string.Empty;

    /// <summary>Nível 3 da biblioteca Promob (ex.: Balcões, Gaveteiros, Cantos).</summary>
    public string LibrarySubGroup { get; init; } = string.Empty;

    /// <summary>Ordem de exibição na galeria (menor = primeiro), espelhando o Promob.</summary>
    public int CatalogOrder { get; init; }

    /// <summary>
    /// Permite manter uma definição somente para abrir projetos antigos sem
    /// exibi-la como opção de inserção na biblioteca atual.
    /// </summary>
    public bool IsCatalogVisible { get; init; } = true;

    /// <summary>Silhueta 3D (canto L, cego, oblíquo, extrator…).</summary>
    public ModuleShapeKind ShapeKind { get; init; } = ModuleShapeKind.Standard;

    public float DefaultWidth { get; init; }

    public float DefaultHeight { get; init; }

    public float DefaultDepth { get; init; }

    public float MinWidth { get; init; }

    public float MaxWidth { get; init; }

    public float MinHeight { get; init; }

    public float MaxHeight { get; init; }

    public float MinDepth { get; init; }

    public float MaxDepth { get; init; }

    public float FrontThickness { get; init; } = 18f;

    public int DoorCount { get; init; }

    public int DrawerCount { get; init; }

    public bool IsWallMounted { get; init; }

    public ModulationRules? ModulationRules { get; init; }

    public bool IsDecorativePanel => Category == ModuleCategory.Paineis;

    public float ClampWidth(float value) => Math.Clamp(value, MinWidth, MaxWidth);

    public float ClampHeight(float value) => Math.Clamp(value, MinHeight, MaxHeight);

    public float ClampDepth(float value) => Math.Clamp(value, MinDepth, MaxDepth);
}
