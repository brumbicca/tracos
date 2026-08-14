using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Canto Reto (CR) Inferiores — caixaria + frentes como no Promob.
/// Dimensões L×A×P do configurador = caixaria (laterais). Frentes à frente da caixa.
/// Frontais: Frente falsa · Fechamento frontal (à frente da falsa) · Porta(s).
/// Distanciador opcional (<see cref="BlindCornerParams.UseSpacer"/> / <c>cr-uso-dist</c>).
/// CR Esq = portas à esquerda; CR Dir = portas à direita. Sem peça diagonal.
/// Não altera o Canto L.
/// </summary>
public static class ModuleCornerMeshBuilder
{
    private const float DefaultBlindFalseMm = 450f;
    /// <summary>Promob «Frente p/ Fechamento 18» — largura padrão 30 mm.</summary>
    private const float DefaultFechamentoMm = 30f;
    private const float FrontGapMm = 1.5f;
    private const float StretcherDepthMm = 80f;
    /// <summary>Profundidade típica do distanciador quando M=0 e UseSpacer=Sim.</summary>
    private const float DefaultSpacerDepthMm = 50f;

    public static void BuildBlindCorner(
        ModuleInstance instance,
        ModuleDefinition definition,
        bool leftHand,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;

        dimensionSettings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(dimensionSettings);
        ChapaConfiguratorService.EnsureChapasInitialized(dimensionSettings);

        instance.BlindCorner ??= BlindCornerParams.FromConfigurator(dimensionSettings);
        var cr = ResolveCantoRetoOptions(dimensionSettings, instance.BlindCorner);

        // Caixaria idêntica ao balcão reto (fundo/avanços/sarrafos/base/prateleira do configurador).
        // crs-tipo-fro: Sem → omite dianteiro; Parcial → omite o inteiro e recoloca abaixo.
        bool sarrafoParcial = cr.SarrafoFrontalTipo.Equals("Parcial", StringComparison.OrdinalIgnoreCase);
        bool sarrafoSem = cr.SarrafoFrontalTipo.Equals("Sem sarrafo", StringComparison.OrdinalIgnoreCase)
                          || cr.SarrafoFrontalTipo.Equals("Sem", StringComparison.OrdinalIgnoreCase);

        ModuleMeshBuilder.BuildCarcass(
            instance,
            definition,
            dimensionSettings,
            includeFronts: false,
            structureMutator: structure =>
            {
                // M — Recuo Prateleira do Canto Reto (e profundidade do distanciador).
                float shelfInset = structure.Shelves is { Count: > 0 }
                    ? MathF.Max(0f, structure.Shelves[0].DepthInsetMm)
                    : 20f;
                if (cr.RecuoPrateleiraMm > 0f)
                    shelfInset = cr.RecuoPrateleiraMm;
                else if (cr.UseSpacer)
                    shelfInset = MathF.Max(shelfInset, DefaultSpacerDepthMm);

                foreach (var shelf in structure.Shelves)
                    shelf.DepthInsetMm = shelfInset;

                if (sarrafoSem || sarrafoParcial)
                    structure.FrontSarrafoVisible = false;
            });

        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = rules?.Structure
            ?? ModulationRulesPresets.CreateStandardBox(definition.DoorCount, 0).Structure;

        float t = structure.PanelThicknessMm > 0f ? structure.PanelThicknessMm : 18f;
        float bt = structure.BackThicknessMm > 0f ? structure.BackThicknessMm : 6f;
        float ft = structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm
            : (definition.FrontThickness > 0f ? definition.FrontThickness : 18f);
        t = MathF.Min(t, MathF.Max(1f, (w - 2f) * 0.45f));
        bt = MathF.Min(bt, MathF.Max(1f, d - 2f));
        ft = Math.Clamp(ft, 1f, 50f);

        float sarrafoH = Math.Clamp(structure.SarrafoHeightMm > 0f ? structure.SarrafoHeightMm : StretcherDepthMm, 10f, h * 0.45f);
        float sarrafoT = Math.Clamp(structure.SarrafoThicknessMm > 0f ? structure.SarrafoThicknessMm : t, 6f, 50f);
        float lateralBaseY = MathF.Max(0f, structure.LateralBaseOverlapMm);

        float shelfInsetZ = structure.Shelves is { Count: > 0 }
            ? MathF.Max(0f, structure.Shelves[0].DepthInsetMm)
            : 20f;
        if (cr.RecuoPrateleiraMm > 0f)
            shelfInsetZ = cr.RecuoPrateleiraMm;
        else if (cr.UseSpacer)
            shelfInsetZ = MathF.Max(shelfInsetZ, DefaultSpacerDepthMm);

        Vector3 World(Vector3 p) =>
            ModulePlacementService.TransformLocalPoint(p, instance.Position, instance.RotationYDegrees);

        // ═══════════════════════════════════════════════════════════════
        // Layout: porta + frente falsa; fechamento na dobradiça.
        // Tipo Lateral (Promob padrão): aleta na dobradiça p/ módulo sequencial
        //   da OUTRA parede. A cega (falsa) tem largura ≈ profundidade (d) para
        //   a dobradiça/fechamento coincidir com a frente desse sequencial.
        //   Aleta inicia na face da frente falsa e avança fechDim em +Z.
        // Tipo Frontal: faixa dim×A×18 no mesmo plano da porta/falsa.
        // ═══════════════════════════════════════════════════════════════

        int doors = Math.Max(1, definition.DoorCount);
        float fechDim = Math.Clamp(
            cr.FechamentoDimMm > 0f ? cr.FechamentoDimMm : DefaultFechamentoMm,
            12f, MathF.Min(120f, w * 0.25f));
        bool fechamentoLateral = cr.FechamentoTipoLateral;

        float doorMin = doors >= 2 ? 400f : 200f;
        float falsa;
        if (fechamentoLateral)
        {
            // Cega ≈ profundidade: dobradiça no plano da frente do sequencial (outra parede).
            falsa = Math.Clamp(d, 200f, MathF.Max(200f, w - doorMin));
        }
        else
        {
            falsa = Math.Clamp(DefaultBlindFalseMm, w * 0.28f, w * 0.55f);
        }

        // Lateral: dimensão em Z — não consome largura da frente além da cega alinhada.
        float doorSpan = fechamentoLateral
            ? MathF.Max(doorMin * 0.5f, w - falsa)
            : MathF.Max(180f, w - falsa - fechDim);

        if (doors >= 2 && doorSpan < 400f && !fechamentoLateral)
        {
            falsa = MathF.Min(falsa, w * 0.36f);
            doorSpan = MathF.Max(200f, w - falsa - fechDim);
        }

        float door0, door1, falsa0, falsa1, hingeX;
        if (leftHand)
        {
            // CR Esq: portas à esquerda | dobradiça | frente falsa
            door0 = 0f;
            door1 = doorSpan;
            falsa0 = doorSpan;
            falsa1 = w;
            hingeX = doorSpan;
            if (!fechamentoLateral)
            {
                // Faixa frontal entre porta e falsa.
                falsa0 = doorSpan + fechDim;
                hingeX = doorSpan;
            }
        }
        else
        {
            door0 = w - doorSpan;
            door1 = w;
            falsa0 = 0f;
            falsa1 = w - doorSpan;
            hingeX = w - doorSpan;
            if (!fechamentoLateral)
            {
                falsa1 = door0 - fechDim;
                hingeX = door0;
            }
        }

        float avaPor = Math.Clamp(cr.AvancoPortaSobreFfMm, 0f, fechDim + 40f);
        if (leftHand)
            door1 = MathF.Min(w - FrontGapMm, door1 + avaPor);
        else
            door0 = MathF.Max(FrontGapMm, door0 - avaPor);

        float y0 = FrontGapMm;
        float y1 = h - FrontGapMm;

        float fechRecuo = MathF.Max(0f, cr.FechamentoRecuoMm);
        float zPorta0 = d + fechRecuo;
        float zPorta1 = d + fechRecuo + ft;

        float falsaRecuo = Math.Clamp(cr.RecuoFfMm, 0f, MathF.Min(d * 0.4f, 80f));
        float zFalsa0 = d - falsaRecuo;
        float zFalsa1 = zFalsa0 + ft;

        float spacerDepth = cr.UseSpacer
            ? Math.Clamp(shelfInsetZ > 0f ? shelfInsetZ : DefaultSpacerDepthMm, 20f, d * 0.6f)
            : 0f;
        float spacerT = t;
        float spacerZ1 = zFalsa0 + MathF.Max(0f, cr.AvancoDistSobreFfMm);
        float spacerZ0 = MathF.Max(bt, spacerZ1 - spacerDepth - MathF.Max(0f, cr.AvancoDistSobrePratMm));
        float spacerY0 = t + lateralBaseY;
        float spacerY1 = h - sarrafoT;

        if (cr.UseSpacer)
        {
            float sx0, sx1;
            if (leftHand)
            {
                sx0 = hingeX + MathF.Max(0f, cr.AvancoFfSobreDistMm);
                sx1 = sx0 + spacerT;
            }
            else
            {
                sx1 = hingeX - MathF.Max(0f, cr.AvancoFfSobreDistMm);
                sx0 = sx1 - spacerT;
            }

            Box(instance, World, id,
                new Vector3(sx0, spacerY0, spacerZ0),
                new Vector3(sx1, spacerY1, spacerZ1),
                FaceKind.ModuleFront, "Distanciador");
        }

        // Sarrafo dianteiro parcial (Promob: vai até o distanciador). Inteiro já veio da caixaria.
        if (sarrafoParcial)
        {
            float recuoFro = MathF.Max(0f, structure.SarrafoDianteiroRecessMm);
            float sarrafoFroZ1 = MathF.Max(10f, d - recuoFro - MathF.Max(0f, cr.AvancoFfSobreSarrafoMm));
            float sarrafoFroZ0 = MathF.Max(bt + 1f, sarrafoFroZ1 - sarrafoH);

            float stopX = cr.UseSpacer
                ? (leftHand
                    ? hingeX + MathF.Max(0f, cr.AvancoFfSobreDistMm)
                    : hingeX - MathF.Max(0f, cr.AvancoFfSobreDistMm))
                : hingeX;
            float sx0 = leftHand ? stopX : t;
            float sx1 = leftHand ? w - t : stopX;

            if (sx1 > sx0 + 1f)
            {
                Box(instance, World, id,
                    new Vector3(sx0, h - sarrafoT, sarrafoFroZ0),
                    new Vector3(sx1, h, sarrafoFroZ1),
                    FaceKind.ModuleTop, "Sarrafo dianteiro");
            }
        }

        float affl = MathF.Max(0f, cr.AvancoFfSobreLateralMm);
        float fx0 = falsa0 + FrontGapMm + (leftHand ? 0f : affl);
        float fx1 = falsa1 - FrontGapMm - (leftHand ? affl : 0f);
        if (cr.UseSpacer)
        {
            float insetDist = spacerT + MathF.Max(0f, cr.AvancoFfSobreDistMm);
            if (leftHand)
                fx0 = MathF.Max(fx0, hingeX + insetDist);
            else
                fx1 = MathF.Min(fx1, hingeX - insetDist);
        }

        if (fx1 > fx0 + 1f)
        {
            Box(instance, World, id,
                new Vector3(fx0, y0, zFalsa0),
                new Vector3(fx1, y1, zFalsa1),
                FaceKind.ModuleFront, "Frente falsa");
        }

        if (cr.TipoFfParcialDupla && cr.DimFfParcialMm > 0f)
        {
            float partialW = Math.Clamp(cr.DimFfParcialMm, 20f, falsa * 0.8f);
            float rffp = Math.Clamp(cr.RecuoFfParcialMm, 0f, 80f);
            float zP0 = d - rffp;
            float zP1 = zP0 + ft;
            float px0, px1;
            if (leftHand)
            {
                px0 = hingeX + FrontGapMm;
                px1 = px0 + partialW;
            }
            else
            {
                px1 = hingeX - FrontGapMm;
                px0 = px1 - partialW;
            }

            Box(instance, World, id,
                new Vector3(px0, y0, zP0),
                new Vector3(px1, y1, zP1),
                FaceKind.ModuleFront, "Frente falsa parcial");
        }

        // Fechamento — inicia na face da frente falsa; face alinhada à profundidade
        // (dobradiça em X≈d) = frente do módulo sequencial da outra parede.
        // Lateral: aleta 18×A×dim a partir de zFalsa1. Frontal: faixa no plano da falsa.
        float zFech0 = fechamentoLateral
            ? zFalsa1 + fechRecuo
            : zFalsa0 + fechRecuo;
        float zFech1 = zFech0 + (fechamentoLateral ? fechDim : ft);
        if (fechamentoLateral)
        {
            float x0, x1;
            if (leftHand)
            {
                // CR Esq: face para o sequencial à direita (X crescente).
                x0 = hingeX;
                x1 = hingeX + ft;
            }
            else
            {
                // CR Dir: face para o sequencial à esquerda (X = d = profundidade).
                x0 = hingeX - ft;
                x1 = hingeX;
            }

            Box(instance, World, id,
                new Vector3(x0, y0, zFech0),
                new Vector3(x1, y1, zFech1),
                leftHand ? FaceKind.ModuleRight : FaceKind.ModuleLeft,
                "Fechamento frontal");
        }
        else
        {
            float fech0 = leftHand ? hingeX : hingeX - fechDim;
            float fech1 = leftHand ? hingeX + fechDim : hingeX;
            Box(instance, World, id,
                new Vector3(fech0, y0, zFech0),
                new Vector3(fech1, y1, zFech1),
                FaceKind.ModuleFront, "Fechamento frontal");
        }

        if (cr.FechamentoSuperior)
        {
            Box(instance, World, id,
                new Vector3(leftHand ? hingeX - ft : hingeX, h - ft - FrontGapMm, zFech0),
                new Vector3(leftHand ? hingeX : hingeX + ft, h - FrontGapMm, zFech1),
                FaceKind.ModuleFront, "Fechamento superior");
        }

        if (cr.FechamentoInferior)
        {
            Box(instance, World, id,
                new Vector3(leftHand ? hingeX - ft : hingeX, FrontGapMm, zFech0),
                new Vector3(leftHand ? hingeX : hingeX + ft, FrontGapMm + ft, zFech1),
                FaceKind.ModuleFront, "Fechamento inferior");
        }

        if (cr.FechamentoTraseiro)
        {
            float backZ0 = structure.BackPanelType == BoxBackPanelType.Pregado
                ? 0f
                : MathF.Max(0f, structure.BackRecessMm);
            Box(instance, World, id,
                new Vector3(leftHand ? hingeX - ft : hingeX, y0, backZ0),
                new Vector3(leftHand ? hingeX : hingeX + ft, y1, backZ0 + bt),
                FaceKind.ModuleBack, "Fechamento traseiro");
        }

        float doorClear = door1 - door0;
        for (int i = 0; i < doors; i++)
        {
            float seg = doorClear / doors;
            float x0 = door0 + i * seg + FrontGapMm;
            float x1 = door0 + (i + 1) * seg - FrontGapMm;
            if (x1 <= x0 + 1f)
                continue;

            string label = doors == 1 ? "Porta" : $"Porta {i + 1}";
            Box(instance, World, id,
                new Vector3(x0, y0, zPorta0),
                new Vector3(x1, y1, zPorta1),
                FaceKind.ModuleFront, label);
        }
    }

