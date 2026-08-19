using System.Globalization;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Engenharia paramétrica dos gaveteiros de cozinha. A caixaria continua sendo
/// construída pelas regras comuns; este builder acrescenta frentes, caixas de
/// gaveta e corrediças como conjuntos e peças individualmente selecionáveis.
/// </summary>
public static class DrawerModuleBuilder
{
    private readonly record struct FrontRect(
        float X1, float Y1, float X2, float Y2, bool IsDrawer, int DrawerIndex, string Label);

    private readonly record struct DrawerBox(
        int Index,
        float X0, float X1,
        float SideY0, float SideY1,
        float CounterY0, float CounterY1,
        float RearY0, float RearY1,
        float Z0, float Z1,
        float CounterZ0, float CounterZ1,
        float RearZ0, float RearZ1,
        float BottomY0, float BottomY1,
        float BottomX0, float BottomX1,
        float BottomZ0, float BottomZ1,
        float SideThickness, float CounterThickness, float RearThickness, float BottomThickness,
        float SlideGap,
        bool HasInternalFront, float InternalFrontAdvance, float InternalFrontThickness);

    public static bool TryBuild(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        if (!IsDrawerCabinet(definition) || definition.ShapeKind != ModuleShapeKind.Standard)
            return false;

        var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();
        ModuleMeshBuilder.BuildCarcass(instance, definition, settings, includeFronts: false);

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)
            ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount);
        var fronts = BuildFrontLayout(instance, definition, rules.Structure, settings);
        AddFronts(instance, definition, rules.Structure, fronts);

        IReadOnlyList<DrawerBox> boxes = ResolveDrawerBoxes(
            instance, definition, rules.Structure, settings, fronts);
        foreach (var box in boxes)
            AddDrawer(instance, box);

        return true;
    }

    /// <summary>
    /// Acrescenta uma coluna de gavetas dentro de um vão já construído por outro
    /// módulo (por exemplo, os balcões de pia 2P+4G/8G). As frentes usam os
    /// limites externos do vão e as caixas usam as faces internas das chapas que
    /// o delimitam, mantendo todas as folgas e ferragens do configurador.
    /// </summary>
    internal static void AddExternalDrawerColumn(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings settings,
        ModulationStructure structure,
        float frontX0,
        float frontX1,
        float clearInnerX0,
        float clearInnerX1,
        int drawerCount,
        int drawerIndexOffset = 0,
        float? frontY0 = null,
        float? frontY1 = null)
    {
        if (drawerCount <= 0 || frontX1 <= frontX0 || clearInnerX1 <= clearInnerX0)
            return;

        float gap = Math.Max(0f, structure.FrontGapMm);
        float bottom = frontY0 ?? Math.Max(0f, structure.FrontBottomGapMm);
        float topEdge = frontY1 ??
            (instance.Height - Math.Max(0f, structure.FrontTopGapMm));
        float usableHeight = Math.Max(1f, topEdge - bottom);
        float drawerHeight = Math.Max(1f,
            (usableHeight - gap * Math.Max(0, drawerCount - 1)) / drawerCount);
        var fronts = new List<FrontRect>(drawerCount);

        for (int i = 0; i < drawerCount; i++)
        {
            int index = drawerIndexOffset + i + 1;
            float y0 = bottom + i * (drawerHeight + gap);
            fronts.Add(new FrontRect(frontX0, y0, frontX1, y0 + drawerHeight,
                true, index, DrawerPartNaming.Part(index, "Frente")));
        }

        AddFronts(instance, definition, structure, fronts);
        foreach (var box in ResolveDrawerBoxes(
                     instance, definition, structure, settings, fronts,
                     clearInnerX0, clearInnerX1))
            AddDrawer(instance, box);
    }

    public static bool TryDecompose(
        ModuleInstance module,
        ModuleDefinition definition,
        float panelThicknessMm,
        float backThicknessMm,
        DimensionConfiguratorSettings? dimensionSettings,
        out IReadOnlyList<PartPiece> pieces)
    {
        pieces = Array.Empty<PartPiece>();
        if (!IsDrawerCabinet(definition) || definition.ShapeKind != ModuleShapeKind.Standard)
            return false;

        var settings = dimensionSettings ?? DimensionConfiguratorSettings.CreateDefault();
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, settings)
            ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount);
        var result = ModulationDecompositionService.Decompose(
            module, definition, rules, panelThicknessMm, backThicknessMm).ToList();
        var fronts = BuildFrontLayout(module, definition, rules.Structure, settings);
        var boxes = ResolveDrawerBoxes(module, definition, rules.Structure, settings, fronts);
        string moduleName = ModuleInstanceNamingService.GetEffectiveDisplayName(module);
        string material = MaterialCatalog.TryGet(module.MaterialId, out var mat) && mat != null
            ? mat.DisplayName
            : MaterialCatalog.GetDefault().DisplayName;

        // O preset legado não representa módulos mistos nem gavetas internas.
        // As frentes são substituídas pelo mesmo layout efetivamente desenhado.
        result.RemoveAll(piece =>
            piece.Name.StartsWith("Frente gaveta", StringComparison.OrdinalIgnoreCase) ||
            piece.Name.StartsWith("Frente porta", StringComparison.OrdinalIgnoreCase));
        float frontThickness = rules.Structure.FrontThicknessMm > 0f
            ? rules.Structure.FrontThicknessMm
            : definition.FrontThickness;
        foreach (var front in fronts)
        {
            result.Add(MakePiece(module, moduleName, material, front.Label,
                front.X2 - front.X1, front.Y2 - front.Y1, frontThickness));
        }

        foreach (var box in boxes)
        {
            float sideDepth = Math.Max(0f, box.Z1 - box.Z0);
            float sideHeight = Math.Max(0f, box.SideY1 - box.SideY0);
            float counterWidth = Math.Max(0f, box.X1 - box.X0 - 2f * box.SideThickness);
            float counterHeight = Math.Max(0f, box.CounterY1 - box.CounterY0);
            float rearHeight = Math.Max(0f, box.RearY1 - box.RearY0);
            float bottomWidth = Math.Max(0f, box.BottomX1 - box.BottomX0);
            float bottomDepth = Math.Max(0f, box.BottomZ1 - box.BottomZ0);

            result.Add(MakePiece(module, moduleName, material,
                DrawerPartNaming.Part(box.Index, "Lateral"), sideDepth, sideHeight, box.SideThickness, 2));
            result.Add(MakePiece(module, moduleName, material,
                DrawerPartNaming.Part(box.Index, "Contra-frente"), counterWidth, counterHeight, box.CounterThickness));
            result.Add(MakePiece(module, moduleName, material,
                DrawerPartNaming.Part(box.Index, "Posterior"), counterWidth, rearHeight, box.RearThickness));
            result.Add(MakePiece(module, moduleName, material,
                DrawerPartNaming.Part(box.Index, "Fundo"), bottomDepth, bottomWidth, box.BottomThickness));
            if (box.HasInternalFront)
                result.Add(MakePiece(module, moduleName, material,
                    DrawerPartNaming.Part(box.Index, "Frente interna"),
                    Math.Max(0f, box.X1 - box.X0 + 2f * box.InternalFrontAdvance),
                    Math.Max(0f, box.SideY1 - box.SideY0), box.InternalFrontThickness));
        }

        pieces = result;
        return true;
    }

    private static bool IsDrawerCabinet(ModuleDefinition definition) =>
        definition.DrawerCount > 0 &&
        string.Equals(definition.LibrarySubGroup, ModuleLibraryHierarchy.SubGaveteiros,
            StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<FrontRect> BuildFrontLayout(
        ModuleInstance module,
        ModuleDefinition definition,
        ModulationStructure structure,
        DimensionConfiguratorSettings settings)
    {
        float w = module.Width;
        float h = module.Height;
        float gap = Math.Max(0f, structure.FrontGapMm);
        float side = Math.Max(0f, structure.FrontSideGapMm);
        float bottom = Math.Max(0f, structure.FrontBottomGapMm);
        float top = Math.Max(0f, structure.FrontTopGapMm);
        float usableW = Math.Max(1f, w - 2f * side);
        float usableH = Math.Max(1f, h - bottom - top);
        bool internalDrawers = definition.Id.Contains("-int-", StringComparison.OrdinalIgnoreCase);
        var result = new List<FrontRect>();

        if (internalDrawers)
        {
            AddDoors(result, definition.DoorCount, side, bottom, usableW, usableH, gap);
            return result;
        }

        if (definition.DoorCount == 0)
        {
            float drawerHeight = Math.Max(1f,
                (usableH - gap * Math.Max(0, definition.DrawerCount - 1)) / definition.DrawerCount);
            for (int i = 0; i < definition.DrawerCount; i++)
            {
                float y0 = bottom + i * (drawerHeight + gap);
                result.Add(new FrontRect(side, y0, side + usableW, y0 + drawerHeight,
                    true, i + 1, DrawerPartNaming.Part(i + 1, "Frente")));
            }
            return result;
        }

        // Módulos mistos: gavetas na faixa superior e portas no vão inferior.
        float drawerBand = Math.Clamp(definition.DrawerCount * 155f,
            usableH * 0.22f, usableH * 0.55f);
        float doorHeight = Math.Max(1f, usableH - drawerBand - gap);
        AddDoors(result, definition.DoorCount, side, bottom, usableW, doorHeight, gap);
        float eachDrawer = Math.Max(1f,
            (drawerBand - gap * Math.Max(0, definition.DrawerCount - 1)) / definition.DrawerCount);
        for (int i = 0; i < definition.DrawerCount; i++)
        {
            float y0 = bottom + doorHeight + gap + i * (eachDrawer + gap);
            result.Add(new FrontRect(side, y0, side + usableW, y0 + eachDrawer,
                true, i + 1, DrawerPartNaming.Part(i + 1, "Frente")));
        }
        return result;
    }

    private static void AddDoors(
        List<FrontRect> result,
        int doorCount,
        float x0,
        float y0,
        float width,
        float height,
        float gap)
    {
        int count = Math.Max(1, doorCount);
        float each = Math.Max(1f, (width - gap * Math.Max(0, count - 1)) / count);
        for (int i = 0; i < count; i++)
        {
            float x = x0 + i * (each + gap);
            result.Add(new FrontRect(x, y0, x + each, y0 + height,
                false, 0, $"Porta {i + 1}"));
        }
    }

    private static void AddFronts(
        ModuleInstance instance,
        ModuleDefinition definition,
        ModulationStructure structure,
        IReadOnlyList<FrontRect> fronts)
    {
        float thickness = structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm
            : definition.FrontThickness;
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);

        foreach (var front in fronts)
        {
            ModuleMeshBuilder.AddPanelBox(instance.Mesh, instance.Id, ToWorld,
                new Vector3(front.X1, front.Y1, instance.Depth),
                new Vector3(front.X2, front.Y2, instance.Depth + thickness),
                FaceKind.ModuleRight, FaceKind.ModuleFront, front.Label, instance.PartOverrides);
        }
    }

    private static IReadOnlyList<DrawerBox> ResolveDrawerBoxes(
        ModuleInstance module,
        ModuleDefinition definition,
        ModulationStructure structure,
        DimensionConfiguratorSettings settings,
        IReadOnlyList<FrontRect> fronts,
        float? clearInnerX0 = null,
        float? clearInnerX1 = null)
    {
        bool internalDrawers = definition.Id.Contains("-int-", StringComparison.OrdinalIgnoreCase);
        CozinhaGavetasSettings source = internalDrawers
            ? new CozinhaGavetasSettings
            {
                Numeric = new Dictionary<string, float>(
                    settings.CozinhaGavetasInternas.Numeric, StringComparer.Ordinal),
                Choice = new Dictionary<string, string>(
                    settings.CozinhaGavetasInternas.Choice, StringComparer.Ordinal)
            }
            : settings.CozinhaGavetas;

        float cabinetT = Math.Max(1f, structure.PanelThicknessMm);
        float backT = Math.Max(1f, structure.BackThicknessMm);
        float sideT = PieceThickness(settings, ChapaPieceKinds.GavLateral, 15f);
        float counterT = PieceThickness(settings, ChapaPieceKinds.GavContraFrente, sideT);
        float rearT = PieceThickness(settings, ChapaPieceKinds.GavPosterior, sideT);
        float bottomT = PieceThickness(settings, ChapaPieceKinds.GavFundo, 6f);
        float internalFrontT = PieceThickness(settings, ChapaPieceKinds.FrenteGavInterna,
            structure.FrontThicknessMm > 0f ? structure.FrontThicknessMm : 18f);
        string slideGapField = module.DrawerSlideType == DrawerSlideType.Concealed
            ? "folg-cor-inv"
            : "folg-cor-tel";
        float slideGap = Value(source, "folgas", slideGapField,
            Value(source, "folgas", "folg-cor", 4f));
        float backGap = Value(source, "folgas", "folg-fundo", 0f);
        float lateralOverCounter = Value(source, "fix-contra-frente", "av-lat-cf", 0f);
        float counterOverLateral = Value(source, "fix-contra-frente", "av-cf-lat", 0f);
        float lateralOverRear = Value(source, "fix-posterior", "av-lat-pos", 0f);
        float rearOverLateral = Value(source, "fix-posterior", "av-pos-lat", 0f);
        float bottomOverSide = Value(source, "fundos", "av-fun-lat", 0f);
        float bottomOverCounter = Value(source, "fundos", "av-fun-cf", 0f);
        float bottomOverRear = Value(source, "fundos", "av-fun-pos", 0f);
        float bottomRecess = Value(source, "fundos", "recuo-fundo", 0f);
        float internalFrontAdvance = internalDrawers
            ? Value(source, "folgas", "av-lat-frente", 0f)
            : 0f;

        float clearX0 = clearInnerX0 ?? cabinetT;
        float clearX1 = clearInnerX1 ?? module.Width - cabinetT;
        clearX0 = Math.Clamp(clearX0, 0f, module.Width - 2f);
        clearX1 = Math.Clamp(clearX1, clearX0 + 2f, module.Width);
        float x0 = Math.Clamp(clearX0 + slideGap, 0f, clearX1 - 1f);
        float x1 = Math.Clamp(clearX1 - slideGap, x0 + 1f, module.Width);
        float z0 = Math.Clamp(backT + backGap, 0f, module.Depth - 80f);
        // A caixa da gaveta nasce no alinhamento frontal da caixaria. Não existe
        // recuo fixo neste lado: a frente aplicada é que avança para fora de D.
        float z1 = Math.Max(z0 + 40f, module.Depth);
        var drawerFronts = fronts.Where(front => front.IsDrawer).ToList();

        if (internalDrawers)
        {
            float upper = Value(source, "folgas", "gint-sup", 0f);
            float lower = Value(source, "folgas", "gint-inf", 0f);
            float between = Value(source, "folgas", "gint-entre", structure.FrontGapMm);
            float minY = cabinetT + lower;
            float maxY = module.Height - cabinetT - upper;
            float each = Math.Max(30f,
                (maxY - minY - between * Math.Max(0, definition.DrawerCount - 1)) / definition.DrawerCount);
            drawerFronts.Clear();
            for (int i = 0; i < definition.DrawerCount; i++)
            {
                float y = minY + i * (each + between);
                drawerFronts.Add(new FrontRect(x0, y, x1, y + each,
                    true, i + 1, DrawerPartNaming.Part(i + 1, "Frente interna")));
            }
        }

        var boxes = new List<DrawerBox>(drawerFronts.Count);
        foreach (var front in drawerFronts)
        {
            bool gavetao = !internalDrawers && IsGavetao(definition, front.DrawerIndex);
            string prefix = gavetao ? "fgav-" : "folg-";
            float topCounter = Value(source, "folgas", $"{prefix}sup-cf", 0f);
            float topSide = Value(source, "folgas", $"{prefix}sup-lat", 0f);
            float topRear = Value(source, "folgas", $"{prefix}sup-pos", 0f);
            float lowerCounter = Value(source, "folgas", $"{prefix}inf-cf", 0f);
            float lowerSide = Value(source, "folgas", $"{prefix}inf-lat", 0f);
            float lowerRear = Value(source, "folgas", $"{prefix}inf-pos", 0f);
            float sy0 = Math.Clamp(front.Y1 + lowerSide, 0f, module.Height - 2f);
            float sy1 = Math.Clamp(front.Y2 - topSide, sy0 + 2f, module.Height);
            float cy0 = Math.Clamp(front.Y1 + lowerCounter, 0f, module.Height - 2f);
            float cy1 = Math.Clamp(front.Y2 - topCounter, cy0 + 2f, module.Height);
            float ry0 = Math.Clamp(front.Y1 + lowerRear, 0f, module.Height - 2f);
            float ry1 = Math.Clamp(front.Y2 - topRear, ry0 + 2f, module.Height);
            float counterZ1 = Math.Clamp(z1 - lateralOverCounter + counterOverLateral, z0 + 20f, module.Depth);
            float counterZ0 = counterZ1 - counterT;
            float rearZ0 = Math.Clamp(z0 + lateralOverRear - rearOverLateral, backT, z1 - 20f);
            float rearZ1 = rearZ0 + rearT;
            float floorY0 = Math.Max(Math.Min(sy0, Math.Min(cy0, ry0)) + bottomRecess, cabinetT);
            float floorY1 = floorY0 + bottomT;
            float floorX0 = Math.Clamp(x0 + sideT - bottomOverSide, cabinetT, x1 - sideT - 2f);
            float floorX1 = Math.Clamp(x1 - sideT + bottomOverSide, floorX0 + 2f, module.Width - cabinetT);
            float floorZ0 = Math.Clamp(rearZ1 - bottomOverRear, backT, counterZ0 - 2f);
            float floorZ1 = Math.Clamp(counterZ0 + bottomOverCounter, floorZ0 + 2f, module.Depth - 2f);

            boxes.Add(new DrawerBox(front.DrawerIndex, x0, x1, sy0, sy1, cy0, cy1, ry0, ry1,
                z0, z1, counterZ0, counterZ1, rearZ0, rearZ1,
                floorY0, floorY1, floorX0, floorX1, floorZ0, floorZ1,
                sideT, counterT, rearT, bottomT, slideGap,
                internalDrawers, internalFrontAdvance, internalFrontT));
        }
        return boxes;
    }

    private static bool IsGavetao(ModuleDefinition definition, int drawerIndex)
    {
        string id = definition.Id;
        int gavetaoCount = id.Contains("1g-2gav", StringComparison.OrdinalIgnoreCase) ? 2
            : id.Contains("2g-1gav", StringComparison.OrdinalIgnoreCase) ? 1
            : id.Contains("2gav", StringComparison.OrdinalIgnoreCase) ? 2
            : 0;

        // O layout é construído de baixo para cima; gavetões ocupam primeiro as
        // frentes inferiores, exatamente como a nomenclatura do catálogo.
        return drawerIndex > 0 && drawerIndex <= gavetaoCount;
    }

    private static void AddDrawer(ModuleInstance instance, DrawerBox box)
    {
        Vector3 ToWorld(Vector3 local) => ModulePlacementService.TransformLocalPoint(
            local, instance.Position, instance.RotationYDegrees);
        void Panel(Vector3 min, Vector3 max, FaceKind kind, string part) =>
            ModuleMeshBuilder.AddPanelBox(instance.Mesh, instance.Id, ToWorld, min, max,
                kind, kind, DrawerPartNaming.Part(box.Index, part), instance.PartOverrides);

        Panel(new Vector3(box.X0, box.SideY0, box.Z0),
            new Vector3(box.X0 + box.SideThickness, box.SideY1, box.Z1),
            FaceKind.ModuleLeft, "Lateral esq.");
        Panel(new Vector3(box.X1 - box.SideThickness, box.SideY0, box.Z0),
            new Vector3(box.X1, box.SideY1, box.Z1),
            FaceKind.ModuleRight, "Lateral dir.");
        Panel(new Vector3(box.X0 + box.SideThickness, box.CounterY0, box.CounterZ0),
            new Vector3(box.X1 - box.SideThickness, box.CounterY1, box.CounterZ1),
            FaceKind.ModuleFront, "Contra-frente");
        Panel(new Vector3(box.X0 + box.SideThickness, box.RearY0, box.RearZ0),
            new Vector3(box.X1 - box.SideThickness, box.RearY1, box.RearZ1),
            FaceKind.ModuleBack, "Posterior");
        Panel(new Vector3(box.BottomX0, box.BottomY0, box.BottomZ0),
            new Vector3(box.BottomX1, box.BottomY1, box.BottomZ1),
            FaceKind.ModuleBottom, "Fundo");

        if (box.HasInternalFront)
        {
            float frontX0 = box.X0 - box.InternalFrontAdvance;
            float frontX1 = box.X1 + box.InternalFrontAdvance;
            Panel(new Vector3(frontX0, box.SideY0, box.CounterZ1),
                new Vector3(frontX1, box.SideY1, box.CounterZ1 + box.InternalFrontThickness),
                FaceKind.ModuleFront, "Frente interna");
        }

        if (instance.DrawerSlideType == DrawerSlideType.Concealed)
        {
            // Corrediça invisível fica sob o fundo da gaveta, recuada das laterais.
            float railWidth = Math.Clamp(box.SideThickness * 0.75f, 8f, 14f);
            float railY1 = box.BottomY0;
            float railY0 = Math.Max(0f, railY1 - 8f);
            Panel(new Vector3(box.X0 + box.SideThickness, railY0, box.Z0 + 12f),
                new Vector3(box.X0 + box.SideThickness + railWidth, railY1, box.Z1 - 8f),
                FaceKind.ModuleBottom, "Corrediça esq.");
            Panel(new Vector3(box.X1 - box.SideThickness - railWidth, railY0, box.Z0 + 12f),
                new Vector3(box.X1 - box.SideThickness, railY1, box.Z1 - 8f),
                FaceKind.ModuleBottom, "Corrediça dir.");
        }
        else
        {
            // Corrediça telescópica ocupa a folga lateral.
            float railWidth = Math.Clamp(box.SlideGap, 2f, 8f);
            float railHeight = Math.Clamp((box.SideY1 - box.SideY0) * 0.16f, 12f, 32f);
            float railY0 = box.SideY0 + Math.Max(2f, (box.SideY1 - box.SideY0) * 0.18f);
            Panel(new Vector3(box.X0 - railWidth, railY0, box.Z0 + 4f),
                new Vector3(box.X0, railY0 + railHeight, box.Z1 - 4f),
                FaceKind.ModuleLeft, "Corrediça esq.");
            Panel(new Vector3(box.X1, railY0, box.Z0 + 4f),
                new Vector3(box.X1 + railWidth, railY0 + railHeight, box.Z1 - 4f),
                FaceKind.ModuleRight, "Corrediça dir.");
        }
    }

    private static float PieceThickness(
        DimensionConfiguratorSettings settings,
        string kind,
        float fallback)
    {
        ChapaConfiguratorService.EnsureChapasInitialized(settings);
        float value = settings.CozinhaChapas.GetOrCreate(kind).ThicknessMm;
        return float.IsFinite(value) && value > 0f ? value : fallback;
    }

    private static float Value(CozinhaGavetasSettings source, string node, string field, float fallback)
    {
        string key = GavetasConfiguratorService.MakeKey(node, field);
        if (source.Numeric.TryGetValue(key, out float numeric) && float.IsFinite(numeric))
            return numeric;

        return source.Choice.TryGetValue(key, out string? raw) &&
               float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
               float.IsFinite(value)
            ? value
            : fallback;
    }

    private static PartPiece MakePiece(
        ModuleInstance module,
        string moduleName,
        string material,
        string name,
        float length,
        float width,
        float thickness,
        int quantity = 1) => new()
        {
            ModuleId = module.Id,
            ModuleName = moduleName,
            Name = name,
            LengthMm = Math.Max(0f, length),
            WidthMm = Math.Max(0f, width),
            ThicknessMm = Math.Max(0f, thickness),
            Quantity = Math.Max(1, quantity),
            MaterialName = material
        };
}
