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
        Action<ModulationStructure>? structureMutator = null)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;

        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

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
        float lateralBaseY = MathF.Max(0f, structure.LateralBaseOverlapMm);
        AddPanelBox(mesh, id, ToWorld,
            new Vector3(0f, lateralBaseY, 0f), new Vector3(t, h, d),
            FaceKind.ModuleLeft, FaceKind.ModuleLeft, "Lateral esq.", overrides);
        AddPanelBox(mesh, id, ToWorld,
            new Vector3(w - t, lateralBaseY, 0f), new Vector3(w, h, d),
            FaceKind.ModuleRight, FaceKind.ModuleRight, "Lateral dir.", overrides);

        // Fundo e sarrafo conforme montagem (V3.7f Fase 3c).
        BuildBackAssembly(mesh, id, ToWorld, w, h, d, t, bt, structure, overrides);

        // Base inferior (à frente do fundo) — avanços/recuo do configurador Inferior.
        float backPlaneZ = GetBackPlaneZ(structure, bt);
        float abf = Math.Clamp(structure.BaseAdvanceOverBackMm, 0f, bt);
        float recBase = Math.Clamp(structure.BaseRecessMm, 0f, MathF.Max(0f, d - backPlaneZ - 10f));
        float baseZ0 = MathF.Max(0f, backPlaneZ - abf);
        float baseZ1 = MathF.Max(baseZ0 + 10f, d - recBase);
        float baseX0 = t;
        float baseX1 = w - t;
        // Avanço Base sobre Lateral (fix-lb via LateralBaseOverlapMm já sobe a lateral;
        // base permanece entre laterais — paridade visual do vão interno).
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
            float recuoFro    = MathF.Max(0f, structure.SarrafoDianteiroRecessMm);

            // ── Sarrafo TRASEIRO ─────────────────────────────────────────────────
            if (structure.BackSarrafoVisible)
            {
                if (!structure.BackSarrafoIsVertical)
                {
                    AddPanelBox(mesh, id, ToWorld,
                        new Vector3(t, h - t, 0f), new Vector3(w - t, h, sarrafoHtra),
                        FaceKind.ModuleTop, FaceKind.ModuleTop, "Sarrafo traseiro", overrides);
                }
                else
                {
                    AddPanelBox(mesh, id, ToWorld,
                        new Vector3(t, h - sarrafoHtra, 0f),
                        new Vector3(w - t, h, sarrafoT),
                        FaceKind.ModuleTop, FaceKind.ModuleTop, "Sarrafo traseiro", overrides);
                }
            }

            // ── Sarrafo DIANTEIRO ────────────────────────────────────────────────
            if (structure.FrontSarrafoVisible)
            {
                float zFroEnd = d - recuoFro;
                float zFro0   = zFroEnd - sarrafoH;
                if (!structure.FrontSarrafoIsVertical)
                {
                    AddPanelBox(mesh, id, ToWorld,
                        new Vector3(t, h - t, zFro0), new Vector3(w - t, h, zFroEnd),
                        FaceKind.ModuleTop, FaceKind.ModuleTop, "Sarrafo dianteiro", overrides);
                }
                else
                {
                    float zFroVertEnd = d - recuoFro;
                    float zFroVert0   = MathF.Max(0f, zFroVertEnd - sarrafoT);
                    AddPanelBox(mesh, id, ToWorld,
                        new Vector3(t, h - sarrafoH, zFroVert0),
                        new Vector3(w - t, h, zFroVertEnd),
                        FaceKind.ModuleTop, FaceKind.ModuleTop, "Sarrafo dianteiro", overrides);
                }
            }
        }

        // Prateleira interna.
        AddShelves(mesh, id, ToWorld, w, h, d, t, bt, structure, overrides);

        // Frentes (portas/gavetas) por fora da caixaria — face traseira na frente do módulo (paridade Promob).
        if (includeFronts)
            AddFronts(mesh, id, ToWorld, w, h, d, ft, structure, overrides);
    }

    private static float GetBackPlaneZ(ModulationStructure structure, float backThickness) =>
        structure.BackPanelType == BoxBackPanelType.Pregado
            ? backThickness
            : MathF.Max(backThickness, structure.BackRecessMm + backThickness);

    private static void BuildBackAssembly(
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

        // Fundo: painel único encostado na traseira do módulo.
        // Travessas (Trav Vertical / Trav Horizontal) rendem o mesmo painel — a distinção
        // é apenas estrutural/construtiva e não altera a geometria 3D aqui.
        float z0 = structure.BackPanelType == BoxBackPanelType.Pregado
            ? 0f
            : MathF.Max(0f, structure.BackRecessMm);

        // Fixação Fundo - Lateral / Base - Fundo (configurador Inferior).
        float afl = Math.Clamp(structure.BackAdvanceOverLateralMm, 0f, t);
        float alf = Math.Clamp(structure.LateralAdvanceOverBackMm, 0f, t);
        float afb = Math.Clamp(structure.BackAdvanceOverBaseMm, 0f, t);

        float x0 = Math.Clamp(t - afl + alf, 0f, moduleWidth * 0.45f);
        float x1 = Math.Clamp(moduleWidth - t + afl - alf, moduleWidth * 0.55f, moduleWidth);
        // afb=0 → fundo assenta sobre a base (y=t); afb>0 → avança sobre a base.
        float y0 = MathF.Max(0f, t - afb);

        AddPanelBox(mesh, ownerId, toWorld,
            new Vector3(x0, y0, z0), new Vector3(x1, moduleHeight, z0 + bt),
            FaceKind.ModuleBack, FaceKind.ModuleBack, "Fundo", overrides);
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
            float wi = MathF.Max(0f, shelf.WidthInsetMm);
            float di = MathF.Max(0f, shelf.DepthInsetMm);

            float x1 = panelThickness + wi;
            float x2 = moduleWidth - panelThickness - wi;
            float z2 = MathF.Max(shelfZ0 + 1f, moduleDepth - di);

            if (x2 <= x1)
                continue;

            AddPanelBox(mesh, ownerId, toWorld,
                new Vector3(x1, yShelf, shelfZ0),
                new Vector3(x2, yShelf + shelfT, z2),
                FaceKind.ModuleTop, FaceKind.ModuleTop,
                structure.Shelves.Count > 1 ? $"Prateleira {index}" : "Prateleira",
                overrides);
        }
    }

    private static void AddFronts(
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

    private static void AddPanelBox(
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