    private sealed class CantoRetoOptions
    {
        public bool UseSpacer;
        public bool TipoFfParcialDupla;
        /// <summary>Promob crf-tipo Lateral (padrão) vs Frontal.</summary>
        public bool FechamentoTipoLateral = true;
        public float AvancoFfSobreBaseMm;
        public float AvancoFfSobreSarrafoMm;
        public float AvancoFfSobreLateralMm;
        public float RecuoFfMm;
        public float RecuoFfParcialMm;
        public float DimFfParcialMm;
        public float AvancoFfSobreDistMm;
        public float AvancoDistSobreFfMm;
        public float AvancoDistSobrePratMm;
        public float RecuoPrateleiraMm;
        public float AvancoPortaSobreFfMm;
        public float FechamentoDimMm;
        public float FechamentoRecuoMm;
        public bool FechamentoSuperior;
        public bool FechamentoInferior;
        public bool FechamentoTraseiro;
        public string SarrafoFrontalTipo = "Parcial";
    }

    /// <summary>
    /// Lê Montagem Caixa Inferior → Canto Reto (cr-*, crf-*, crs-*) + UseSpacer da instância.
    /// </summary>
    private static CantoRetoOptions ResolveCantoRetoOptions(
        DimensionConfiguratorSettings settings,
        BlindCornerParams blind)
    {
        var box = settings.CozinhaInferiorBox;
        var opts = new CantoRetoOptions
        {
            UseSpacer = blind.UseSpacer,
            FechamentoDimMm = DefaultFechamentoMm,
            FechamentoTipoLateral = true,
            SarrafoFrontalTipo = "Parcial"
        };

        if (box.InferiorChoice.TryGetValue("cr-tipo-ff", out var tipoFf) &&
            tipoFf.Contains("Parcial", StringComparison.OrdinalIgnoreCase))
            opts.TipoFfParcialDupla = true;

        if (box.InferiorChoice.TryGetValue("crf-tipo", out var tipoFech) &&
            !string.IsNullOrWhiteSpace(tipoFech))
        {
            opts.FechamentoTipoLateral =
                tipoFech.StartsWith("Lateral", StringComparison.OrdinalIgnoreCase);
        }

        ReadNum(box, "cr-affb", v => opts.AvancoFfSobreBaseMm = v);
        ReadNum(box, "cr-affs", v => opts.AvancoFfSobreSarrafoMm = v);
        ReadNum(box, "cr-affl", v => opts.AvancoFfSobreLateralMm = v);
        ReadNum(box, "cr-rff", v => opts.RecuoFfMm = v);
        ReadNum(box, "cr-rffp", v => opts.RecuoFfParcialMm = v);
        ReadNum(box, "cr-dim-ffp", v => opts.DimFfParcialMm = v);
        ReadNum(box, "cr-affd", v => opts.AvancoFfSobreDistMm = v);
        ReadNum(box, "cr-adff", v => opts.AvancoDistSobreFfMm = v);
        ReadNum(box, "cr-adp", v => opts.AvancoDistSobrePratMm = v);
        ReadNum(box, "cr-rec-prat", v => opts.RecuoPrateleiraMm = v);
        ReadNum(box, "cr-ava-por", v => opts.AvancoPortaSobreFfMm = v);
        ReadNum(box, "crf-dim-fro", v => opts.FechamentoDimMm = v);
        ReadNum(box, "crf-recuo-fro", v => opts.FechamentoRecuoMm = v);

        opts.FechamentoSuperior = IsSim(box, "crf-sup");
        opts.FechamentoInferior = IsSim(box, "crf-inf");
        opts.FechamentoTraseiro = IsSim(box, "crf-tra");

        if (box.InferiorChoice.TryGetValue("crs-tipo-fro", out var sarTipo) &&
            !string.IsNullOrWhiteSpace(sarTipo))
            opts.SarrafoFrontalTipo = sarTipo;

        return opts;
    }

