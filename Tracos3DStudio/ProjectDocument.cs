namespace Tracos3DStudio;

public sealed class ProjectDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public ProjectMetadata Metadata { get; set; } = new();

    public List<WallSegmentData> Walls { get; set; } = new();

    public List<RoomCompartmentData> Compartments { get; set; } = new();

    public List<ModuleInstanceData> Modules { get; set; } = new();

    public List<WallManualDimensionData> ManualDimensions { get; set; } = new();

    public FloorSurfaceData? Floor { get; set; }
}

public sealed class FloorSurfaceData
{
    public string? DefaultMaterialId { get; set; }

    public bool ShowGrid { get; set; } = true;

    public List<FloorZoneData> Zones { get; set; } = new();
}

public sealed class FloorZoneData
{
    public Guid Id { get; set; }

    public string MaterialId { get; set; } = FloorMaterialCatalog.DefaultMaterialId;

    public string? Name { get; set; }

    public WallRegionShape Shape { get; set; } = WallRegionShape.Rectangular;

    public float MinX { get; set; }

    public float MinZ { get; set; }

    public float MaxX { get; set; }

    public float MaxZ { get; set; }

    public float CenterX { get; set; }

    public float CenterZ { get; set; }

    public float RadiusMm { get; set; }

    public float OffsetMm { get; set; }

    public float OffsetEdgeStartAlongMm { get; set; }

    public float OffsetEdgeEndAlongMm { get; set; }

    public float OffsetEdgeBottomMm { get; set; }

    public float OffsetEdgeTopMm { get; set; }

    public List<float> PolygonXMm { get; set; } = new();

    public List<float> PolygonZMm { get; set; } = new();
}

public enum ClientCustomerType
{
    Individual,
    LegalEntity
}

public sealed class ProjectMetadata
{
    public string Name { get; set; } = "Projeto sem título";

    /// <summary>Nome da obra (Promob — distinto do nome do arquivo).</summary>
    public string? WorkName { get; set; }

    /// <summary>Título do ambiente no orçamento (ex.: Cozinhas — Ambiente 3D).</summary>
    public string? EnvironmentName { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public string? ClientCode { get; set; }

    public ClientCustomerType ClientCustomerType { get; set; } = ClientCustomerType.Individual;

    public string? ClientName { get; set; }

    public string? ClientPhone { get; set; }

    public string? ClientMobile { get; set; }

    public string? ClientEmail { get; set; }

    public string? ClientTaxId { get; set; }

    public string? ClientAddress { get; set; }

    public string? ClientAddressNumber { get; set; }

    public string? ClientAddressComplement { get; set; }

    public string? ClientNeighborhood { get; set; }

    public string? ClientDeliveryAddress { get; set; }

    public string? ClientCity { get; set; }

    public string? ClientState { get; set; }

    public string? ClientZip { get; set; }

    /// <summary>Observações / anotações sobre o cliente ou a obra.</summary>
    public string? ClientNotes { get; set; }

    /// <summary>Validade da proposta comercial em dias (Promob — prazo no PDF).</summary>
    public int BudgetValidityDays { get; set; } = 30;

    /// <summary>Desconto comercial sobre o total (%). 0 = sem desconto.</summary>
    public decimal BudgetDiscountPercent { get; set; }

    /// <summary>Condições de pagamento exibidas no PDF comercial.</summary>
    public string? BudgetPaymentTerms { get; set; }

    /// <summary>Nome do vendedor/consultor exibido no cabeçalho do PDF.</summary>
    public string? BudgetSalesPerson { get; set; }

    /// <summary>Observações comerciais da proposta (caixa no PDF, distinto das anotações do cliente).</summary>
    public string? BudgetCommercialNotes { get; set; }

    public DateTime GetBudgetValidUntil(DateTime generatedAt) =>
        generatedAt.Date.AddDays(Math.Max(1, BudgetValidityDays));

    /// <summary>Preço base por tipo de módulo (definitionId → R$).</summary>
    public Dictionary<string, decimal>? ModuleUnitPrices { get; set; }

    /// <summary>Preço unitário customizado por instância (moduleId → R$).</summary>
    public Dictionary<string, decimal>? CustomModulePrices { get; set; }

    public float PanelThicknessMm { get; set; } = 18f;

    public float BackThicknessMm { get; set; } = 6f;

    public float SheetLengthMm { get; set; } = 2750f;

    public float SheetWidthMm { get; set; } = 1850f;

    public float CutKerfMm { get; set; } = 3f;

    public float SheetMarginMm { get; set; } = 10f;

    public string EdgeBandProfile { get; set; } = "PVC 1 mm";

    public string ConstructionProfileId { get; set; } = ConstructionProfiles.Padrao;

    public bool ShowAutomaticCeiling { get; set; } = true;

    /// <summary>Visibilidade por camada (layerId → visível).</summary>
    public Dictionary<string, bool>? WallLayerVisibility { get; set; }

    /// <summary>Camadas customizadas (layerId → nome exibido).</summary>
    public Dictionary<string, string>? CustomLayerNames { get; set; }

    /// <summary>Bloqueio por camada (layerId → bloqueada — itens não selecionáveis).</summary>
    public Dictionary<string, bool>? LayerLocked { get; set; }

    /// <summary>Modo de preenchimento por camada (Promob C7).</summary>
    public Dictionary<string, LayerFillMode>? LayerFillModes { get; set; }

    /// <summary>Padrões do Configurador de Dimensões (Promob — salvo no projeto).</summary>
    public DimensionConfiguratorSettings? DimensionSettings { get; set; }

    public bool TryGetModulePrice(string definitionId, Guid moduleId, out decimal price)
    {
        if (CustomModulePrices != null &&
            CustomModulePrices.TryGetValue(moduleId.ToString(), out price))
            return true;

        if (ModuleUnitPrices != null &&
            ModuleUnitPrices.TryGetValue(definitionId, out price))
            return true;

        price = 0m;
        return false;
    }

    public string GetWorkDisplayName() =>
        string.IsNullOrWhiteSpace(WorkName) ? Name : WorkName!;

    public string GetEnvironmentDisplayTitle() =>
        string.IsNullOrWhiteSpace(EnvironmentName) ? "Cozinhas — Ambiente 3D" : EnvironmentName!;
}

public sealed class RoomCompartmentData
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = "";
}

