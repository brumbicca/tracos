namespace Tracos3DStudio;

/// <summary>
/// Agregado do projeto em memória: ambiente + módulos.
/// </summary>
public sealed class Project
{
    public ProjectMetadata Metadata { get; set; } = new();

    public Room Room { get; } = new();

    public List<ModuleInstance> Modules { get; } = new();

    public List<WallManualDimension> ManualWallDimensions { get; } = new();

    public void Clear()
    {
        Room.Clear();
        Modules.Clear();
        ManualWallDimensions.Clear();
        Metadata = new ProjectMetadata();
        DimensionConfiguratorService.EnsureProjectSettings(this);
    }

    public void ImportFrom(Project source)
    {
        Metadata = new ProjectMetadata
        {
            Name = source.Metadata.Name,
            WorkName = source.Metadata.WorkName,
            EnvironmentName = source.Metadata.EnvironmentName,
            CreatedUtc = source.Metadata.CreatedUtc,
            ModifiedUtc = source.Metadata.ModifiedUtc,
            ClientCode = source.Metadata.ClientCode,
            ClientCustomerType = source.Metadata.ClientCustomerType,
            ClientName = source.Metadata.ClientName,
            ClientPhone = source.Metadata.ClientPhone,
            ClientMobile = source.Metadata.ClientMobile,
            ClientEmail = source.Metadata.ClientEmail,
            ClientTaxId = source.Metadata.ClientTaxId,
            ClientAddress = source.Metadata.ClientAddress,
            ClientAddressNumber = source.Metadata.ClientAddressNumber,
            ClientAddressComplement = source.Metadata.ClientAddressComplement,
            ClientNeighborhood = source.Metadata.ClientNeighborhood,
            ClientDeliveryAddress = source.Metadata.ClientDeliveryAddress,
            ClientCity = source.Metadata.ClientCity,
            ClientState = source.Metadata.ClientState,
            ClientZip = source.Metadata.ClientZip,
            ClientNotes = source.Metadata.ClientNotes,
            BudgetValidityDays = source.Metadata.BudgetValidityDays,
            BudgetDiscountPercent = source.Metadata.BudgetDiscountPercent,
            BudgetPaymentTerms = source.Metadata.BudgetPaymentTerms,
            BudgetSalesPerson = source.Metadata.BudgetSalesPerson,
            BudgetCommercialNotes = source.Metadata.BudgetCommercialNotes,
            ModuleUnitPrices = source.Metadata.ModuleUnitPrices != null
                ? new Dictionary<string, decimal>(source.Metadata.ModuleUnitPrices)
                : null,
            CustomModulePrices = source.Metadata.CustomModulePrices != null
                ? new Dictionary<string, decimal>(source.Metadata.CustomModulePrices)
                : null,
            PanelThicknessMm = source.Metadata.PanelThicknessMm,
            BackThicknessMm = source.Metadata.BackThicknessMm,
            SheetLengthMm = source.Metadata.SheetLengthMm,
            SheetWidthMm = source.Metadata.SheetWidthMm,
            CutKerfMm = source.Metadata.CutKerfMm,
            SheetMarginMm = source.Metadata.SheetMarginMm,
            EdgeBandProfile = source.Metadata.EdgeBandProfile,
            ConstructionProfileId = source.Metadata.ConstructionProfileId,
            ShowAutomaticCeiling = source.Metadata.ShowAutomaticCeiling,
            WallLayerVisibility = source.Metadata.WallLayerVisibility != null
                ? new Dictionary<string, bool>(source.Metadata.WallLayerVisibility)
                : null,
            CustomLayerNames = source.Metadata.CustomLayerNames != null
                ? new Dictionary<string, string>(source.Metadata.CustomLayerNames)
                : null,
            LayerLocked = source.Metadata.LayerLocked != null
                ? new Dictionary<string, bool>(source.Metadata.LayerLocked)
                : null,
            DimensionSettings = source.Metadata.DimensionSettings?.Clone()
        };

        Room.ShowAutomaticCeiling = source.Metadata.ShowAutomaticCeiling;
        Room.Compartments.Clear();

        foreach (var compartment in source.Room.Compartments)
        {
            Room.Compartments.Add(new RoomCompartment
            {
                Id = compartment.Id,
                DisplayName = compartment.DisplayName
            });
        }

        Room.SetWalls(source.Room.Walls);
        Modules.Clear();

        foreach (var module in source.Modules)
        {
            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            var instance = new ModuleInstance
            {
                Id = module.Id,
                DefinitionId = module.DefinitionId,
                Position = module.Position,
                RotationYDegrees = module.RotationYDegrees,
                MaterialId = module.MaterialId,
                AttachedWallId = module.AttachedWallId,
                DistanceAlongWall = module.DistanceAlongWall,
                LayerId = module.LayerId,
                InstanceDisplayName = module.InstanceDisplayName,
                IsVisible = module.IsVisible,
                IsLocked = module.IsLocked,
                IsMirrored = module.IsMirrored,
                DrawerSlideType = module.DrawerSlideType
            };

            var dimSettings = DimensionConfiguratorService.GetSettings(this);
            instance.SetDimensions(
                module.Width,
                module.Height,
                module.Depth,
                definition,
                dimSettings,
                respectCatalogLimits: false);
            foreach (string label in module.HiddenPartLabels)
                instance.HiddenPartLabels.Add(label);
            Modules.Add(instance);
        }

        ManualWallDimensions.Clear();

        foreach (var dim in source.ManualWallDimensions)
        {
            ManualWallDimensions.Add(new WallManualDimension
            {
                Id = dim.Id,
                Kind = dim.Kind,
                PointA = dim.PointA,
                PointB = dim.PointB,
                PointC = dim.PointC,
                DimStart = dim.DimStart,
                DimEnd = dim.DimEnd,
                ArcRadius = dim.ArcRadius,
                DisplayValue = dim.DisplayValue
            });
        }
    }

    public ModuleInstance AddModule(string definitionId, OpenTK.Mathematics.Vector3 position)
    {
        var instance = ModuleCatalog.CreateInstance(definitionId, position);
        Modules.Add(instance);
        return instance;
    }

    public ModuleInstance? FindModule(Guid id)
    {
        foreach (var module in Modules)
        {
            if (module.Id == id)
                return module;
        }

        return null;
    }
}