    private static void ReadNum(BoxAssemblySectionSettings box, string key, Action<float> apply)
    {
        if (box.InferiorNumeric.TryGetValue(key, out var v) && v >= 0f)
            apply(v);
    }

    private static bool IsSim(BoxAssemblySectionSettings box, string key) =>
        box.InferiorChoice.TryGetValue(key, out var v) &&
        v.Equals("Sim", StringComparison.OrdinalIgnoreCase);

    public static void BuildCornerL(
        ModuleInstance instance,
        ModuleDefinition definition,
        bool leftHand)
    {
        // Mantido — próximo refinamento com a mesma regra de profundidade da caixa.
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = 18f;
        float ft = Math.Clamp(definition.FrontThickness, 12f, 25f);
        float legA = MathF.Max(320f, w * 0.42f);
        float legB = MathF.Max(320f, d * 0.85f);
        int doors = Math.Max(1, definition.DoorCount);
        var id = instance.Id;

        Vector3 World(Vector3 p) =>
            ModulePlacementService.TransformLocalPoint(p, instance.Position, instance.RotationYDegrees);

        Box(instance, World, id, new Vector3(0f, 0f, 0f), new Vector3(w, h, t), FaceKind.ModuleBack, "Fundo");
        Box(instance, World, id, new Vector3(0f, 0f, 0f), new Vector3(t, h, d), FaceKind.ModuleLeft, "Lateral esq.");
        Box(instance, World, id, new Vector3(w - t, 0f, 0f), new Vector3(w, h, d), FaceKind.ModuleRight, "Lateral dir.");

        if (leftHand)
        {
            Box(instance, World, id, new Vector3(legA, 0f, legB - t), new Vector3(w - t, h, legB), FaceKind.ModuleFront, "Divisão");
            Box(instance, World, id, new Vector3(legA - t, 0f, legB), new Vector3(legA, h, d), FaceKind.ModuleRight, "Divisão");
            Box(instance, World, id, new Vector3(t, 0f, t), new Vector3(w - t, t, legB), FaceKind.ModuleBottom, "Base");
            Box(instance, World, id, new Vector3(t, 0f, legB), new Vector3(legA, t, d), FaceKind.ModuleBottom, "Base");
            Box(instance, World, id, new Vector3(t, h - t, t), new Vector3(w - t, h, t + 60f), FaceKind.ModuleTop, "Sarrafo");
            PlaceLDoors(instance, World, id, doors, ft, leftHand: true, legA, legB, w, d, h);
        }
        else
        {
            float legAx = w - legA;
            Box(instance, World, id, new Vector3(t, 0f, legB - t), new Vector3(legAx, h, legB), FaceKind.ModuleFront, "Divisão");
            Box(instance, World, id, new Vector3(legAx, 0f, legB), new Vector3(legAx + t, h, d), FaceKind.ModuleLeft, "Divisão");
            Box(instance, World, id, new Vector3(t, 0f, t), new Vector3(w - t, t, legB), FaceKind.ModuleBottom, "Base");
            Box(instance, World, id, new Vector3(legAx, 0f, legB), new Vector3(w - t, t, d), FaceKind.ModuleBottom, "Base");
            Box(instance, World, id, new Vector3(t, h - t, t), new Vector3(w - t, h, t + 60f), FaceKind.ModuleTop, "Sarrafo");
            PlaceLDoors(instance, World, id, doors, ft, leftHand: false, legA, legB, w, d, h);
        }
    }

