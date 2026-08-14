using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Instância de módulo posicionada no projeto.
/// </summary>
public sealed class ModuleInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string DefinitionId { get; init; }

    public float Width { get; set; }

    public float Height { get; set; }

    public float Depth { get; set; }

    public Vector3 Position { get; set; }

    public float RotationYDegrees { get; set; }

    public Guid? AttachedWallId { get; set; }

    public float DistanceAlongWall { get; set; }

    public string MaterialId { get; set; } = MaterialCatalog.DefaultMaterialId;

    public string LayerId { get; set; } = WallLayerCatalog.DefaultModuleLayerId;

    public string? InstanceDisplayName { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Ajustes de dimensão por peça (chave = <see cref="SelectableFace.Label"/>),
    /// aplicados sobre a carcaça paramétrica ao reconstruir o mesh.
    /// </summary>
    public Dictionary<string, PartDimensionOverride> PartOverrides { get; } = new();

    /// <summary>Parâmetros do Canto L (null nos demais módulos).</summary>
    public CornerLParams? CornerL { get; set; }

    /// <summary>Parâmetros do Canto Reto (null nos demais módulos).</summary>
    public BlindCornerParams? BlindCorner { get; set; }

    public MeshData Mesh { get; } = new();

    /// <summary>
    /// Engenharia usada na última reconstrução da malha (evita perder sarrafo/chapas ao
    /// reposicionar ou selecionar sem passar <see cref="DimensionConfiguratorSettings"/>).
    /// </summary>
    private DimensionConfiguratorSettings? _cachedDimensionSettings;

    public void ApplyDefinition(ModuleDefinition definition)
    {
        Width = definition.DefaultWidth;
        Height = definition.DefaultHeight;
        Depth = definition.DefaultDepth;

        if (definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight)
        {
            CornerL = CornerLParams.FromModuleDefaults(
                definition.DefaultWidth,
                definition.DefaultDepth,
                definition.DefaultHeight,
                panelMm: 18f,
                leftHand: definition.ShapeKind == ModuleShapeKind.CornerLLeft);
            BlindCorner = null;
        }
        else if (definition.ShapeKind is ModuleShapeKind.BlindCornerLeft or ModuleShapeKind.BlindCornerRight)
        {
            CornerL = null;
            BlindCorner = BlindCornerParams.FromConfigurator(null);
        }
        else
        {
            CornerL = null;
            BlindCorner = null;
        }
    }

    /// <param name="syncCornerArmDepthFromDepth">
    /// true (inserção / aplicar configurador): <paramref name="depth"/> = Medida A/B (profundidade das asas).
    /// false (painel L×A×P): <paramref name="depth"/> = comprimento da asa no envelope; preserva Pe/Pd.
    /// </param>
    public void SetDimensions(
        float width,
        float height,
        float depth,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings = null,
        bool respectCatalogLimits = true,
        bool syncCornerArmDepthFromDepth = true)
    {
        if (respectCatalogLimits)
        {
            Width = definition.ClampWidth(width);
            Height = definition.ClampHeight(height);
            Depth = definition.ClampDepth(depth);
        }
        else
        {
            var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();
            Width = ModuleDimensionClamp.ClampForFreeEdit(width, settings.MaxWidthMm);
            Height = ModuleDimensionClamp.ClampForFreeEdit(height, settings.MaxHeightMm);
            Depth = ModuleDimensionClamp.ClampForFreeEdit(depth, settings.MaxDepthMm);
        }

        if (CornerL != null ||
            definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight)
        {
            bool leftHand = definition.ShapeKind == ModuleShapeKind.CornerLLeft;
            if (CornerL == null)
            {
                float armDepth = syncCornerArmDepthFromDepth ? Depth : definition.DefaultDepth;
                CornerL = CornerLParams.FromModuleDefaults(Width, armDepth, Height, 18f, leftHand);
                CornerL.IsLeftHand = leftHand;
                if (!syncCornerArmDepthFromDepth)
                    CornerL.ApplyEffectiveEnvelope(Width, Depth, Height);
            }
            else
            {
                CornerL.IsLeftHand = leftHand;
                if (syncCornerArmDepthFromDepth)
                    CornerL.ApplyInsertion(Width, Height, Depth);
                else
                    CornerL.ApplyEffectiveEnvelope(Width, Depth, Height);
            }
        }

        if (definition.ShapeKind is ModuleShapeKind.BlindCornerLeft or ModuleShapeKind.BlindCornerRight)
        {
            BlindCorner ??= BlindCornerParams.FromConfigurator(dimensionSettings);
            if (dimensionSettings != null)
                BlindCorner.SyncFromConfigurator(dimensionSettings);
        }

        RebuildMesh(definition, dimensionSettings);
    }

    public void RebuildMesh(ModuleDefinition definition)
    {
        RebuildMesh(definition, dimensionSettings: null);
    }

    public void RebuildMesh(ModuleDefinition definition, DimensionConfiguratorSettings? dimensionSettings)
    {
        var effective = ResolveDimensionSettings(dimensionSettings);
        Mesh.Clear();
        ModuleMeshBuilder.Build(this, definition, effective);
    }

    private DimensionConfiguratorSettings ResolveDimensionSettings(DimensionConfiguratorSettings? dimensionSettings)
    {
        if (dimensionSettings != null)
            _cachedDimensionSettings = dimensionSettings.Clone();

        return _cachedDimensionSettings?.Clone() ?? DimensionConfiguratorSettings.CreateDefault();
    }

    public void ApplyPlacement(
        Vector3 position,
        float rotationYDegrees,
        ModuleDefinition definition,
        Guid? attachedWallId = null,
        float distanceAlongWall = 0f,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        Position = position;
        RotationYDegrees = rotationYDegrees;
        AttachedWallId = attachedWallId;
        DistanceAlongWall = distanceAlongWall;
        RebuildMesh(definition, dimensionSettings);
    }

    public (Vector3 Min, Vector3 Max) GetBounds() =>
        ModulePlacementService.ComputeBounds(Position, Width, Height, Depth, RotationYDegrees);
}

/// <summary>
/// Ajuste opcional de dimensão de uma peça (mm), em coordenadas locais do módulo.
/// <para>
/// <b>Absoluto</b> (<see cref="Width"/>/<see cref="Height"/>/<see cref="Depth"/>):
/// congela o tamanho do eixo no valor informado.
/// </para>
/// <para>
/// <b>Deslocamento por face</b> (<see cref="MinXOffset"/> … <see cref="MaxZOffset"/>):
/// cada face (seta) tem seu próprio ajuste, independente do lado oposto. Positivo
/// cresce para fora; negativo recua. Como é relativo à base paramétrica, a peça
/// mantém o ajuste ao redimensionar o módulo (ex.: face direita sempre 50 mm menor).
/// </para>
/// </summary>
public sealed class PartDimensionOverride
{
    public float? Width { get; set; }

    public float? Height { get; set; }

    public float? Depth { get; set; }

    public float MinXOffset { get; set; }

    public float MaxXOffset { get; set; }

    public float MinYOffset { get; set; }

    public float MaxYOffset { get; set; }

    public float MinZOffset { get; set; }

    public float MaxZOffset { get; set; }

    public bool HasAny =>
        Width.HasValue || Height.HasValue || Depth.HasValue ||
        MinXOffset != 0f || MaxXOffset != 0f ||
        MinYOffset != 0f || MaxYOffset != 0f ||
        MinZOffset != 0f || MaxZOffset != 0f;
}
