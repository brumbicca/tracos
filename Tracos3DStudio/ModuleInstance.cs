using OpenTK.Mathematics;

namespace Tracos3DStudio;

public enum DrawerSlideType
{
    Telescopic,
    Concealed
}

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
    /// Espelha toda a engenharia no eixo horizontal local. É uma propriedade da
    /// instância (atalho I), portanto não exige SKUs duplicados no catálogo.
    /// </summary>
    public bool IsMirrored { get; set; }

    /// <summary>
    /// Ajustes de dimensão por peça (chave = <see cref="SelectableFace.Label"/>),
    /// aplicados sobre a carcaça paramétrica ao reconstruir o mesh.
    /// </summary>
    public Dictionary<string, PartDimensionOverride> PartOverrides { get; } = new();

    /// <summary>Peças ocultadas no ambiente; continuam pertencendo à engenharia do módulo.</summary>
    public HashSet<string> HiddenPartLabels { get; } = new(StringComparer.Ordinal);

    public bool IsPartVisible(string? label) =>
        string.IsNullOrEmpty(label) || !HiddenPartLabels.Contains(label);

    /// <summary>Parâmetros do Canto L (null nos demais módulos).</summary>
    public CornerLParams? CornerL { get; set; }

    /// <summary>Parâmetros do Canto Reto (null nos demais módulos).</summary>
    public BlindCornerParams? BlindCorner { get; set; }

    /// <summary>Quantidade de folhas do canto oblíquo, escolhida por instância.</summary>
    public int ObliqueDoorCount { get; set; } = 1;

    /// <summary>Lado das dobradiças quando o canto oblíquo usa uma porta.</summary>
    public bool ObliqueHingesOnLeft { get; set; } = true;

    /// <summary>Medidas e quantidade de portas dos terminais Diagonal/Chanfrado.</summary>
    public EndTerminalParams? EndTerminal { get; set; }

    /// <summary>Perfil de folga/ferragem usado pelas caixas de gaveta desta instância.</summary>
    public DrawerSlideType DrawerSlideType { get; set; } = DrawerSlideType.Telescopic;

    /// <summary>Recorte para coluna dos módulos especiais (null nos demais módulos).</summary>
    public SpecialColumnParams? SpecialColumn { get; set; }

    public MeshData Mesh { get; } = new();

    /// <summary>
    /// Deslocamento paramétrico da caixaria em relação à origem de inserção.
    /// Usado pelo canto reto para manter seleção e colisão alinhadas ao afastamento.
    /// É recalculado a cada reconstrução e não faz parte do arquivo do projeto.
    /// </summary>
    internal Vector3 GeometryEnvelopeLocalOffset { get; set; }

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
        if (definition.ShapeKind == ModuleShapeKind.Oblique)
            ObliqueDoorCount = Math.Clamp(definition.DoorCount, 1, 2);

        EndTerminal = definition.ShapeKind is ModuleShapeKind.EndDiagonal or ModuleShapeKind.EndChamfer
            ? EndTerminalParams.FromDefinition(definition)
            : null;

        SpecialColumn = definition.ShapeKind == ModuleShapeKind.ColumnDoors
            ? SpecialColumnParams.FromDefinition(definition)
            : null;

        if (IsCornerLDefinition(definition))
        {
            CornerL = CornerLParams.FromModuleDefaults(
                definition.DefaultWidth,
                definition.DefaultDepth,
                definition.DefaultHeight,
                panelMm: 18f,
                leftHand: IsCornerLLeftHand(definition));
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

        if (CornerL != null || IsCornerLDefinition(definition))
        {
            bool leftHand = IsCornerLLeftHand(definition);
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


        if (definition.ShapeKind == ModuleShapeKind.ColumnDoors)
        {
            SpecialColumn ??= SpecialColumnParams.FromDefinition(definition);
            SpecialColumn.ClampToModule(Width, Depth);
        }

        if (definition.ShapeKind is ModuleShapeKind.EndDiagonal or ModuleShapeKind.EndChamfer)
        {
            EndTerminal ??= EndTerminalParams.FromDefinition(definition);
            EndTerminal.ClampToModule(Width, Depth, definition.ShapeKind == ModuleShapeKind.EndChamfer);
        }

        RebuildMesh(definition, dimensionSettings);
    }

    private static bool IsCornerLDefinition(ModuleDefinition definition) =>
        definition.ShapeKind is ModuleShapeKind.CornerLLeft or ModuleShapeKind.CornerLRight ||
        definition.Id.StartsWith("canto-bifold-l-", StringComparison.OrdinalIgnoreCase);

    private static bool IsCornerLLeftHand(ModuleDefinition definition) =>
        definition.ShapeKind == ModuleShapeKind.CornerLLeft ||
        definition.Id.Contains("-esq-", StringComparison.OrdinalIgnoreCase);

    public void RebuildMesh(ModuleDefinition definition)
    {
        RebuildMesh(definition, dimensionSettings: null);
    }

    public void RebuildMesh(ModuleDefinition definition, DimensionConfiguratorSettings? dimensionSettings)
    {
        var effective = ResolveDimensionSettings(dimensionSettings);
        Mesh.Clear();
        GeometryEnvelopeLocalOffset = Vector3.Zero;
        ModuleMeshBuilder.Build(this, definition, effective);
        if (IsMirrored)
            MirrorMeshAcrossLocalWidth();
    }

    private void MirrorMeshAcrossLocalWidth()
    {
        Vector3 ReflectPoint(Vector3 world)
        {
            Vector3 local = ModulePlacementService.InverseTransformPoint(
                world, Position, RotationYDegrees);
            local.X = Width - local.X;
            return ModulePlacementService.TransformLocalPoint(
                local, Position, RotationYDegrees);
        }

        Vector3 ReflectNormal(Vector3 worldNormal)
        {
            Vector3 local = ModulePlacementService.InverseTransformPoint(
                Position + worldNormal, Position, RotationYDegrees);
            local.X = -local.X;
            Vector3 world = ModulePlacementService.TransformLocalPoint(
                local, Vector3.Zero, RotationYDegrees);
            return world.LengthSquared > 0f ? Vector3.Normalize(world) : world;
        }

        for (int i = 0; i < Mesh.Vertices.Count; i++)
            Mesh.Vertices[i] = ReflectPoint(Mesh.Vertices[i]);
        for (int i = 0; i < Mesh.Normals.Count; i++)
            Mesh.Normals[i] = ReflectNormal(Mesh.Normals[i]);

        // A reflexão troca a orientação dos triângulos; inverter B/C conserva
        // as normais externas e o back-face culling correto.
        for (int i = 0; i + 2 < Mesh.Indices.Count; i += 3)
            (Mesh.Indices[i + 1], Mesh.Indices[i + 2]) =
                (Mesh.Indices[i + 2], Mesh.Indices[i + 1]);

        for (int i = 0; i < Mesh.Faces.Count; i++)
        {
            var face = Mesh.Faces[i];
            Vector3[] vertices = face.Vertices.Select(ReflectPoint).Reverse().ToArray();
            Mesh.Faces[i] = new SelectableFace
            {
                OwnerId = face.OwnerId,
                Kind = face.Kind,
                Label = MirrorPartLabel(face.Label),
                TriangleStartIndex = face.TriangleStartIndex,
                TriangleCount = face.TriangleCount,
                Vertices = vertices,
                Normal = ReflectNormal(face.Normal)
            };
        }
    }

    private static string MirrorPartLabel(string label) =>
        label
            .Replace("esquerda", "__lado__", StringComparison.OrdinalIgnoreCase)
            .Replace("direita", "esquerda", StringComparison.OrdinalIgnoreCase)
            .Replace("__lado__", "direita", StringComparison.OrdinalIgnoreCase)
            .Replace("esq.", "__lado_abrev__", StringComparison.OrdinalIgnoreCase)
            .Replace("dir.", "esq.", StringComparison.OrdinalIgnoreCase)
            .Replace("__lado_abrev__", "dir.", StringComparison.OrdinalIgnoreCase);

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

    public (Vector3 Min, Vector3 Max) GetBounds()
    {
        var nominal = ModulePlacementService.ComputeBounds(
            Position, Width, Height, Depth, RotationYDegrees);
        Vector3 envelopeOrigin = ModulePlacementService.TransformLocalPoint(
            GeometryEnvelopeLocalOffset,
            Position,
            RotationYDegrees);
        var shifted = ModulePlacementService.ComputeBounds(
            envelopeOrigin, Width, Height, Depth, RotationYDegrees);

        // O afastamento do canto é espaço reservado: a seleção/colisão deve
        // abranger tanto a origem nominal quanto a caixaria deslocada. Apenas
        // deslocar o envelope corrigia um lado, mas retirava a colisão do outro.
        return (
            Vector3.ComponentMin(nominal.Min, shifted.Min),
            Vector3.ComponentMax(nominal.Max, shifted.Max));
    }
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