    private static void PlaceLDoors(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Guid id,
        int doors,
        float ft,
        bool leftHand,
        float legA,
        float legB,
        float w,
        float d,
        float h)
    {
        float pad = FrontGapMm;
        if (doors == 1)
        {
            if (leftHand)
                Box(instance, world, id, new Vector3(legA + pad, pad, legB), new Vector3(w - 18f - pad, h - pad, legB + ft), FaceKind.ModuleFront, "Porta");
            else
                Box(instance, world, id, new Vector3(18f + pad, pad, legB), new Vector3(w - legA - pad, h - pad, legB + ft), FaceKind.ModuleFront, "Porta");
            return;
        }

        if (leftHand)
        {
            Box(instance, world, id, new Vector3(legA + pad, pad, legB), new Vector3(w - 18f - pad, h - pad, legB + ft), FaceKind.ModuleFront, "Porta 1");
            Box(instance, world, id, new Vector3(legA, pad, legB + pad), new Vector3(legA + ft, h - pad, d - 18f - pad), FaceKind.ModuleFront, "Porta 2");
        }
        else
        {
            Box(instance, world, id, new Vector3(18f + pad, pad, legB), new Vector3(w - legA - pad, h - pad, legB + ft), FaceKind.ModuleFront, "Porta 1");
            Box(instance, world, id, new Vector3(w - legA - ft, pad, legB + pad), new Vector3(w - legA, h - pad, d - 18f - pad), FaceKind.ModuleFront, "Porta 2");
        }
    }