public sealed class WallSegmentData
{
    public Guid Id { get; set; }

    public float StartX { get; set; }

    public float StartZ { get; set; }

    public float EndX { get; set; }

    public float EndZ { get; set; }

    // Pé-direito Inicial (null em arquivos antigos → usa Height como fallback)
    public float? HeightStart { get; set; }

    // Pé-direito Final (null em arquivos antigos → usa HeightStart)
    public float? HeightEnd { get; set; }

    // Legado: mantido para compatibilidade com arquivos antigos (schema v1)
    public float Height { get; set; } = 2600f;

    public float Thickness { get; set; } = 150f;

    public WallOrientation Orientation { get; set; } = WallOrientation.Right;

    public WallMeasureSide MeasureSide { get; set; } = WallMeasureSide.Interior;

    public float FloorOffset { get; set; } = 0f;

    public float CotaAnterior { get; set; } = 0f;
    public float CotaPosterior { get; set; } = 0f;
    public float CotaInferior { get; set; } = 0f;
    public float CotaSuperior { get; set; } = 0f;

    public bool DrawBottomFace { get; set; } = false;
    public bool IsMovable { get; set; } = false;
    public bool IsVisible { get; set; } = true;

    public float ChamferStartMm { get; set; } = 0f;
    public float ChamferEndMm { get; set; } = 0f;
    public float FlechaMm { get; set; } = 0f;
    public WallConstructionType ConstructionType { get; set; } = WallConstructionType.Normal;

    public string LayerId { get; set; } = WallLayerCatalog.DefaultLayerId;

    public Guid? CompartmentId { get; set; }

    public string? InternalFaceMaterialId { get; set; }

    public string? ExternalFaceMaterialId { get; set; }

    public List<WallBandData> Bands { get; set; } = new();

    public List<WallRegionData> Regions { get; set; } = new();

    public List<WallOpeningData> Openings { get; set; } = new();
}

public sealed class WallBandData
{
    public Guid Id { get; set; }

    public bool IsHorizontal { get; set; } = true;

    public float StartMm { get; set; }

    public float EndMm { get; set; }

    public string? MaterialId { get; set; }
}

public sealed class WallRegionData
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public WallRegionShape Shape { get; set; } = WallRegionShape.Rectangular;

    public FaceType Face { get; set; } = FaceType.Internal;

    public float StartAlongMm { get; set; }

    public float EndAlongMm { get; set; }

    public float BottomMm { get; set; }

    public float TopMm { get; set; }

    public float CenterAlongMm { get; set; }

    public float CenterHeightMm { get; set; }

    public float RadiusMm { get; set; }

    public float OffsetMm { get; set; }

    public float OffsetEdgeStartAlongMm { get; set; }

    public float OffsetEdgeEndAlongMm { get; set; }

    public float OffsetEdgeBottomMm { get; set; }

    public float OffsetEdgeTopMm { get; set; }

    public string? MaterialId { get; set; }

    public float RotationDegrees { get; set; }

    public List<float> PolygonAlongMm { get; set; } = new();

    public List<float> PolygonHeightMm { get; set; } = new();
}

public sealed class WallOpeningData
{
    public Guid Id { get; set; }

    public OpeningType Type { get; set; }

    public float DistanceFromStart { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public float SillHeight { get; set; }
}

public sealed class ModuleInstanceData
{
    public Guid Id { get; set; }

    public string DefinitionId { get; set; } = "";

    public float Width { get; set; }

    public float Height { get; set; }

    public float Depth { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float RotationYDegrees { get; set; }

    public string? MaterialId { get; set; }

    public Guid? AttachedWallId { get; set; }

    public float DistanceAlongWall { get; set; }

    public string? LayerId { get; set; }

    public string? InstanceDisplayName { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; } = false;

    public Dictionary<string, PartDimensionOverrideData>? PartOverrides { get; set; }

    /// <summary>Canto L — Medida A (profundidade asa direita / Promob).</summary>
    public float? CornerMedidaA { get; set; }

    /// <summary>Canto L — Medida B (profundidade asa esquerda / Promob).</summary>
    public float? CornerMedidaB { get; set; }

    /// <summary>Canto L — Largura A (comprimento asa direita).</summary>
    public float? CornerLarguraA { get; set; }

    /// <summary>Canto L — Largura B (comprimento asa esquerda).</summary>
    public float? CornerLarguraB { get; set; }

    /// <summary>Canto Reto — Utilização do distanciador (UseSpacer).</summary>
    public bool? BlindCornerUseSpacer { get; set; }
}

public sealed class PartDimensionOverrideData
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
}

public sealed class WallManualDimensionData
{
    public Guid Id { get; set; }

    public WallManualDimensionKind Kind { get; set; }

    public float PointAX { get; set; }

    public float PointAY { get; set; }

    public float PointBX { get; set; }

    public float PointBY { get; set; }

    public float PointCX { get; set; }

    public float PointCY { get; set; }

    public float DimStartX { get; set; }

    public float DimStartY { get; set; }

    public float DimEndX { get; set; }

    public float DimEndY { get; set; }

    public float ArcRadius { get; set; }

    public float DisplayValue { get; set; }
}
