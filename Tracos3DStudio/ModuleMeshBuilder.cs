using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ModuleMeshBuilder
{
    public static void Build(ModuleInstance instance, ModuleDefinition definition)
    {
        Build(instance, definition, dimensionSettings: null);
    }

    public static void Build(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        if (definition.IsDecorativePanel)
            BuildFlatPanel(instance);
        else if (!ModuleMeshShapes.TryBuild(instance, definition, dimensionSettings))
            BuildBoxWithFront(instance, definition, dimensionSettings);
    }

    public static void BuildFlatPanel(ModuleInstance instance)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;

        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        AddBoxLocal(instance.Mesh, id, ToWorld,
            Vector3.Zero,
            new Vector3(w, h, d),
            FaceKind.ModuleBack);

        instance.Mesh.AddQuad(
            ToWorld(new Vector3(0f, 0f, d)),
            ToWorld(new Vector3(w, 0f, d)),
            ToWorld(new Vector3(w, h, d)),
            ToWorld(new Vector3(0f, h, d)),
            FaceKind.ModuleFront,
            id,
            "Painel");
    }

    public static void BuildBoxWithFront(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        BuildCarcass(instance, definition, dimensionSettings, includeFronts: true);
    }

    /// <summary>
    /// Caixaria do módulo reto (laterais, fundo com avanços, base, sarrafos, prateleiras)
    /// conforme o configurador de dimensões — mesma engenharia para balcão e Canto Reto.
    /// </summary>
    /// <param name="includeFronts">Se true, adiciona portas/gavetas padrão.</param>
    /// <param name="structureMutator">Ajuste opcional na estrutura efetiva (ex.: recuo CR).</param>
    public static void BuildCarcass(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings,
        bool includeFronts = false,
        Action<ModulationStructure>? structureMutator = null,
        Vector3 localOffset = default)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;

        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(
                local + localOffset, instance.Position, instance.RotationYDegrees);

        var effectiveRules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = effectiveRules?.Structure
            ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, definition.DrawerCount).Structure;

        structureMutator?.Invoke(structure);

        // Espessuras (mm) — regras do template ou padrão do perfil.
        float t = structure.PanelThicknessMm > 0f ? structure.PanelThicknessMm : 18f;
        float bt = structure.BackThicknessMm > 0f ? structure.BackThicknessMm : 6f;
        float ft = structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm
            : definition.FrontThickness;

        // Clamps de segurança para módulos pequenos.
        t = MathF.Min(t, MathF.Max(1f, (w - 2f) * 0.45f));
        bt = MathF.Min(bt, MathF.Max(1f, d - 2f));
        ft = Math.Clamp(ft, 1f, 50f);

        var mesh = instance.Mesh;
        var overrides = instance.PartOverrides;

        // Carcaça com peças individualizadas (espessura real), estilo Promob.
        // Laterais (altura e profundidade totais).
        float lateralBaseY = MathF.Abs(structure.LateralBaseOverlapMm) >= MathF.Abs(structure.LateralBottomRecessMm)
            ? structure.LateralBaseOverlapMm
            : structure.LateralBottomRecessMm;
        float lateralGap = Math.Clamp(structure.LateralDepthGapMm, -d * 0.5f, MathF.Max(0f, d - 10f));
        float lateralZ0 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Back => lateralGap,
            LateralDepthAlignment.Center => lateralGap * 0.5f,
            _ => 0f
        };
        float lateralZ1 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Front => d - lateralGap,
            LateralDepthAlignment.Center => d - lateralGap * 0.5f,
            _ => d
        };
        AddPanelBox(mesh, id, ToWorld,
            new Vector3(0f, lateralBaseY, lateralZ0), new Vector3(t, h, lateralZ1),
            FaceKind.ModuleLeft, FaceKind.ModuleLeft, "Lateral esq.", overrides);
        AddPanelBox(mesh, id, ToWorld,
            new Vector3(w - t, lateralBaseY, lateralZ0), new Vector3(w, h, lateralZ1),
            FaceKind.ModuleRight, FaceKind.ModuleRight, "Lateral dir.", overrides);

        // Fundo e sarrafo conforme montagem (V3.7f Fase 3c).
        BuildBackAssembly(mesh, id, ToWorld, w, h, d, t, bt, structure, overrides);

        // Base inferior acompanha a profundidade das laterais. O fundo encaixado
        // não pode encurtar a base; somente o recuo frontal configurado o faz.
        float backPlaneZ = GetBackPlaneZ(structure, bt);
        float recBase = Math.Clamp(structure.BaseRecessMm, -d * 0.5f, MathF.Max(0f, d - backPlaneZ - 10f));
        float baseZ0 = structure.BaseFullDepth ? 0f : lateralZ0;
        float baseZ1 = structure.BaseFullDepth ? d : MathF.Max(baseZ0 + 10f, lateralZ1 - recBase);
        float baseOverLateral = Math.Clamp(structure.BaseAdvanceOverLateralMm, -t, t);
        float baseX0 = t - baseOverLateral;
        float baseX1 = w - t + baseOverLateral;
        AddPanelBox(mesh, id, ToWorld,
            new Vector3(baseX0, lateralBaseY, baseZ0), new Vector3(baseX1, t + lateralBaseY, baseZ1),
            FaceKind.ModuleBottom, FaceKind.ModuleBottom, "Base inferior", overrides);

        // Topo: aéreo fecha com tampo; balcão usa travessas dianteira e traseira (Promob).
        if (definition.IsWallMounted)
        {
            AddPanelBox(mesh, id, ToWorld,
                new Vector3(t, h - t, backPlaneZ), new Vector3(w - t, h, d),
                FaceKind.ModuleTop, FaceKind.ModuleTop, "Tampo", overrides);
        }
        else
        {
            float sarrafoH    = Math.Clamp(structure.SarrafoHeightMm,         10f, h * 0.5f);
            float sarrafoHtra = Math.Clamp(structure.SarrafoTraseiroHeightMm, 10f, h * 0.5f);
            float sarrafoT    = Math.Clamp(structure.SarrafoThicknessMm, 6f, 50f);
            float recuoFro    = Math.Clamp(structure.SarrafoDianteiroRecessMm, -d * 0.5f, d - 10f);
            float sarrafoAdvanceX = Math.Clamp(structure.SarrafoAdvanceOverLateralMm, -t, t);
            float sarrafoX0 = t - sarrafoAdvanceX;
            float sarrafoX1 = w - t + sarrafoAdvanceX;

            if (structure.SarrafoWhole && structure.SarrafoVisible)
            {
                float wholeZ0 = backPlaneZ - structure.SarrafoAdvanceOverBackMm
                    + structure.BackAdvanceOverSarrafoMm;
                float wholeZ1 = MathF.Max(wholeZ0 + 10f, d - recuoFro);
                AddSarrafoPanel(mesh, id, ToWorld,
                    new Vector3(sarrafoX0, h - sarrafoT, wholeZ0),
                    new Vector3(sarrafoX1, h, wholeZ1),
                    "Sarrafo inteiro", segmented: false, structure.SarrafoChamfered, overrides);
            }

            // ── Sarrafo TRASEIRO ─────────────────────────────────────────────────
            if (structure.BackSarrafoVisible)
            {
                bool recessedBack = structure.BackPanelLayout is BoxBackPanelLayout.Rebaixado
                    or BoxBackPanelLayout.TravessaHorizontal
                    or BoxBackPanelLayout.TravessaVertical
                    or BoxBackPanelLayout.SemFundo;
                float backRailZ0 = recessedBack
                    ? structure.BackSarrafoRecessMm
                    : backPlaneZ - structure.SarrafoAdvanceOverBackMm
                        + structure.BackAdvanceOverSarrafoMm;
                float backRailTop = h - structure.BackSarrafoLowerRecessMm;
                if (!structure.BackSarrafoIsVertical)
                {
                    AddSarrafoPanel(mesh, id, ToWorld,
                        new Vector3(sarrafoX0, backRailTop - sarrafoT, backRailZ0),
                        new Vector3(sarrafoX1, backRailTop, backRailZ0 + sarrafoHtra),
                        "Sarrafo traseiro", structure.BackSarrafoSegmented,
                        structure.SarrafoChamfered, overrides);
                }
                else
                {
                    AddSarrafoPanel(mesh, id, ToWorld,
                        new Vector3(sarrafoX0, backRailTop - sarrafoHtra, backRailZ0),
                        new Vector3(sarrafoX1, backRailTop, backRailZ0 + sarrafoT),
                        "Sarrafo traseiro", structure.BackSarrafoSegmented,
                        structure.SarrafoChamfered, overrides);
                }
            }

            // ── Sarrafo DIANTEIRO ────────────────────────────────────────────────
            if (structure.FrontSarrafoVisible)
            {
                float zFroEnd = d - recuoFro;
                float zFro0   = zFroEnd - sarrafoH;
                if (!structure.FrontSarrafoIsVertical)
                {
                    AddSarrafoPanel(mesh, id, ToWorld,
                        new Vector3(sarrafoX0, h - sarrafoT, zFro0),
                        new Vector3(sarrafoX1, h, zFroEnd),
                        "Sarrafo dianteiro", structure.FrontSarrafoSegmented,
                        structure.SarrafoChamfered, overrides);
                }
                else
                {
                    float zFroVertEnd = d - recuoFro;
                    float zFroVert0   = zFroVertEnd - sarrafoT;
                    AddSarrafoPanel(mesh, id, ToWorld,
                        new Vector3(sarrafoX0, h - sarrafoH, zFroVert0),
                        new Vector3(sarrafoX1, h, zFroVertEnd),
                        "Sarrafo dianteiro", structure.FrontSarrafoSegmented,
                        structure.SarrafoChamfered, overrides);
                }
            }
        }

        // Prateleira interna.
        AddShelves(mesh, id, ToWorld, w, h, d, t, bt, structure, overrides);
        AddDivisions(mesh, id, ToWorld, w, h, d, t, bt, structure, overrides);

        // Frentes (portas/gavetas) por fora da caixaria — face traseira na frente do módulo (paridade Promob).
        if (includeFronts)
            AddFronts(mesh, id, ToWorld, w, h, d, ft, structure, overrides);
    }

    internal static float GetBackPlaneZ(ModulationStructure structure, float backThickness) =>
        structure.BackPanelLayout is BoxBackPanelLayout.SemFundo
            or BoxBackPanelLayout.TravessaHorizontal
            or BoxBackPanelLayout.TravessaVertical
                ? 0f
                : structure.BackPanelType == BoxBackPanelType.Pregado
                    ? backThickness
                    : structure.BackRecessMm + backThickness;

    internal static void BuildBackAssembly(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        float moduleWidth,
        float moduleHeight,
        float moduleDepth,
        float panelThickness,
        float backThickness,
        ModulationStructure structure,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides)
    {
        float t = panelThickness;
        float bt = backThickness;

        if (structure.BackPanelLayout == BoxBackPanelLayout.SemFundo)
            return;

        float z0 = structure.BackPanelType == BoxBackPanelType.Pregado
            ? 0f
            : structure.BackRecessMm;

        // Fixação Fundo - Lateral / Base - Fundo (configurador Inferior).
        float afl = Math.Clamp(structure.BackAdvanceOverLateralMm, -t, t);
        float alf = Math.Clamp(structure.LateralAdvanceOverBackMm, -t, t);
        float afb = Math.Clamp(structure.BackAdvanceOverBaseMm, -t, t);

        float x0 = Math.Clamp(t - afl + alf, -t, moduleWidth * 0.45f);
        float x1 = Math.Clamp(moduleWidth - t + afl - alf, moduleWidth * 0.55f, moduleWidth + t);
        // afb=0 → fundo assenta sobre a base (y=t); afb>0 → avança sobre a base.
        float y0 = t - afb;

        float railWidth = structure.CrossRailWidthMm > 0f
            ? structure.CrossRailWidthMm
            : MathF.Max(40f, t * 3f);
        railWidth = Math.Clamp(railWidth, 10f, MathF.Max(10f, MathF.Min(moduleWidth, moduleHeight) * 0.45f));

        if (structure.BackPanelLayout == BoxBackPanelLayout.TravessaVertical)
        {
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x0, y0, z0), new Vector3(MathF.Min(x1, x0 + railWidth), moduleHeight, z0 + t),
                FaceKind.ModuleBack, FaceKind.ModuleBack, "Travessa traseira esq.", overrides);
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(MathF.Max(x0, x1 - railWidth), y0, z0), new Vector3(x1, moduleHeight, z0 + t),
                FaceKind.ModuleBack, FaceKind.ModuleBack, "Travessa traseira dir.", overrides);
            return;
        }

        if (structure.BackPanelLayout == BoxBackPanelLayout.TravessaHorizontal)
        {
            float lower = Math.Clamp(structure.BackLowerRailOffsetMm, -moduleHeight * 0.5f, moduleHeight - 10f);
            float upperTop = moduleHeight - Math.Clamp(
                structure.BackUpperRailOffsetMm, -moduleHeight * 0.5f, moduleHeight - 10f);
            float upperBottom = upperTop - railWidth;
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x0, lower, z0), new Vector3(x1, lower + railWidth, z0 + t),
                FaceKind.ModuleBack, FaceKind.ModuleBack, "Travessa traseira inferior", overrides);
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x0, upperBottom, z0), new Vector3(x1, upperTop, z0 + t),
                FaceKind.ModuleBack, FaceKind.ModuleBack, "Travessa traseira superior", overrides);
            return;
        }

        float backTop = structure.BackPanelLayout == BoxBackPanelLayout.Rebaixado
            ? moduleHeight - Math.Clamp(
                structure.BackHeightRecessMm,
                -moduleHeight * 0.5f,
                MathF.Max(0f, moduleHeight - y0 - 10f))
            : moduleHeight;

        AddPanelBox(mesh, ownerId, toWorld,
            new Vector3(x0, y0, z0), new Vector3(x1, backTop, z0 + bt),
            FaceKind.ModuleBack, FaceKind.ModuleBack, "Fundo", overrides);

        AddBackSupportRails(mesh, ownerId, toWorld, x0, x1, y0, backTop, z0, t, structure, overrides);
    }

    private static void AddBackSupportRails(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        float x0,
        float x1,
        float y0,
        float y1,
        float z0,
        float thickness,
        ModulationStructure structure,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides)
    {
        int count = Math.Clamp(structure.BackSupportRailCount, 0, 2);
        if (count == 0)
            return;

        float width = structure.BackSupportRailWidthMm > 0f
            ? structure.BackSupportRailWidthMm
            : MathF.Max(40f, thickness * 3f);
        width = Math.Clamp(width, 10f, MathF.Max(10f, (y1 - y0) * 0.4f));

        for (int i = 0; i < count; i++)
        {
            float fraction = count == 1 ? 0.5f : (i == 0 ? 0.33f : 0.67f);
            float center = y0 + (y1 - y0) * fraction;
            float railY0 = Math.Clamp(center - width * 0.5f, y0, MathF.Max(y0, y1 - width));
            // A travessa sustenta a face traseira do fundo. Sua face dianteira
            // termina em z0; não pode atravessar o fundo e aparecer dentro da caixa.
            float railZ1 = z0;
            float railZ0 = railZ1 - thickness;
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x0, railY0, railZ0), new Vector3(x1, railY0 + width, railZ1),
                FaceKind.ModuleBack, FaceKind.ModuleBack,
                count == 1 ? "Travessa de sustentação" : $"Travessa de sustentação {i + 1}",
                overrides);
        }
    }

    private static void AddSarrafoPanel(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        Vector3 min,
        Vector3 max,
        string label,
        bool segmented,
        bool chamfered,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides)
    {
        string effectiveLabel = chamfered ? $"{label} chanfrado" : label;
        if (!segmented || max.X - min.X < 40f)
        {
            AddSarrafoPiece(mesh, ownerId, toWorld, min, max, effectiveLabel, chamfered, overrides);
            return;
        }

        float gap = Math.Clamp((max.X - min.X) * 0.02f, 4f, 18f);
        float mid = (min.X + max.X) * 0.5f;
        AddSarrafoPiece(mesh, ownerId, toWorld, min,
            new Vector3(mid - gap * 0.5f, max.Y, max.Z), $"{effectiveLabel} 1", chamfered, overrides);
        AddSarrafoPiece(mesh, ownerId, toWorld,
            new Vector3(mid + gap * 0.5f, min.Y, min.Z), max,
            $"{effectiveLabel} 2", chamfered, overrides);
    }

    private static void AddSarrafoPiece(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        Vector3 min,
        Vector3 max,
        string label,
        bool chamfered,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides)
    {
        if (!chamfered)
        {
            AddPanelBox(mesh, ownerId, toWorld, min, max,
                FaceKind.ModuleTop, FaceKind.ModuleTop, label, overrides);
            return;
        }

        float cut = Math.Clamp(MathF.Min(max.X - min.X, max.Z - min.Z) * 0.2f, 3f, 30f);
        Vector2[] plan =
        [
            new(min.X, min.Z), new(max.X, min.Z),
            new(max.X, max.Z - cut), new(max.X - cut, max.Z),
            new(min.X + cut, max.Z), new(min.X, max.Z - cut)
        ];
        Vector3 P(int i, float y) => toWorld(new Vector3(plan[i].X, y, plan[i].Y));

        for (int i = 1; i < plan.Length - 1; i++)
        {
            mesh.AddTriangle(P(0, min.Y), P(i + 1, min.Y), P(i, min.Y),
                FaceKind.ModuleTop, ownerId, label);
            mesh.AddTriangle(P(0, max.Y), P(i, max.Y), P(i + 1, max.Y),
                FaceKind.ModuleTop, ownerId, label);
        }

        for (int i = 0; i < plan.Length; i++)
        {
            int next = (i + 1) % plan.Length;
            mesh.AddQuad(P(i, min.Y), P(next, min.Y), P(next, max.Y), P(i, max.Y),
                FaceKind.ModuleTop, ownerId, label);
        }
    }

    private static void AddDivisions(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        float moduleWidth,
        float moduleHeight,
        float moduleDepth,
        float panelThickness,
        float backThickness,
        ModulationStructure structure,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides)
    {
        if (structure.Divisions.Count == 0)
            return;

        float backPlane = GetBackPlaneZ(structure, backThickness);
        float innerWidth = MathF.Max(1f, moduleWidth - 2f * panelThickness);
        int index = 0;
        foreach (var division in structure.Divisions)
        {
            index++;
            float xCenter = panelThickness + innerWidth * Math.Clamp(division.WidthFraction, 0.05f, 0.95f);
            float x0 = Math.Clamp(xCenter - panelThickness * 0.5f, panelThickness, moduleWidth - 2f * panelThickness);
            float x1 = MathF.Min(moduleWidth - panelThickness, x0 + panelThickness);
            float rearInset = division.IsFixed
                ? structure.DivisionFixedBackInsetMm
                : structure.DivisionMovableBackInsetMm;
            float z0 = structure.DivisionsInsideBackPanel
                ? backPlane + rearInset
                : division.IsFixed
                    ? rearInset
                    : backPlane + rearInset;
            z0 -= structure.BackAdvanceOverDivisionMm;
            if (structure.DivisionsInsideBackPanel)
                z0 = MathF.Max(backPlane, z0);
            float z1 = moduleDepth - structure.DivisionFrontInsetMm;
            float y0 = panelThickness + structure.DivisionBottomRecessMm;
            float y1 = moduleHeight - panelThickness;
            if (z1 <= z0 + 1f || y1 <= y0 + 1f)
                continue;

            string label = structure.Divisions.Count > 1 ? $"Divisória {index}" : "Divisória";
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x0, y0, z0), new Vector3(x1, y1, z1),
                FaceKind.ModuleRight, FaceKind.ModuleRight, label, overrides);

            float spacer = Math.Clamp(structure.DivisionSpacerWidthMm, 0f, z1 - z0);
            if (spacer > 0.5f)
            {
                AddPanelBox(mesh, ownerId, toWorld,
                    new Vector3(x0, y0, z1 - spacer), new Vector3(x1, y1, z1),
                    FaceKind.ModuleRight, FaceKind.ModuleRight, $"Distanciador divisória {index}", overrides);
            }
        }
    }

    /// <summary>
    private static void AddShelves(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        float moduleWidth,
        float moduleHeight,
        float moduleDepth,
        float panelThickness,
        float backThickness,
        ModulationStructure structure,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides = null)
    {
        if (structure.Shelves.Count == 0)
            return;

        // Face interna do fundo (= recuo + espessura do fundo). A prateleira começa aí,
        // não na traseira do módulo — evita invadir o vão do fundo rebaixado/encaixado.
        float shelfZ0 = GetBackPlaneZ(structure, backThickness);
        // Espessura da prateleira: chapa de painel (mesma da lateral/base no 3D atual).
        float shelfT = Math.Clamp(panelThickness, 1f, 50f);

        int index = 0;

        foreach (var shelf in structure.Shelves)
        {
            index++;
            float frac = Math.Clamp(shelf.HeightFraction, 0f, 1f);
            float bottom = panelThickness;
            float top = moduleHeight - panelThickness;
            float yShelf = Math.Clamp(bottom + (top - bottom) * frac, bottom, top - shelfT);
            float wi = Math.Clamp(shelf.WidthInsetMm, -panelThickness, moduleWidth * 0.45f);
            float di = Math.Clamp(shelf.DepthInsetMm, -moduleDepth * 0.5f, moduleDepth - 1f);

            float x1 = panelThickness + wi;
            float x2 = moduleWidth - panelThickness - wi;
            float z1 = shelfZ0 + shelf.BackInsetMm;
            float z2 = MathF.Max(z1 + 1f, moduleDepth - di);

            if (x2 <= x1)
                continue;

            var divisionRanges = structure.Divisions
                .Select(division =>
                {
                    float innerWidth = MathF.Max(1f, moduleWidth - 2f * panelThickness);
                    float center = panelThickness + innerWidth *
                        Math.Clamp(division.WidthFraction, 0.05f, 0.95f);
                    float min = Math.Clamp(center - panelThickness * 0.5f,
                        panelThickness, moduleWidth - 2f * panelThickness);
                    float max = MathF.Min(moduleWidth - panelThickness, min + panelThickness);
                    return (Min: min, Max: max);
                })
                .OrderBy(range => range.Min)
                .ToList();

            if (divisionRanges.Count == 0)
            {
                AddPanelBox(mesh, ownerId, toWorld,
                    new Vector3(x1, yShelf, z1),
                    new Vector3(x2, yShelf + shelfT, z2),
                    FaceKind.ModuleTop, FaceKind.ModuleTop,
                    structure.Shelves.Count > 1 ? $"Prateleira {index}" : "Prateleira",
                    overrides);
                continue;
            }

            // A prateleira é uma peça por vão. As extremidades internas usam a
            // mesma folga lateral configurada e seguem a posição paramétrica das
            // divisórias quando a largura do módulo muda.
            float cursor = x1;
            int segmentIndex = 0;
            foreach (var range in divisionRanges)
            {
                AddShelfSegment(range.Min - wi);
                cursor = range.Max + wi;
            }
            AddShelfSegment(x2);

            void AddShelfSegment(float end)
            {
                if (end <= cursor + 0.5f)
                    return;

                segmentIndex++;
                string label = structure.Shelves.Count == 1
                    ? $"Prateleira {segmentIndex}"
                    : $"Prateleira {index}.{segmentIndex}";
                AddPanelBox(mesh, ownerId, toWorld,
                    new Vector3(cursor, yShelf, z1),
                    new Vector3(end, yShelf + shelfT, z2),
                    FaceKind.ModuleTop, FaceKind.ModuleTop, label, overrides);
            }
        }
    }

    internal static void AddFronts(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        float moduleWidth,
        float moduleHeight,
        float moduleDepth,
        float frontThickness,
        ModulationStructure structure,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides = null)
    {
        var rects = ModulationFrontLayout.Layout(moduleWidth, moduleHeight, structure);
        float zBack = moduleDepth;
        float zFront = moduleDepth + frontThickness;

        foreach (var rect in rects)
        {
            string label = rect.Type switch
            {
                ModulationFrontType.Drawer => rect.Label.StartsWith("Gaveta", StringComparison.Ordinal)
                    ? rect.Label
                    : $"Gaveta {rect.Label}",
                ModulationFrontType.Door => rect.Label.StartsWith("Porta", StringComparison.Ordinal)
                    ? rect.Label
                    : $"Porta {rect.Label}",
                _ => rect.Label
            };

            // Painel da frente com espessura: apenas a face visível (+Z) é ModuleFront,
            // as bordas de espessura ficam como laterais (mantém contagem de frentes).
            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(rect.X1, rect.Y1, zBack),
                new Vector3(rect.X2, rect.Y2, zFront),
                FaceKind.ModuleRight,
                FaceKind.ModuleFront,
                label,
                overrides);
        }
    }

    internal static void AddPanelBox(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        Vector3 min,
        Vector3 max,
        FaceKind bodyKind,
        FaceKind frontKind,
        string label,
        IReadOnlyDictionary<string, PartDimensionOverride>? overrides = null)
    {
        // Ajuste por peça: absoluto congela o tamanho (âncora no min); em seguida
        // cada face recebe seu deslocamento independente (ponto de referência da seta).
        if (overrides != null &&
            !string.IsNullOrEmpty(label) &&
            overrides.TryGetValue(label, out var ov) &&
            ov != null)
        {
            const float minSize = 1f;

            if (ov.Width is > 0f) max.X = min.X + ov.Width.Value;
            if (ov.Height is > 0f) max.Y = min.Y + ov.Height.Value;
            if (ov.Depth is > 0f) max.Z = min.Z + ov.Depth.Value;

            min.X -= ov.MinXOffset; max.X += ov.MaxXOffset;
            min.Y -= ov.MinYOffset; max.Y += ov.MaxYOffset;
            min.Z -= ov.MinZOffset; max.Z += ov.MaxZOffset;

            EnsureMinSize(ref min.X, ref max.X, minSize);
            EnsureMinSize(ref min.Y, ref max.Y, minSize);
            EnsureMinSize(ref min.Z, ref max.Z, minSize);
        }

        var a = toWorld(new Vector3(min.X, min.Y, min.Z));
        var b = toWorld(new Vector3(max.X, min.Y, min.Z));
        var c = toWorld(new Vector3(max.X, max.Y, min.Z));
        var d = toWorld(new Vector3(min.X, max.Y, min.Z));
        var e = toWorld(new Vector3(min.X, min.Y, max.Z));
        var f = toWorld(new Vector3(max.X, min.Y, max.Z));
        var g = toWorld(new Vector3(max.X, max.Y, max.Z));
        var h = toWorld(new Vector3(min.X, max.Y, max.Z));

        static void EnsureMinSize(ref float lo, ref float hi, float minSize)
        {
            if (hi - lo >= minSize)
                return;

            float center = (lo + hi) * 0.5f;
            lo = center - minSize * 0.5f;
            hi = center + minSize * 0.5f;
        }

        mesh.AddQuad(f, e, h, g, frontKind, ownerId, label); // face frontal (+Z)
        mesh.AddQuad(a, b, c, d, bodyKind, ownerId, label);  // traseira (−Z)
        mesh.AddQuad(e, a, d, h, bodyKind, ownerId, label);  // esquerda (−X)
        mesh.AddQuad(b, f, g, c, bodyKind, ownerId, label);  // direita (+X)
        mesh.AddQuad(e, f, b, a, bodyKind, ownerId, label);  // inferior (−Y)
        mesh.AddQuad(d, c, g, h, bodyKind, ownerId, label);  // superior (+Y)
    }

    private static void AddBoxLocal(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        Vector3 min,
        Vector3 max,
        FaceKind backKind)
    {
        var a = toWorld(new Vector3(min.X, min.Y, min.Z));
        var b = toWorld(new Vector3(max.X, min.Y, min.Z));
        var c = toWorld(new Vector3(max.X, max.Y, min.Z));
        var d = toWorld(new Vector3(min.X, max.Y, min.Z));

        var e = toWorld(new Vector3(min.X, min.Y, max.Z));
        var f = toWorld(new Vector3(max.X, min.Y, max.Z));
        var g = toWorld(new Vector3(max.X, max.Y, max.Z));
        var h = toWorld(new Vector3(min.X, max.Y, max.Z));

        mesh.AddQuad(a, b, c, d, FaceKind.ModuleBottom, ownerId, "Base");
        mesh.AddQuad(f, e, h, g, FaceKind.ModuleTop, ownerId, "Tampo");
        mesh.AddQuad(e, a, d, h, FaceKind.ModuleLeft, ownerId, "Lateral esq.");
        mesh.AddQuad(b, f, g, c, FaceKind.ModuleRight, ownerId, "Lateral dir.");
        mesh.AddQuad(a, e, f, b, backKind, ownerId, "Fundo");
    }

    private static void AddQuadFrontLocal(
        MeshData mesh,
        Guid ownerId,
        Func<Vector3, Vector3> toWorld,
        Vector3 min,
        Vector3 max,
        int doorCount,
        int drawerCount)
    {
        float width = max.X - min.X;
        float height = max.Y - min.Y;
        float gap = 4f;

        if (drawerCount > 0)
        {
            float drawerHeight = (height - gap * (drawerCount + 1)) / drawerCount;

            for (int i = 0; i < drawerCount; i++)
            {
                float y1 = min.Y + gap + i * (drawerHeight + gap);
                float y2 = y1 + drawerHeight;
                mesh.AddQuad(
                    toWorld(new Vector3(min.X, y1, min.Z)),
                    toWorld(new Vector3(max.X, y1, min.Z)),
                    toWorld(new Vector3(max.X, y2, min.Z)),
                    toWorld(new Vector3(min.X, y2, min.Z)),
                    FaceKind.ModuleFront,
                    ownerId,
                    $"Gaveta {i + 1}");
            }

            return;
        }

        int count = Math.Max(1, doorCount);
        float doorWidth = (width - gap * (count + 1)) / count;

        for (int i = 0; i < count; i++)
        {
            float x1 = min.X + gap + i * (doorWidth + gap);
            float x2 = x1 + doorWidth;
            mesh.AddQuad(
                toWorld(new Vector3(x1, min.Y, min.Z)),
                toWorld(new Vector3(x2, min.Y, min.Z)),
                toWorld(new Vector3(x2, max.Y, min.Z)),
                toWorld(new Vector3(x1, max.Y, min.Z)),
                FaceKind.ModuleFront,
                ownerId,
                $"Porta {i + 1}");
        }
    }
}