    public static void BuildOblique(ModuleInstance instance, ModuleDefinition definition)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = 18f;
        float ft = Math.Clamp(definition.FrontThickness, 12f, 25f);
        float cut = MathF.Min(w, d) * 0.38f;
        int doors = Math.Max(1, definition.DoorCount);
        if (definition.DisplayName.Contains("Ajust", StringComparison.OrdinalIgnoreCase))
            cut *= 0.85f;

        var id = instance.Id;
        Vector3 World(Vector3 p) =>
            ModulePlacementService.TransformLocalPoint(p, instance.Position, instance.RotationYDegrees);

        Box(instance, World, id, new Vector3(0f, 0f, 0f), new Vector3(t, h, d - cut), FaceKind.ModuleLeft, "Lateral esq.");
        Box(instance, World, id, new Vector3(w - t, 0f, 0f), new Vector3(w, h, d - cut), FaceKind.ModuleRight, "Lateral dir.");
        Box(instance, World, id, new Vector3(t, 0f, 0f), new Vector3(w - t, h, t), FaceKind.ModuleBack, "Fundo");
        Box(instance, World, id, new Vector3(t, 0f, t), new Vector3(w - t, t, d - cut), FaceKind.ModuleBottom, "Base");
        Box(instance, World, id, new Vector3(cut * 0.15f, 0f, d - cut), new Vector3(w - cut * 0.15f, t, d), FaceKind.ModuleBottom, "Base");
        Box(instance, World, id, new Vector3(t, h - t, t), new Vector3(w - t, h, t + StretcherDepthMm), FaceKind.ModuleTop, "Sarrafo");

