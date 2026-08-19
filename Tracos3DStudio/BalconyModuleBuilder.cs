using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Engenharia específica dos Cozinhas → Inferiores → Balcões.
/// A caixaria continua sendo produzida pelo <see cref="ModuleMeshBuilder"/>, portanto
/// chapas, fundo, base, sarrafos, prateleiras e folgas permanecem ligados ao
/// Configurador de Dimensões. Este builder acrescenta somente os interiores,
/// frentes e ferragens característicos de cada SKU.
/// </summary>
public static class BalconyModuleBuilder
{
    private static readonly HashSet<string> SinkCompositeIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "pia-1gav-basc-800",
        "pia-2p-4g-1200",
        "pia-2p-8g-1600",
        "pia-3p-4g-1600"
    };

    public static bool TryBuild(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        if (!string.Equals(
                definition.LibrarySubGroup,
                ModuleLibraryHierarchy.SubBalcoes,
                StringComparison.OrdinalIgnoreCase))
            return false;

        switch (definition.Id)
        {
            case "bal-toalheiro-150":
                BuildTowelPullOut(instance, definition, settings);
                return true;
            case "bal-adega-150":
                BuildWineCradles(instance, definition, settings, circular: false);
                return true;
            case "bal-porta-latas-200":
                BuildBasketPullOut(instance, definition, settings, mdf: false, spiceRack: false);
                return true;
            case "bal-porta-latas-mdf-200":
                BuildBasketPullOut(instance, definition, settings, mdf: true, spiceRack: false);
                return true;
            case "bal-porta-temperos-150":
                BuildBasketPullOut(instance, definition, settings, mdf: false, spiceRack: true);
                return true;
            case "bal-tulha-400":
                BuildHamper(instance, definition, settings);
                return true;
            case "bal-lixeira-400":
                BuildWasteBin(instance, definition, settings);
                return true;
            case "bal-1p-basc-600":
                BuildLiftUp(instance, definition, settings);
                return true;
            case "bal-ilha-800":
                BuildIsland(instance, definition, settings);
                return true;
            case "balcao-1p-400":
            case "balcao-2-portas":
                BuildDoorCabinet(instance, definition, settings);
                return true;
            case "balcao-3-portas":
                BuildThreeDoorCabinet(instance, definition, settings);
                return true;
            case "pia-1gav-basc-800":
                BuildSinkDrawerLiftUp(instance, definition, settings);
                return true;
            case "pia-2p-4g-1200":
                BuildSinkComposite(instance, definition, settings,
                    doorCount: 2, drawerColumnLeft: false, drawerColumnRight: true);
                return true;
            case "pia-2p-8g-1600":
                BuildSinkComposite(instance, definition, settings,
                    doorCount: 2, drawerColumnLeft: true, drawerColumnRight: true);
                return true;
            case "pia-3p-4g-1600":
                BuildSinkComposite(instance, definition, settings,
                    doorCount: 3, drawerColumnLeft: false, drawerColumnRight: true);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Lista de corte dos balcões compostos gerada a partir da mesma malha
    /// paramétrica exibida no 3D. Assim divisórias, prateleira fixa, frentes e
    /// cada peça das gavetas não retornam ao layout genérico de largura total.
    /// </summary>
    public static bool TryDecompose(
        ModuleInstance module,
        ModuleDefinition definition,
        out IReadOnlyList<PartPiece> pieces)
    {
        pieces = Array.Empty<PartPiece>();
        if (!SinkCompositeIds.Contains(definition.Id))
            return false;

        string moduleName = ModuleInstanceNamingService.GetEffectiveDisplayName(module);
        string material = MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null
            ? mat.DisplayName
            : MaterialCatalog.GetDefault().DisplayName;
        var result = new List<PartPiece>();
        var hardwareTerms = new[]
        {
            "Corrediça", "Pistão", "Eixo", "Dobradiça"
        };

        foreach (string label in module.Mesh.Faces
                     .Select(face => face.Label)
                     .Where(label => !string.IsNullOrWhiteSpace(label))
                     .Distinct(StringComparer.Ordinal))
        {
            if (hardwareTerms.Any(term =>
                    label.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!ModulePartDimensionService.TryComputeLocalBounds(
                    module, label, out var min, out var max))
                continue;

            float[] dimensions =
            [
                Math.Max(0f, max.X - min.X),
                Math.Max(0f, max.Y - min.Y),
                Math.Max(0f, max.Z - min.Z)
            ];
            Array.Sort(dimensions);
            if (dimensions[2] <= 0f)
                continue;

            result.Add(new PartPiece
            {
                ModuleId = module.Id,
                ModuleName = moduleName,
                Name = label,
                LengthMm = dimensions[2],
                WidthMm = dimensions[1],
                ThicknessMm = dimensions[0],
                Quantity = 1,
                MaterialName = material
            });
        }

        pieces = result;
        return true;
    }

    private static void BuildSinkComposite(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings,
        int doorCount,
        bool drawerColumnLeft,
        bool drawerColumnRight)
    {
        var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        var structure = GetEffectiveStructure(definition, settings);
        var shelfRule = structure.Shelves.FirstOrDefault();
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = Math.Clamp(structure.PanelThicknessMm, 1f, 50f);
        float columnWidth = Math.Min(400f, Math.Max(220f, (w - 2f * t) * 0.42f));
        float leftBoundary = drawerColumnLeft ? columnWidth : 0f;
        float rightBoundary = drawerColumnRight ? w - columnWidth : w;
        var boundaries = new List<float>();
        if (drawerColumnLeft)
            boundaries.Add(leftBoundary);
        if (drawerColumnRight)
            boundaries.Add(rightBoundary);

        ConfigureCompositeStructure(structure, w, boundaries);
        structure.BaseFullDepth = true;
        structure.DivisionsInsideBackPanel = true;
        ModuleMeshBuilder.BuildCarcass(instance, definition, settings, includeFronts: false,
            structureMutator: carcass =>
            {
                ConfigureCompositeStructure(carcass, w, boundaries);
                carcass.BaseFullDepth = true;
                carcass.DivisionsInsideBackPanel = true;
            });

        float halfT = t * 0.5f;
        float sinkInnerX0 = drawerColumnLeft ? leftBoundary + halfT : t;
        float sinkInnerX1 = drawerColumnRight ? rightBoundary - halfT : w - t;
        AddSinkShelf(instance, structure, shelfRule, sinkInnerX0, sinkInnerX1);

        float side = Math.Max(0f, structure.FrontSideGapMm);
        float gap = Math.Max(0f, structure.FrontGapMm);
        float sinkFrontX0 = drawerColumnLeft ? leftBoundary + gap * 0.5f : side;
        float sinkFrontX1 = drawerColumnRight ? rightBoundary - gap * 0.5f : w - side;
        AddDoorFronts(instance, definition, structure, sinkFrontX0, sinkFrontX1,
            doorCount, "Porta");

        int drawerOffset = 0;
        if (drawerColumnLeft)
        {
            DrawerModuleBuilder.AddExternalDrawerColumn(
                instance, definition, settings, structure,
                side, leftBoundary - gap * 0.5f,
                t, leftBoundary - halfT,
                drawerCount: 4, drawerIndexOffset: drawerOffset);
            drawerOffset += 4;
        }

        if (drawerColumnRight)
        {
            DrawerModuleBuilder.AddExternalDrawerColumn(
                instance, definition, settings, structure,
                rightBoundary + gap * 0.5f, w - side,
                rightBoundary + halfT, w - t,
                drawerCount: 4, drawerIndexOffset: drawerOffset);
        }
    }

    private static void BuildSinkDrawerLiftUp(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        var structure = GetEffectiveStructure(definition, settings);
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = Math.Clamp(structure.PanelThicknessMm, 1f, 50f);
        ConfigureCompositeStructure(structure, w, []);
        structure.BaseFullDepth = true;
        ModuleMeshBuilder.BuildCarcass(instance, definition, settings, includeFronts: false,
            structureMutator: carcass =>
            {
                ConfigureCompositeStructure(carcass, w, []);
                carcass.BaseFullDepth = true;
            });

        // Uma porta basculante superior e uma gaveta inferior, sem multiplicar
        // gavetas pela altura inteira do módulo.
        float gap = Math.Max(0f, structure.FrontGapMm);
        float side = Math.Max(0f, structure.FrontSideGapMm);
        float bottom = Math.Max(0f, structure.FrontBottomGapMm);
        float top = Math.Max(0f, structure.FrontTopGapMm);
        float split = Math.Clamp(h * 0.5f, h * 0.38f, h * 0.62f);
        float backPlane = BackPlane(structure);
        // Prateleira interna inteira: sem recuo lateral, frontal ou traseiro.
        AddPanel(instance,
            new Vector3(t, split - t * 0.5f, backPlane),
            new Vector3(w - t, split + t * 0.5f, d),
            FaceKind.ModuleTop, "Prateleira interna");
        // O módulo documentado é 1 porta basculante superior + 1 gaveta
        // inferior; a prateleira fixa é a separação estrutural entre os dois.
        AddFrontPanel(instance, definition, structure,
            side, split + gap * 0.5f, w - side, h - top, "Porta basculante");
        DrawerModuleBuilder.AddExternalDrawerColumn(
            instance, definition, settings, structure,
            side, w - side, t, w - t,
            drawerCount: 1, drawerIndexOffset: 0,
            frontY0: bottom,
            frontY1: split - gap * 0.5f);

        AddCylinder(instance,
            new Vector3(t + 20f, h - top - 18f, d + 2f),
            new Vector3(w - t - 20f, h - top - 18f, d + 2f),
            6f, FaceKind.ModuleFront, "Eixo da porta basculante");
        AddCylinder(instance,
            new Vector3(t + 18f, split + 55f, d - 150f),
            new Vector3(t + 18f, h - 65f, d - 15f),
            6f, FaceKind.ModuleLeft, "Pistão basculante esquerdo");
        AddCylinder(instance,
            new Vector3(w - t - 18f, split + 55f, d - 150f),
            new Vector3(w - t - 18f, h - 65f, d - 15f),
            6f, FaceKind.ModuleRight, "Pistão basculante direito");
    }

    private static void ConfigureCompositeStructure(
        ModulationStructure structure,
        float moduleWidth,
        IReadOnlyList<float> boundaries)
    {
        structure.FrontBays.Clear();
        structure.Shelves.Clear();
        structure.Divisions.Clear();
        float t = Math.Clamp(structure.PanelThicknessMm, 1f, 50f);
        float innerWidth = Math.Max(1f, moduleWidth - 2f * t);

        for (int i = 0; i < boundaries.Count; i++)
        {
            float fraction = Math.Clamp((boundaries[i] - t) / innerWidth, 0.05f, 0.95f);
            structure.Divisions.Add(new ModulationDivisionRule
            {
                Id = $"divisoria-pia-{i + 1}",
                WidthFraction = fraction,
                IsFixed = true
            });
        }
    }

    private static void AddSinkShelf(
        ModuleInstance instance,
        ModulationStructure structure,
        ModulationShelfRule? shelfRule,
        float innerX0,
        float innerX1)
    {
        if (innerX1 <= innerX0 + 10f)
            return;

        float t = Math.Clamp(structure.PanelThicknessMm, 1f, 50f);
        float inset = shelfRule?.WidthInsetMm ?? 4f;
        float z0 = BackPlane(structure) + (shelfRule?.BackInsetMm ?? 0f);
        float z1 = Math.Max(z0 + 20f,
            instance.Depth - (shelfRule?.DepthInsetMm ?? 20f));
        float fraction = Math.Clamp(shelfRule?.HeightFraction ?? 0.5f, 0f, 1f);
        float y = Math.Clamp(t + (instance.Height - 2f * t) * fraction,
            t, instance.Height - 2f * t);
        AddPanel(instance,
            new Vector3(innerX0 + inset, y, z0),
            new Vector3(innerX1 - inset, y + t, z1),
            FaceKind.ModuleTop, "Prateleira pia");
    }

    private static void AddDoorFronts(
        ModuleInstance instance,
        ModuleDefinition definition,
        ModulationStructure structure,
        float x0,
        float x1,
        int doorCount,
        string labelPrefix)
    {
        int count = Math.Max(1, doorCount);
        float gap = Math.Max(0f, structure.FrontGapMm);
        float y0 = Math.Max(0f, structure.FrontBottomGapMm);
        float y1 = instance.Height - Math.Max(0f, structure.FrontTopGapMm);
        float each = Math.Max(1f, (x1 - x0 - gap * (count - 1)) / count);

        for (int i = 0; i < count; i++)
        {
            float doorX0 = x0 + i * (each + gap);
            AddFrontPanel(instance, definition, structure,
                doorX0, y0, doorX0 + each, y1, $"{labelPrefix} {i + 1}");
        }
    }

    private static void AddFrontPanel(
        ModuleInstance instance,
        ModuleDefinition definition,
        ModulationStructure structure,
        float x0,
        float y0,
        float x1,
        float y1,
        string label)
    {
        float ft = structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm
            : definition.FrontThickness;
        AddPanel(instance,
            new Vector3(x0, y0, instance.Depth),
            new Vector3(x1, y1, instance.Depth + Math.Clamp(ft, 1f, 50f)),
            FaceKind.ModuleFront, label);
    }

    private static float BackPlane(ModulationStructure structure)
    {
        float bt = Math.Max(1f, structure.BackThicknessMm);
        return structure.BackPanelLayout is BoxBackPanelLayout.SemFundo
            or BoxBackPanelLayout.TravessaHorizontal
            or BoxBackPanelLayout.TravessaVertical
                ? 0f
                : structure.BackPanelType == BoxBackPanelType.Pregado
                    ? bt
                    : structure.BackRecessMm + bt;
    }

    private static void AddPanel(
        ModuleInstance instance,
        Vector3 min,
        Vector3 max,
        FaceKind kind,
        string label)
    {
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);
        ModuleMeshBuilder.AddPanelBox(instance.Mesh, instance.Id, ToWorld,
            min, max, kind, kind, label, instance.PartOverrides);
    }

    private static void BuildDoorCabinet(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(instance, definition, settings);
        AddDoorHinges(instance, definition.DoorCount, "Dobradiça caneco");
    }

    private static void BuildThreeDoorCabinet(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildCarcass(instance, definition, settings, includeFronts: true);
        AddDoorHinges(instance, 3, "Dobradiça caneco");
    }

    /// <summary>
    /// Engenharia estrutural própria dos balcões especiais. É aplicada antes da
    /// geometria e da decomposição para que 3D e lista de corte usem a mesma regra.
    /// </summary>
    public static void ApplyStructureRules(ModuleDefinition definition, ModulationStructure structure)
    {
        bool? doubleBayOnLeft = definition.Id switch
        {
            "balcao-3-portas" => true,
            _ => null
        };

        if (doubleBayOnLeft is not bool left)
            return;

        // Uma única divisória móvel: o campo B (recuo traseiro móvel) do
        // Configurador de Dimensões passa a comandar sua profundidade.
        structure.Divisions.Clear();
        structure.Divisions.Add(new ModulationDivisionRule
        {
            Id = left ? "divisoria-2-3" : "divisoria-1-3",
            WidthFraction = left ? 2f / 3f : 1f / 3f,
            IsFixed = false
        });
    }

    private static void BuildTowelPullOut(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        BuildAccessoryCarcass(instance, definition, settings);

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float x = w * 0.5f;
        float z0 = Math.Clamp(d * 0.18f, 45f, 100f);
        float z1 = MathF.Max(z0 + 80f, d - 55f);
        float[] levels = [h * 0.28f, h * 0.52f, h * 0.76f];

        foreach (float y in levels)
            AddCylinder(instance, new Vector3(x, y, z0), new Vector3(x, y, z1), 7f,
                FaceKind.ModuleTop, "Barra do toalheiro");

        AddCylinder(instance, new Vector3(x, h * 0.18f, z0), new Vector3(x, h * 0.84f, z0), 8f,
            FaceKind.ModuleTop, "Estrutura do toalheiro");
        AddTelescopicSlides(instance, h * 0.16f, "Corrediça do toalheiro");
    }

    private static void BuildWineCradles(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings,
        bool circular)
    {
        BuildAccessoryCarcass(instance, definition, settings);

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float x = w * 0.5f;
        int rows = circular ? 5 : 4;

        for (int i = 0; i < rows; i++)
        {
            float y = h * (i + 1f) / (rows + 1f);
            float radius = Math.Clamp(w * 0.18f, 18f, 32f);
            if (circular)
            {
                AddCylinder(instance,
                    new Vector3(x, y, 65f),
                    new Vector3(x, y, MathF.Max(120f, d - 45f)),
                    radius,
                    FaceKind.ModuleFront,
                    $"Berço circular para garrafa {i + 1}",
                    segments: 16);
            }
            else
            {
                float shelfDepth = MathF.Max(120f, d - 70f);
                AddBox(instance,
                    new Vector3(18f, y - 7f, 45f),
                    new Vector3(w - 18f, y + 7f, shelfDepth),
                    FaceKind.ModuleTop,
                    $"Berço inclinado para garrafa {i + 1}");
                AddCylinder(instance,
                    new Vector3(x, y + 22f, 70f),
                    new Vector3(x, y + 22f, shelfDepth - 10f),
                    Math.Clamp(radius * 0.65f, 12f, 22f),
                    FaceKind.ModuleFront,
                    $"Garrafa de referência {i + 1}",
                    segments: 12);
            }
        }
    }

    private static void BuildBasketPullOut(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings,
        bool mdf,
        bool spiceRack)
    {
        BuildAccessoryCarcass(instance, definition, settings);
        AddSingleFront(instance, definition, settings,
            spiceRack ? "Frente extraível porta-temperos" : "Frente extraível porta-latas");

        int count = spiceRack ? 4 : 3;
        float h = instance.Height;
        var effectiveSettings = settings ?? DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(effectiveSettings);
        for (int i = 0; i < count; i++)
        {
            float y = h * (i + 1f) / (count + 1f);
            if (mdf)
                AddMdfBasket(instance, y, $"Cesto MDF {i + 1}", effectiveSettings,
                    i == count - 1 ? "pl-sup" : "pl-inf");
            else
                AddWireBasket(instance, y, spiceRack ? $"Cesto de temperos {i + 1}" : $"Cesto porta-latas {i + 1}");
        }

        AddTelescopicSlides(instance, Math.Clamp(h * 0.12f, 70f, 120f), "Corrediça telescópica do extrator");
    }

    private static void BuildHamper(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        BuildAccessoryCarcass(instance, definition, settings);
        AddSingleFront(instance, definition, settings, "Frente basculante da tulha");

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        AddOpenBin(instance,
            centerX: w * 0.5f,
            centerZ: d * 0.52f,
            y0: 55f,
            y1: h * 0.72f,
            radiusX: MathF.Max(30f, w * 0.34f),
            radiusZ: MathF.Max(45f, d * 0.30f),
            "Cesto interno da tulha");

        float hingeY = 38f;
        AddCylinder(instance, new Vector3(35f, hingeY, d - 8f), new Vector3(w - 35f, hingeY, d - 8f), 7f,
            FaceKind.ModuleFront, "Eixo basculante da tulha");
        AddBox(instance, new Vector3(18f, 60f, d * 0.52f), new Vector3(28f, h * 0.55f, d - 25f),
            FaceKind.ModuleLeft, "Braço lateral da tulha");
        AddBox(instance, new Vector3(w - 28f, 60f, d * 0.52f), new Vector3(w - 18f, h * 0.55f, d - 25f),
            FaceKind.ModuleRight, "Braço lateral da tulha");
    }

    private static void BuildWasteBin(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        BuildAccessoryCarcass(instance, definition, settings);
        AddSingleFront(instance, definition, settings, "Frente extraível da lixeira");

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float radiusX = MathF.Max(35f, w * 0.25f);
        float radiusZ = MathF.Max(45f, d * 0.25f);
        AddOpenBin(instance, w * 0.5f, d * 0.52f, 65f, h * 0.66f, radiusX, radiusZ, "Lixeira interna");
        AddTelescopicSlides(instance, 55f, "Corrediça telescópica da lixeira");
    }

    private static void BuildLiftUp(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        BuildAccessoryCarcass(instance, definition, settings, clearShelves: false);
        AddSingleFront(instance, definition, settings, "Porta basculante");

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float z = d - 20f;
        AddCylinder(instance, new Vector3(24f, h * 0.48f, z - 120f), new Vector3(24f, h * 0.80f, z), 6f,
            FaceKind.ModuleLeft, "Pistão basculante esquerdo");
        AddCylinder(instance, new Vector3(w - 24f, h * 0.48f, z - 120f), new Vector3(w - 24f, h * 0.80f, z), 6f,
            FaceKind.ModuleRight, "Pistão basculante direito");
        AddCylinder(instance, new Vector3(30f, h - 35f, d), new Vector3(w - 30f, h - 35f, d), 7f,
            FaceKind.ModuleFront, "Eixo superior da porta basculante");
    }

    private static void BuildIsland(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(instance, definition, settings);
        AddDoorHinges(instance, Math.Max(1, definition.DoorCount), "Dobradiça caneco da ilha");

        float ft = FrontThickness(definition, settings);
        AddBox(instance,
            new Vector3(0f, 0f, -ft),
            new Vector3(instance.Width, instance.Height, 0f),
            FaceKind.ModuleBack,
            "Painel traseiro de acabamento da ilha");
    }

    private static void BuildAccessoryCarcass(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings,
        bool clearShelves = true)
    {
        ModuleMeshBuilder.BuildCarcass(
            instance,
            definition,
            settings,
            includeFronts: false,
            structureMutator: structure =>
            {
                structure.FrontBays.Clear();
                structure.Divisions.Clear();
                if (clearShelves)
                    structure.Shelves.Clear();
            });
    }

    private static void AddSingleFront(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings,
        string label)
    {
        var structure = GetEffectiveStructure(definition, settings);
        float side = Math.Clamp(structure.FrontSideGapMm, -50f, instance.Width * 0.3f);
        float bottom = Math.Clamp(structure.FrontBottomGapMm, -50f, instance.Height * 0.3f);
        float top = Math.Clamp(structure.FrontTopGapMm, -50f, instance.Height * 0.3f);
        float ft = Math.Clamp(structure.FrontThicknessMm, 1f, 50f);
        AddBox(instance,
            new Vector3(side, bottom, instance.Depth),
            new Vector3(instance.Width - side, instance.Height - top, instance.Depth + ft),
            FaceKind.ModuleFront,
            label);
    }

    private static float FrontThickness(
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings) =>
        Math.Clamp(
            GetEffectiveStructure(definition, settings).FrontThicknessMm,
            1f,
            50f);

    private static ModulationStructure GetEffectiveStructure(
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings) =>
        DimensionConfiguratorService.CreateEffectiveRules(definition, settings)?.Structure
        ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount).Structure;

    private static void AddDoorHinges(ModuleInstance instance, int doorCount, string label)
    {
        int count = Math.Max(1, doorCount);
        float doorW = instance.Width / count;
        float z = instance.Depth + 4f;
        float[] levels = HingeLevels(instance.Height);

        for (int i = 0; i < count; i++)
        {
            bool hingeLeft = i % 2 == 0;
            float x = i * doorW + (hingeLeft ? 10f : doorW - 10f);
            foreach (float y in levels)
                AddCylinder(instance,
                    new Vector3(x, y - 20f, z),
                    new Vector3(x, y + 20f, z),
                    6f,
                    FaceKind.ModuleFront,
                    $"{label} porta {i + 1}");
        }
    }

    private static float[] HingeLevels(float height)
    {
        if (height < 650f)
            return [height * 0.25f, height * 0.75f];
        return [120f, height - 120f];
    }

    private static void AddTelescopicSlides(ModuleInstance instance, float y, string label)
    {
        float d = instance.Depth;
        float w = instance.Width;
        AddBox(instance, new Vector3(14f, y, 45f), new Vector3(24f, y + 12f, d - 28f),
            FaceKind.ModuleLeft, $"{label} esquerda");
        AddBox(instance, new Vector3(w - 24f, y, 45f), new Vector3(w - 14f, y + 12f, d - 28f),
            FaceKind.ModuleRight, $"{label} direita");
    }

    private static void AddWireBasket(ModuleInstance instance, float y, string label)
    {
        float x0 = 24f;
        float x1 = instance.Width - 24f;
        float z0 = 65f;
        float z1 = instance.Depth - 45f;
        float top = y + 80f;

        AddCylinder(instance, new Vector3(x0, y, z0), new Vector3(x1, y, z0), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x0, y, z1), new Vector3(x1, y, z1), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x0, top, z0), new Vector3(x1, top, z0), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x0, top, z1), new Vector3(x1, top, z1), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x0, y, z0), new Vector3(x0, top, z0), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x1, y, z0), new Vector3(x1, top, z0), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x0, y, z1), new Vector3(x0, top, z1), 4f, FaceKind.ModuleTop, label);
        AddCylinder(instance, new Vector3(x1, y, z1), new Vector3(x1, top, z1), 4f, FaceKind.ModuleTop, label);

        for (int i = 1; i <= 3; i++)
        {
            float z = z0 + (z1 - z0) * i / 4f;
            AddCylinder(instance, new Vector3(x0, y, z), new Vector3(x1, y, z), 3f,
                FaceKind.ModuleTop, label);
        }
    }

    private static void AddMdfBasket(
        ModuleInstance instance,
        float y,
        string label,
        DimensionConfiguratorSettings settings,
        string profile)
    {
        float baseLeft = 22f;
        float baseRight = instance.Width - 22f;
        float baseRear = 60f;
        float baseFront = instance.Depth - 40f;

        float topLeft = baseLeft + GavetaValue(settings, $"{profile}-sup-lat-esq");
        float topRight = baseRight - GavetaValue(settings, $"{profile}-sup-lat-dir");
        float topRear = baseRear + GavetaValue(settings, $"{profile}-sup-pos");
        float topFront = baseFront - GavetaValue(settings, $"{profile}-sup-cf");
        float lowerSide = GavetaValue(settings, $"{profile}-inf-lat");
        float lowerLeft = baseLeft + lowerSide;
        float lowerRight = baseRight - lowerSide;
        float lowerRear = baseRear + GavetaValue(settings, $"{profile}-inf-pos");
        float lowerFront = baseFront - GavetaValue(settings, $"{profile}-inf-cf");

        topLeft = Math.Clamp(topLeft, 0f, instance.Width - 20f);
        topRight = Math.Clamp(topRight, topLeft + 20f, instance.Width);
        lowerLeft = Math.Clamp(lowerLeft, 0f, instance.Width - 20f);
        lowerRight = Math.Clamp(lowerRight, lowerLeft + 20f, instance.Width);
        topRear = Math.Clamp(topRear, 0f, instance.Depth - 30f);
        topFront = Math.Clamp(topFront, topRear + 30f, instance.Depth);
        lowerRear = Math.Clamp(lowerRear, 0f, instance.Depth - 30f);
        lowerFront = Math.Clamp(lowerFront, lowerRear + 30f, instance.Depth);

        AddBox(instance, new Vector3(lowerLeft, y, lowerRear),
            new Vector3(lowerRight, y + 10f, lowerFront), FaceKind.ModuleTop, label);

        float wallLeft = Math.Min(lowerLeft, topLeft);
        float wallRight = Math.Max(lowerRight, topRight);
        float wallRear = Math.Min(lowerRear, topRear);
        float wallFront = Math.Max(lowerFront, topFront);
        AddBox(instance, new Vector3(wallLeft, y, wallRear),
            new Vector3(wallLeft + 10f, y + 75f, wallFront), FaceKind.ModuleLeft, label);
        AddBox(instance, new Vector3(wallRight - 10f, y, wallRear),
            new Vector3(wallRight, y + 75f, wallFront), FaceKind.ModuleRight, label);
        AddBox(instance, new Vector3(wallLeft, y, wallRear),
            new Vector3(wallRight, y + 75f, wallRear + 10f), FaceKind.ModuleBack, label);

        // A borda superior usa os recuos I–L independentemente da base M–O.
        AddBox(instance, new Vector3(topLeft, y + 65f, topRear),
            new Vector3(topRight, y + 75f, topRear + 10f), FaceKind.ModuleBack, label);
        AddBox(instance, new Vector3(topLeft, y + 65f, topFront - 10f),
            new Vector3(topRight, y + 75f, topFront), FaceKind.ModuleFront, label);
    }

    private static float GavetaValue(DimensionConfiguratorSettings settings, string field)
    {
        string key = GavetasConfiguratorService.MakeKey("folgas", field);
        return settings.CozinhaGavetas.Numeric.TryGetValue(key, out float value) && float.IsFinite(value)
            ? value
            : 0f;
    }

    private static void AddOpenBin(
        ModuleInstance instance,
        float centerX,
        float centerZ,
        float y0,
        float y1,
        float radiusX,
        float radiusZ,
        string label)
    {
        const int sides = 12;
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);
        var mesh = instance.Mesh;

        for (int i = 0; i < sides; i++)
        {
            float a0 = MathF.Tau * i / sides;
            float a1 = MathF.Tau * (i + 1) / sides;
            var p0 = new Vector3(centerX + MathF.Cos(a0) * radiusX, y0, centerZ + MathF.Sin(a0) * radiusZ);
            var p1 = new Vector3(centerX + MathF.Cos(a1) * radiusX, y0, centerZ + MathF.Sin(a1) * radiusZ);
            var q0 = new Vector3(p0.X, y1, p0.Z);
            var q1 = new Vector3(p1.X, y1, p1.Z);
            mesh.AddQuad(ToWorld(p0), ToWorld(p1), ToWorld(q1), ToWorld(q0), FaceKind.ModuleFront, instance.Id, label);
            mesh.AddTriangle(
                ToWorld(new Vector3(centerX, y0, centerZ)),
                ToWorld(p1),
                ToWorld(p0),
                FaceKind.ModuleBottom,
                instance.Id,
                label);
        }
    }

    private static void AddBox(
        ModuleInstance instance,
        Vector3 min,
        Vector3 max,
        FaceKind kind,
        string label)
    {
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);
        var mesh = instance.Mesh;
        Guid id = instance.Id;

        var a = ToWorld(new Vector3(min.X, min.Y, min.Z));
        var b = ToWorld(new Vector3(max.X, min.Y, min.Z));
        var c = ToWorld(new Vector3(max.X, max.Y, min.Z));
        var d = ToWorld(new Vector3(min.X, max.Y, min.Z));
        var e = ToWorld(new Vector3(min.X, min.Y, max.Z));
        var f = ToWorld(new Vector3(max.X, min.Y, max.Z));
        var g = ToWorld(new Vector3(max.X, max.Y, max.Z));
        var h = ToWorld(new Vector3(min.X, max.Y, max.Z));

        mesh.AddQuad(f, e, h, g, kind, id, label);
        mesh.AddQuad(a, b, c, d, kind, id, label);
        mesh.AddQuad(e, a, d, h, kind, id, label);
        mesh.AddQuad(b, f, g, c, kind, id, label);
        mesh.AddQuad(e, f, b, a, kind, id, label);
        mesh.AddQuad(d, c, g, h, kind, id, label);
    }

    private static void AddCylinder(
        ModuleInstance instance,
        Vector3 p0,
        Vector3 p1,
        float radius,
        FaceKind kind,
        string label,
        int segments = 10)
    {
        Vector3 axis = p1 - p0;
        float length = axis.Length;
        if (length < 0.01f || radius <= 0f)
            return;

        Vector3 direction = axis / length;
        Vector3 reference = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) < 0.9f
            ? Vector3.UnitY
            : Vector3.UnitX;
        Vector3 u = Vector3.Cross(direction, reference).Normalized();
        Vector3 v = Vector3.Cross(direction, u).Normalized();
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);

        segments = Math.Clamp(segments, 6, 32);
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.Tau * i / segments;
            float a1 = MathF.Tau * (i + 1) / segments;
            Vector3 r0 = (u * MathF.Cos(a0) + v * MathF.Sin(a0)) * radius;
            Vector3 r1 = (u * MathF.Cos(a1) + v * MathF.Sin(a1)) * radius;
            Vector3 a = p0 + r0;
            Vector3 b = p0 + r1;
            Vector3 c = p1 + r1;
            Vector3 d = p1 + r0;

            instance.Mesh.AddQuad(ToWorld(a), ToWorld(b), ToWorld(c), ToWorld(d), kind, instance.Id, label);
            instance.Mesh.AddTriangle(ToWorld(p0), ToWorld(b), ToWorld(a), kind, instance.Id, label);
            instance.Mesh.AddTriangle(ToWorld(p1), ToWorld(d), ToWorld(c), kind, instance.Id, label);
        }
    }
}