        float steps = 6f;
        for (int i = 0; i < (int)steps; i++)
        {
            float u0 = i / steps;
            float u1 = (i + 1) / steps;
            float x0 = cut * (1f - u0) + (w - cut) * u0;
            float x1 = cut * (1f - u1) + (w - cut) * u1;
            float z0 = (d - cut) + cut * u0;
            float z1 = (d - cut) + cut * u1;
            float xMin = MathF.Min(x0, x1);
            float xMax = MathF.Max(x0, x1);
            float zMin = MathF.Min(z0, z1);
            float zMax = MathF.Max(z0, z1) + 4f;
            if (xMax - xMin < 4f)
                xMax = xMin + 4f;
            Box(instance, World, id, new Vector3(xMin, 0f, zMin), new Vector3(xMax, h, zMax), FaceKind.ModuleFront, "Caixa");
        }

        for (int i = 0; i < doors; i++)
        {
            float u0 = (float)i / doors;
            float u1 = (float)(i + 1) / doors;
            float x0 = cut + (w - 2f * cut) * u0 + FrontGapMm;
            float x1 = cut + (w - 2f * cut) * u1 - FrontGapMm;
            float zMid = d - cut * 0.35f;
            Box(instance, World, id, new Vector3(x0, FrontGapMm, zMid), new Vector3(x1, h - FrontGapMm, zMid + ft), FaceKind.ModuleFront, doors == 1 ? "Porta" : $"Porta {i + 1}");
        }
    }

    private static void Box(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        Vector3 min,
        Vector3 max,
        FaceKind kind,
        string label = "")
    {
        if (max.X <= min.X || max.Y <= min.Y || max.Z <= min.Z)
            return;

        var mesh = instance.Mesh;
        var a = toWorld(new Vector3(min.X, min.Y, min.Z));
        var b = toWorld(new Vector3(max.X, min.Y, min.Z));
        var c = toWorld(new Vector3(max.X, max.Y, min.Z));
        var d = toWorld(new Vector3(min.X, max.Y, min.Z));
        var e = toWorld(new Vector3(min.X, min.Y, max.Z));
        var f = toWorld(new Vector3(max.X, min.Y, max.Z));
        var g = toWorld(new Vector3(max.X, max.Y, max.Z));
        var h = toWorld(new Vector3(min.X, max.Y, max.Z));

        mesh.AddQuad(a, b, c, d, kind, id, label);
        mesh.AddQuad(e, f, g, h, kind, id, label);
        mesh.AddQuad(a, e, h, d, kind, id, label);
        mesh.AddQuad(b, f, g, c, kind, id, label);
        mesh.AddQuad(d, c, g, h, kind, id, label);
        mesh.AddQuad(a, b, f, e, kind, id, label);
    }
}
