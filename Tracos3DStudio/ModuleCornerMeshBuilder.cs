using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Canto Reto (CR) Inferiores — caixaria + frentes como no Promob.
/// Dimensões L×A×P do configurador = caixaria (laterais). Frentes à frente da caixa.
/// Frontais: Frente falsa · Fechamento frontal configurável · Porta(s).
/// Distanciador opcional (<see cref="BlindCornerParams.UseSpacer"/> / <c>cr-uso-dist</c>).
/// CR Esq = canto cego à esquerda; CR Dir = canto cego à direita. Sem peça diagonal.
/// Não altera o Canto L.
/// </summary>
public static class ModuleCornerMeshBuilder
{
    private const float DefaultBlindFalseMm = 450f;
    /// <summary>Promob «Frente p/ Fechamento 18» — largura padrão 30 mm.</summary>
    private const float DefaultFechamentoMm = 30f;
    private const float FrontGapMm = 1.5f;
    private const float StretcherDepthMm = 80f;

    public static void BuildBlindCorner(
        ModuleInstance instance,
        ModuleDefinition definition,
        bool leftHand,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        float nominalW = instance.Width;
        float h = instance.Height;
        float nominalD = instance.Depth;
        var id = instance.Id;

        dimensionSettings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(dimensionSettings);
        ChapaConfiguratorService.EnsureChapasInitialized(dimensionSettings);

        instance.BlindCorner ??= BlindCornerParams.FromConfigurator(dimensionSettings);
        var cr = ResolveCantoRetoOptions(dimensionSettings, instance.BlindCorner);
        // No catálogo, Esq./Dir. identifica o lado do canto cego (frente falsa),
        // não o lado das portas. O afastamento é descontado somente no lado
        // cego; o lado da porta permanece na dimensão nominal para encostar no
        // próximo módulo sem vão e sem atravessar a parede do canto.
        bool cornerOnLeft = leftHand;
        bool doorsOnLeft = !cornerOnLeft;
        float wallSideGap = Math.Clamp(cr.WallSideOffsetMm, -nominalW * 0.5f, nominalW - 200f);
        float wallBackGap = Math.Clamp(cr.WallBackOffsetMm, -nominalD * 0.5f, nominalD - 100f);
        float w = nominalW - wallSideGap;
        float d = nominalD - wallBackGap;
        float geometryOffsetX = cornerOnLeft ? wallSideGap : 0f;
        var cornerLocalOffset = new Vector3(geometryOffsetX, 0f, wallBackGap);

        // Afastamentos positivos ficam dentro da dimensão nominal. Somente
        // valores negativos ampliam o envelope para fora do módulo.
        float envelopeX = cornerOnLeft
            ? MathF.Min(0f, wallSideGap)
            : MathF.Max(0f, -wallSideGap);
        instance.GeometryEnvelopeLocalOffset = new Vector3(
            envelopeX, 0f, MathF.Min(0f, wallBackGap));

        // Caixaria idêntica ao balcão reto (fundo/avanços/sarrafos/base/prateleira do configurador).
        // crs-tipo-fro: Sem → omite dianteiro; Parcial → omite o inteiro e recoloca abaixo.
        bool sarrafoParcial = cr.SarrafoFrontalTipo.Equals("Parcial", StringComparison.OrdinalIgnoreCase);
        bool sarrafoSem = cr.SarrafoFrontalTipo.Equals("Sem sarrafo", StringComparison.OrdinalIgnoreCase)
                          || cr.SarrafoFrontalTipo.Equals("Sem", StringComparison.OrdinalIgnoreCase);

        instance.Width = w;
        instance.Depth = d;
        try
        {
            ModuleMeshBuilder.BuildCarcass(
                instance,
                definition,
                dimensionSettings,
                includeFronts: false,
                structureMutator: structure =>
                {
                    // M — Recuo Prateleira do Canto Reto substitui o recuo genérico.
                    // Zero significa sem recuo, inclusive quando há distanciador.
                    float shelfInset = cr.RecuoPrateleiraMm;

                    foreach (var shelf in structure.Shelves)
                        shelf.DepthInsetMm = shelfInset;

                    if (sarrafoSem || sarrafoParcial)
                        structure.FrontSarrafoVisible = false;
                },
                localOffset: cornerLocalOffset);
        }
        finally
        {
            instance.Width = nominalW;
            instance.Depth = nominalD;
        }

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
        float lateralBaseY = Math.Clamp(structure.LateralBaseOverlapMm, -h * 0.25f, h * 0.25f);

        // Mantém a mesma semântica aplicada à caixaria: M=0 encosta a
        // prateleira na frente e M também determina a profundidade do distanciador.
        float shelfInsetZ = cr.RecuoPrateleiraMm;

        Vector3 World(Vector3 p) =>
            ModulePlacementService.TransformLocalPoint(
                p + cornerLocalOffset,
                instance.Position, instance.RotationYDegrees);

        // ═══════════════════════════════════════════════════════════════
        // Layout: porta + frente falsa; fechamento na dobradiça.
        // Tipo Lateral (Promob padrão): aleta na dobradiça p/ módulo sequencial
        //   da OUTRA parede. A cega (falsa) tem largura ≈ profundidade (d) para
        //   a dobradiça/fechamento coincidir com a frente desse sequencial.
        //   O recuo B posiciona a aleta para dentro; C define seu comprimento.
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
        if (doorsOnLeft)
        {
            // Canto cego à direita: portas à esquerda | dobradiça | frente falsa
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

        float avaPor = Math.Clamp(cr.AvancoPortaSobreFfMm, -(fechDim + 40f), fechDim + 40f);
        if (doorsOnLeft)
            door1 = MathF.Min(w - FrontGapMm, door1 + avaPor);
        else
            door0 = MathF.Max(FrontGapMm, door0 - avaPor);

        float y0 = FrontGapMm;
        float y1 = h - FrontGapMm;
        float ffBaseAdvance = Math.Clamp(cr.AvancoFfSobreBaseMm, -h * 0.25f, h * 0.25f);
        float ffSarrafoAdvance = Math.Clamp(cr.AvancoFfSobreSarrafoMm, -h * 0.25f, h * 0.25f);
        float ffY0 = Math.Clamp(t - ffBaseAdvance, 0f, h - 2f);
        float ffY1 = Math.Clamp(h - sarrafoT + ffSarrafoAdvance, ffY0 + 1f, h);

        // B controla a distância lateral para o lado da frente falsa.
        // D posiciona a borda frontal do fechamento em relação à frente da
        // caixaria: positivo avança para fora e negativo recua para dentro.
        // C é a profundidade da peça e cresce dessa borda para trás.
        float fechRecuo = Math.Clamp(cr.FechamentoRecuoMm, -w * 0.35f, w * 0.35f);
        float zPorta0 = d;
        float zPorta1 = d + ft;

        float falsaRecuo = Math.Clamp(cr.RecuoFfMm, -d * 0.25f, MathF.Min(d * 0.4f, 80f));
        float zFalsa0 = d - falsaRecuo;
        float zFalsa1 = zFalsa0 + ft;

        float spacerT = t;
        float avancoFfDist = Math.Clamp(cr.AvancoFfSobreDistMm, -w * 0.25f, w * 0.25f);
        float avancoDistFf = Math.Clamp(cr.AvancoDistSobreFfMm, -d * 0.25f, d * 0.25f);
        float avancoDistPrat = Math.Clamp(cr.AvancoDistSobrePratMm, -d * 0.25f, d * 0.25f);
        float shelfFrontZ = d - shelfInsetZ;
        float spacerZ1 = zFalsa0 + avancoDistFf;
        // M posiciona a frente da prateleira e, por consequência, determina a
        // profundidade do distanciador. L permite que ele avance além da prateleira.
        float spacerZ0 = MathF.Min(shelfFrontZ - avancoDistPrat, spacerZ1 - spacerT);
        float spacerY0 = t + lateralBaseY;
        float spacerY1 = h - sarrafoT;
        float spacerX0 = doorsOnLeft ? hingeX : hingeX - spacerT;
        float spacerX1 = doorsOnLeft ? hingeX + spacerT : hingeX;

        if (cr.UseSpacer)
        {
            Box(instance, World, id,
                new Vector3(spacerX0, spacerY0, spacerZ0),
                new Vector3(spacerX1, spacerY1, spacerZ1),
                FaceKind.ModuleFront, "Distanciador");
        }

        // Sarrafo dianteiro parcial (Promob: vai até o distanciador). Inteiro já veio da caixaria.
        if (sarrafoParcial)
        {
            float recuoFro = Math.Clamp(structure.SarrafoDianteiroRecessMm, -d * 0.5f, d - 10f);
            // A medida C é sobreposição vertical da frente falsa; a posição em
            // profundidade segue o mesmo recuo de sarrafo usado nos demais inferiores.
            float sarrafoFroZ1 = d - recuoFro;
            float sarrafoFroZ0 = sarrafoFroZ1 - sarrafoH;

            float stopX = cr.UseSpacer
                ? (doorsOnLeft ? spacerX0 : spacerX1)
                : hingeX;
            float sx0 = doorsOnLeft ? t : stopX;
            float sx1 = doorsOnLeft ? stopX : w - t;

            if (sx1 > sx0 + 1f)
            {
                Box(instance, World, id,
                    new Vector3(sx0, h - sarrafoT, sarrafoFroZ0),
                    new Vector3(sx1, h, sarrafoFroZ1),
                    FaceKind.ModuleTop, "Sarrafo dianteiro");
            }
        }

        float affl = Math.Clamp(cr.AvancoFfSobreLateralMm, -t, t * 2f);
        if (doorsOnLeft)
            falsa1 = Math.Clamp(w - t + affl, hingeX + 1f, w);
        else
            falsa0 = Math.Clamp(t - affl, 0f, hingeX - 1f);

        float fx0 = falsa0 + FrontGapMm;
        float fx1 = falsa1 - FrontGapMm;
        if (cr.UseSpacer)
        {
            // J controla a sobreposição da frente falsa inteira sobre o
            // distanciador; não desloca a peça distanciadora.
            if (doorsOnLeft)
                fx0 = MathF.Max(fx0, spacerX1 - avancoFfDist);
            else
                fx1 = MathF.Min(fx1, spacerX0 + avancoFfDist);
        }

        if (fx1 > fx0 + 1f)
        {
            Box(instance, World, id,
                new Vector3(fx0, ffY0, zFalsa0),
                new Vector3(fx1, ffY1, zFalsa1),
                FaceKind.ModuleFront, "Frente falsa");
        }

        if (cr.TipoFfParcialDupla && cr.DimFfParcialMm > 0f)
        {
            float partialW = Math.Clamp(cr.DimFfParcialMm, 20f, falsa * 0.8f);
            float rffp = Math.Clamp(cr.RecuoFfParcialMm, -d * 0.25f, 80f);
            float zP0 = d - rffp;
            float zP1 = zP0 + ft;
            float px0, px1;
            float avancoParcial = Math.Clamp(cr.AvancoFfSobreFfParcialMm, -w * 0.25f, w * 0.25f);
            if (doorsOnLeft)
            {
                px0 = hingeX + FrontGapMm - avancoParcial;
                px1 = px0 + partialW;
            }
            else
            {
                px1 = hingeX - FrontGapMm + avancoParcial;
                px0 = px1 - partialW;
            }

            Box(instance, World, id,
                new Vector3(px0, ffY0, zP0),
                new Vector3(px1, ffY1, zP1),
                FaceKind.ModuleFront, "Frente falsa parcial");
        }

        float fechamentoDim = Math.Clamp(fechDim, ft, d + MathF.Max(0f, -falsaRecuo));
        float xFech0;
        float xFech1;
        if (doorsOnLeft)
        {
            xFech0 = hingeX + fechRecuo;
            xFech1 = xFech0 + (fechamentoLateral ? ft : fechamentoDim);
        }
        else
        {
            xFech1 = hingeX - fechRecuo;
            xFech0 = xFech1 - (fechamentoLateral ? ft : fechamentoDim);
        }

        float fechamentoAvanco = cr.FechamentoAvancoMm;
        float zFech1 = d + fechamentoAvanco;
        float zFech0 = zFech1 - (fechamentoLateral ? fechamentoDim : ft);
        if (fechamentoLateral)
        {
            Box(instance, World, id,
                new Vector3(xFech0, ffY0, zFech0),
                new Vector3(xFech1, ffY1, zFech1),
                doorsOnLeft ? FaceKind.ModuleRight : FaceKind.ModuleLeft,
                "Fechamento frontal");
        }
        else
        {
            Box(instance, World, id,
                new Vector3(xFech0, ffY0, zFech0),
                new Vector3(xFech1, ffY1, zFech1),
                FaceKind.ModuleFront, "Fechamento frontal");
        }

        if (cr.FechamentoSuperior)
        {
            Box(instance, World, id,
                new Vector3(xFech0, h - ft - FrontGapMm, zFech0),
                new Vector3(xFech1, h - FrontGapMm, zFech1),
                FaceKind.ModuleFront, "Fechamento superior");
        }

        if (cr.FechamentoInferior)
        {
            Box(instance, World, id,
                new Vector3(xFech0, FrontGapMm, zFech0),
                new Vector3(xFech1, FrontGapMm + ft, zFech1),
                FaceKind.ModuleFront, "Fechamento inferior");
        }

        if (cr.FechamentoTraseiro)
        {
            float backZ0 = structure.BackPanelType == BoxBackPanelType.Pregado
                ? 0f
                : Math.Clamp(structure.BackRecessMm, -d * 0.5f, d - bt);
            Box(instance, World, id,
                new Vector3(xFech0, y0, backZ0),
                new Vector3(xFech1, y1, backZ0 + bt),
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
        public float FechamentoAvancoMm = 18f;
        public float AvancoFfSobreBaseMm = 18f;
        public float AvancoFfSobreSarrafoMm = 18f;
        public float AvancoFfSobreLateralMm = 18f;
        public float AvancoFfSobreFfParcialMm;
        public float RecuoFfMm;
        public float RecuoFfParcialMm;
        public float DimFfParcialMm;
        public float AvancoFfSobreDistMm = -12f;
        public float AvancoDistSobreFfMm;
        public float AvancoDistSobrePratMm;
        public float RecuoPrateleiraMm;
        public float AvancoPortaSobreFfMm = 27f;
        public float FechamentoDimMm;
        public float FechamentoRecuoMm = 80f;
        public bool FechamentoSuperior;
        public bool FechamentoInferior;
        public bool FechamentoTraseiro;
        public float WallSideOffsetMm = 30f;
        public float WallBackOffsetMm;
        public string SarrafoFrontalTipo = "Total";
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
            FechamentoRecuoMm = 80f,
            FechamentoTipoLateral = true,
            AvancoFfSobreBaseMm = 18f,
            AvancoFfSobreSarrafoMm = 18f,
            AvancoFfSobreLateralMm = 18f,
            AvancoFfSobreDistMm = -12f,
            AvancoPortaSobreFfMm = 27f,
            WallSideOffsetMm = 30f,
            SarrafoFrontalTipo = "Total"
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

        ReadSignedNum(box, "cr-affb", v => opts.AvancoFfSobreBaseMm = v);
        ReadSignedNum(box, "cr-affs", v => opts.AvancoFfSobreSarrafoMm = v);
        ReadSignedNum(box, "cr-affl", v => opts.AvancoFfSobreLateralMm = v);
        ReadSignedNum(box, "cr-affffp", v => opts.AvancoFfSobreFfParcialMm = v);
        ReadSignedNum(box, "cr-rff", v => opts.RecuoFfMm = v);
        ReadSignedNum(box, "cr-rffp", v => opts.RecuoFfParcialMm = v);
        ReadNum(box, "cr-dim-ffp", v => opts.DimFfParcialMm = v);
        ReadSignedNum(box, "cr-affd", v => opts.AvancoFfSobreDistMm = v);
        ReadSignedNum(box, "cr-adff", v => opts.AvancoDistSobreFfMm = v);
        ReadSignedNum(box, "cr-adp", v => opts.AvancoDistSobrePratMm = v);
        ReadSignedNum(box, "cr-rec-prat", v => opts.RecuoPrateleiraMm = v);
        ReadSignedNum(box, "cr-ava-por", v => opts.AvancoPortaSobreFfMm = v);
        ReadNum(box, "crf-dim-fro", v => opts.FechamentoDimMm = v);
        ReadSignedNum(box, "crf-recuo-fro", v => opts.FechamentoRecuoMm = v);
        ReadSignedNum(box, "crf-pos-lat", v => opts.FechamentoAvancoMm = v);
        ReadSignedNum(box, "cr-afa-lat", v => opts.WallSideOffsetMm = v);
        ReadSignedNum(box, "cr-afa-tra", v => opts.WallBackOffsetMm = v);

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
        if (box.InferiorNumeric.TryGetValue(key, out var v) && float.IsFinite(v) && v >= 0f)
            apply(v);
    }

    private static void ReadSignedNum(BoxAssemblySectionSettings box, string key, Action<float> apply)
    {
        if (box.InferiorNumeric.TryGetValue(key, out var v) && float.IsFinite(v))
            apply(v);
    }

    private static bool IsSim(BoxAssemblySectionSettings box, string key) =>
        box.InferiorChoice.TryGetValue(key, out var v) &&
        (v.Equals("Sim", StringComparison.OrdinalIgnoreCase)
         || v.Equals("Usar", StringComparison.OrdinalIgnoreCase));

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

    public static void BuildOblique(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings = null,
        bool includeDiagonalFront = true)
    {
        dimensionSettings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(dimensionSettings);
        var box = dimensionSettings.CozinhaInferiorBox;
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = rules?.Structure ?? new ModulationStructure();
        float nominalW = instance.Width;
        float h = instance.Height;
        float nominalD = instance.Depth;
        float t = Math.Clamp(structure.PanelThicknessMm > 0f ? structure.PanelThicknessMm : 18f, 12f, 30f);
        float fundoT = Math.Clamp(structure.BackThicknessMm > 0f ? structure.BackThicknessMm : 6f, 3f, 18f);
        float ft = Math.Clamp(structure.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm : definition.FrontThickness, 12f, 30f);

        float backRecess = structure.BackPanelType == BoxBackPanelType.Pregado
            ? 0f
            : structure.BackRecessMm;

        string tipo = box.InferiorChoice.TryGetValue("cl-tipo", out var tipoValue)
            ? tipoValue : "Travessas";
        bool useRails = !tipo.Equals("Sem travessas", StringComparison.OrdinalIgnoreCase);
        bool invertedRails = tipo.Contains("invertidas", StringComparison.OrdinalIgnoreCase);
        float railWidth = ReadCg(box, "cl-larg-trav", 88f, 20f, nominalW * 0.4f);
        float railDepth = ReadCg(box, "cl-prof-trav", 88f, 20f, nominalD * 0.4f);
        float backOverRail = ReadSignedCg(box, "cl-aftv", 8f, -MathF.Min(railWidth, railDepth), MathF.Min(railWidth, railDepth));
        float spacerDepth = ReadCg(box, "cl-prof-dist", 0f, 0f, MathF.Min(nominalW, nominalD) * 0.4f);
        float wallSide = ReadSignedCg(box, "cl-afa-lat", 0f, -nominalW * 0.25f, nominalW * 0.5f);
        float wallBack = ReadSignedCg(box, "cl-afa-tra", 0f, -nominalD * 0.25f, nominalD * 0.5f);
        float baseOverBack = ReadSignedCg(box, "cl-abt", 0f, -100f, 100f);
        float backOverBase = ReadSignedCg(box, "cl-atb", 0f, -100f, 100f);
        float fundoOverBack = ReadSignedCg(box, "cl-aft", 0f, -100f, 100f);
        bool baseWhole = !box.InferiorChoice.TryGetValue("cl-tipo-base", out var baseType)
                         || !baseType.Contains("Bipart", StringComparison.OrdinalIgnoreCase);
        bool shelfWhole = !box.InferiorChoice.TryGetValue("cl-tipo-tampo", out var shelfType)
                          || !shelfType.Contains("Bipart", StringComparison.OrdinalIgnoreCase);

        float w = MathF.Max(420f, nominalW - wallSide);
        float d = MathF.Max(420f, nominalD - wallBack);
        var localOffset = new Vector3(wallSide, 0f, wallBack);
        instance.GeometryEnvelopeLocalOffset = new Vector3(
            MathF.Min(0f, wallSide), 0f, MathF.Min(0f, wallBack));

        var id = instance.Id;
        Vector3 World(Vector3 p) =>
            ModulePlacementService.TransformLocalPoint(
                p + localOffset, instance.Position, instance.RotationYDegrees);

        // Planta pentagonal do canto oblíquo. As duas costas acompanham as
        // paredes; as duas laterais têm a profundidade normal da cozinha; uma
        // única aresta diagonal liga as extremidades frontais.
        float standardDepth = Math.Clamp(
            dimensionSettings.CozinhaInferiorDepthMm,
            250f,
            MathF.Max(250f, MathF.Min(w, d) - 100f));
        float rightFrontZ = Math.Clamp(standardDepth, t + 100f, d - t - 100f);
        float leftFrontX = Math.Clamp(standardDepth, t + 100f, w - t - 100f);
        float railThickness = Math.Clamp(t, 12f, MathF.Min(railWidth, railDepth) * 0.5f);
        float railOrigin = useRails ? 0f : backRecess;
        float railInner = useRails ? railOrigin + railThickness : backRecess + fundoT;
        float railOuterX = useRails ? railOrigin + railWidth : backRecess + fundoT;
        float railOuterZ = useRails ? railOrigin + railDepth : backRecess + fundoT;
        float fundoFront = backRecess + fundoT;
        float innerStart = useRails ? MathF.Max(fundoFront, railInner) : fundoFront;

        float afl = Math.Clamp(structure.BackAdvanceOverLateralMm, -t, t);
        float alf = Math.Clamp(structure.LateralAdvanceOverBackMm, -t, t);
        float afb = Math.Clamp(structure.BackAdvanceOverBaseMm, -t, t);
        float abf = Math.Clamp(structure.BaseAdvanceOverBackMm, -fundoT, fundoT);

        float baseInnerStart = MathF.Max(0f,
            innerStart - abf - baseOverBack + backOverBase);

        Vector2[] basePlan = BuildObliqueInternalPlan(
            w, d, rightFrontZ, leftFrontX, t, baseInnerStart, frontInset: 0f);

        float lateralY0 = MathF.Abs(structure.LateralBottomRecessMm) >= MathF.Abs(structure.LateralBaseOverlapMm)
            ? structure.LateralBottomRecessMm
            : structure.LateralBaseOverlapMm;

        float lateralGap = Math.Clamp(
            structure.LateralDepthGapMm,
            -MathF.Min(rightFrontZ, leftFrontX) * 0.5f,
            MathF.Max(0f, MathF.Min(rightFrontZ, leftFrontX) - 10f));
        float rightZ0 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Back => lateralGap,
            LateralDepthAlignment.Center => lateralGap * 0.5f,
            _ => 0f
        };
        float rightZ1 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Front => rightFrontZ - lateralGap,
            LateralDepthAlignment.Center => rightFrontZ - lateralGap * 0.5f,
            _ => rightFrontZ
        };
        float leftX0 = rightZ0;
        float leftX1 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Front => leftFrontX - lateralGap,
            LateralDepthAlignment.Center => leftFrontX - lateralGap * 0.5f,
            _ => leftFrontX
        };

        // Laterais perpendiculares às duas paredes.
        Box(instance, World, id,
            new Vector3(w - t, lateralY0, rightZ0),
            new Vector3(w, h, rightZ1),
            FaceKind.ModuleRight, "Lateral dir.");
        Box(instance, World, id,
            new Vector3(leftX0, lateralY0, d - t),
            new Vector3(leftX1, h, d),
            FaceKind.ModuleLeft, "Lateral esq.");

        // Mesma montagem traseira validada do Canto L: duas travessas finas
        // encaixadas, dois fundos e todos os avanços do configurador.
        AddObliqueBackAssembly(
            instance, World, id,
            useRails, invertedRails,
            railOrigin, backRecess, fundoT, railWidth, railDepth, backOverRail,
            afl, alf, afb, fundoOverBack,
            w, d, t, h);

        if (baseWhole)
            AddPlanPrism(instance, World, id, basePlan, 0f, t, FaceKind.ModuleBottom, "Base oblíqua inteira");
        else
        {
            var middle = (basePlan[2] + basePlan[3]) * 0.5f;
            AddPlanPrism(instance, World, id,
                [basePlan[0], basePlan[1], basePlan[2], middle],
                0f, t, FaceKind.ModuleBottom, "Base oblíqua A");
            AddPlanPrism(instance, World, id,
                [basePlan[0], middle, basePlan[3], basePlan[4]],
                0f, t, FaceKind.ModuleBottom, "Base oblíqua B");
        }

        float shelfY = Math.Clamp(h * 0.5f - t * 0.5f, 2f * t, h - 3f * t);
        float shelfInset = structure.Shelves is { Count: > 0 }
            ? MathF.Max(2f, structure.Shelves[0].DepthInsetMm)
            : 2f;
        Vector2[] shelfPlan = BuildObliqueInternalPlan(
            w, d, rightFrontZ, leftFrontX, t, innerStart, shelfInset);
        if (shelfWhole)
            AddPlanPrism(instance, World, id, shelfPlan, shelfY, shelfY + t, FaceKind.ModuleTop, "Prateleira oblíqua inteira");
        else
        {
            var middle = (shelfPlan[2] + shelfPlan[3]) * 0.5f;
            AddPlanPrism(instance, World, id,
                [shelfPlan[0], shelfPlan[1], shelfPlan[2], middle],
                shelfY, shelfY + t, FaceKind.ModuleTop, "Prateleira oblíqua A");
            AddPlanPrism(instance, World, id,
                [shelfPlan[0], middle, shelfPlan[3], shelfPlan[4]],
                shelfY, shelfY + t, FaceKind.ModuleTop, "Prateleira oblíqua B");
        }

        if (spacerDepth > 0f)
        {
            Box(instance, World, id,
                new Vector3(w - 2f * t, t, rightFrontZ - spacerDepth),
                new Vector3(w - t, h - t, rightFrontZ),
                FaceKind.ModuleRight, "Distanciador oblíquo A");
            Box(instance, World, id,
                new Vector3(leftFrontX - spacerDepth, t, d - 2f * t),
                new Vector3(leftFrontX, h - t, d - t),
                FaceKind.ModuleLeft, "Distanciador oblíquo B");
        }

        // Plano frontal útil fica ENTRE as faces internas das laterais.
        // Sarrafo e portas compartilham esta mesma diagonal paramétrica.
        Vector2 frontStart = new(w - t, rightFrontZ);
        Vector2 frontEnd = new(leftFrontX, d - t);
        Vector2 interiorReference = new(innerStart, innerStart);

        float sFro = Math.Clamp(
            structure.SarrafoHeightMm > 0f ? structure.SarrafoHeightMm : 80f,
            10f, MathF.Min(MathF.Min(rightFrontZ, leftFrontX) * 0.55f, h * 0.5f));
        float sTra = Math.Clamp(
            structure.SarrafoTraseiroHeightMm > 0f ? structure.SarrafoTraseiroHeightMm : sFro,
            10f, MathF.Min(MathF.Min(rightFrontZ, leftFrontX) * 0.55f, h * 0.5f));
        float sarrafoT = Math.Clamp(
            structure.SarrafoThicknessMm > 0f ? structure.SarrafoThicknessMm : t,
            6f, MathF.Min(t, 50f));
        bool showSarrafos = structure.SarrafoVisible;
        bool showFront = showSarrafos && structure.FrontSarrafoVisible;
        bool showBack = showSarrafos && structure.BackSarrafoVisible;

        AddObliqueRearSarrafos(
            instance, World, id,
            w, d, h, t, fundoFront,
            railOuterX, railOuterZ, useRails,
            sTra, sarrafoT, showBack, structure.BackSarrafoIsVertical);

        // Sarrafo/travessa frontal usa exatamente a mesma linha do chanfro.
        if (showFront || (showSarrafos && structure.SarrafoWhole))
        {
            float recuoFro = Math.Clamp(
                structure.SarrafoDianteiroRecessMm,
                -MathF.Min(rightFrontZ, leftFrontX) * 0.25f,
                MathF.Min(rightFrontZ, leftFrontX) * 0.25f);
            var (sarStart, sarEnd) = InsetDiagonalEndpoints(
                frontStart,
                frontEnd,
                interiorReference,
                recuoFro);
            AddMiteredDiagonalStripPrism(instance, World, id,
                sarStart, sarEnd, interiorReference, sFro,
                h - sarrafoT, h, FaceKind.ModuleTop, "Sarrafo frontal oblíquo");
        }

        if (includeDiagonalFront)
        {
            int doors = Math.Clamp(instance.ObliqueDoorCount, 1, 2);
            float gapA = ReadSignedCg(box, "cl-folga-pa", 2f, -20f, 20f);
            float gapB = ReadSignedCg(box, "cl-folga-pb", 2f, -20f, 20f);
            float gapBetweenDoors = ReadSignedCg(box, "cl-folga-entre", 2f, -20f, 20f);

            // G/H são medidos no VÃO INTERNO entre as laterais. Depois de
            // descontar as folgas, a porta inteira é apenas transladada para
            // a frente do sarrafo; sua largura não pode aumentar ao mudar de plano.
            Vector2 direction = Normalize2(frontEnd - frontStart);
            Vector2 usableBackStart = frontStart + direction * gapA;
            Vector2 usableBackEnd = frontEnd - direction * gapB;
            Vector2 inward = ResolveInwardNormal(
                usableBackStart, usableBackEnd, interiorReference);
            Vector2 usableStart = usableBackStart - inward * ft;
            Vector2 usableEnd = usableBackEnd - inward * ft;

            if (doors == 1)
            {
                AddSegmentStripPrism(instance, World, id,
                    usableStart, usableEnd, interiorReference, ft,
                    FrontGapMm, h - FrontGapMm, FaceKind.ModuleFront,
                    instance.ObliqueHingesOnLeft ? "Porta — abertura esquerda" : "Porta — abertura direita",
                    outward: false);
            }
            else
            {
                Vector2 middle = (usableStart + usableEnd) * 0.5f;
                float centerHalfGap = gapBetweenDoors * 0.5f;
                AddSegmentStripPrism(instance, World, id,
                    usableStart, middle - direction * centerHalfGap, interiorReference, ft,
                    FrontGapMm, h - FrontGapMm, FaceKind.ModuleFront, "Porta 1", outward: false);
                AddSegmentStripPrism(instance, World, id,
                    middle + direction * centerHalfGap, usableEnd, interiorReference, ft,
                    FrontGapMm, h - FrontGapMm, FaceKind.ModuleFront, "Porta 2", outward: false);
            }
        }
    }

    private static Vector2[] BuildObliqueInternalPlan(
        float width,
        float depth,
        float rightFrontZ,
        float leftFrontX,
        float thickness,
        float innerStart,
        float frontInset)
    {
        var frontStart = new Vector2(width - thickness, rightFrontZ);
        var frontEnd = new Vector2(leftFrontX, depth - thickness);
        var interior = new Vector2(innerStart, innerStart);
        (frontStart, frontEnd) = InsetDiagonalEndpoints(
            frontStart, frontEnd, interior, MathF.Max(0f, frontInset));

        return
        [
            new(innerStart, innerStart),
            new(width - thickness, innerStart),
            frontStart,
            frontEnd,
            new(innerStart, depth - thickness)
        ];
    }

    private static (Vector2 Start, Vector2 End) InsetDiagonalEndpoints(
        Vector2 start,
        Vector2 end,
        Vector2 interiorReference,
        float inset)
    {
        if (MathF.Abs(inset) < 0.001f)
            return (start, end);

        Vector2 direction = Normalize2(end - start);
        Vector2 inward = new(-direction.Y, direction.X);
        Vector2 midpoint = (start + end) * 0.5f;
        if (Vector2.Dot(inward, interiorReference - midpoint) < 0f)
            inward = -inward;

        Vector2 shiftedLinePoint = start + inward * inset;
        Vector2 shiftedDirection = end - start;
        Vector2 shiftedStart = shiftedLinePoint;
        Vector2 shiftedEnd = end + inward * inset;

        // Mantém a linha diagonal paralela e recalcula suas interseções com
        // as faces internas das duas laterais, sem deixar pontas soltas.
        if (MathF.Abs(shiftedDirection.X) > 0.001f)
        {
            float u = (start.X - shiftedLinePoint.X) / shiftedDirection.X;
            shiftedStart = new Vector2(
                start.X,
                shiftedLinePoint.Y + shiftedDirection.Y * u);
        }

        if (MathF.Abs(shiftedDirection.Y) > 0.001f)
        {
            float u = (end.Y - shiftedLinePoint.Y) / shiftedDirection.Y;
            shiftedEnd = new Vector2(
                shiftedLinePoint.X + shiftedDirection.X * u,
                end.Y);
        }

        return (shiftedStart, shiftedEnd);
    }

    /// <summary>
    /// Sarrafo diagonal com as duas pontas chanfradas pelas faces internas
    /// das laterais. As arestas externa e interna sempre chegam às laterais,
    /// inclusive quando largura, profundidade ou ângulo são alterados.
    /// </summary>
    private static void AddMiteredDiagonalStripPrism(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Guid id,
        Vector2 start,
        Vector2 end,
        Vector2 interiorReference,
        float depth,
        float y0,
        float y1,
        FaceKind kind,
        string label)
    {
        if ((end - start).Length < 1f || depth <= 0f)
            return;

        var (innerStart, innerEnd) = InsetDiagonalEndpoints(
            start, end, interiorReference, depth);
        AddPlanPrism(
            instance,
            world,
            id,
            [start, end, innerEnd, innerStart],
            y0,
            y1,
            kind,
            label);
    }

    /// <summary>
    /// Montagem traseira idêntica ao Canto L: duas travessas finas em L e
    /// fundos independentes, consumindo os mesmos avanços do configurador.
    /// </summary>
    private static void AddObliqueBackAssembly(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        bool useRails,
        bool invertedRails,
        float railOrigin,
        float backRecess,
        float backThickness,
        float railWidth,
        float railDepth,
        float backOverRail,
        float backOverLateral,
        float lateralOverBack,
        float backOverBase,
        float backOverRear,
        float width,
        float depth,
        float panelThickness,
        float height)
    {
        float y0 = MathF.Max(0f, panelThickness - backOverBase);
        float rightEnd = Math.Clamp(
            width - panelThickness + backOverLateral - lateralOverBack,
            width * 0.5f,
            width);
        float leftEnd = Math.Clamp(
            depth - panelThickness + backOverLateral - lateralOverBack,
            depth * 0.5f,
            depth);

        if (!useRails)
        {
            float backFront = MathF.Max(
                backRecess,
                backRecess + backThickness - MathF.Max(0f, backOverRear));
            Box(instance, toWorld, id,
                new Vector3(backFront, y0, backRecess),
                new Vector3(rightEnd, height, backFront),
                FaceKind.ModuleBack, "Fundo dir.");
            Box(instance, toWorld, id,
                new Vector3(backRecess, y0, backFront),
                new Vector3(backFront, height, leftEnd),
                FaceKind.ModuleBack, "Fundo esq.");
            return;
        }

        float e = Math.Clamp(panelThickness, 12f, MathF.Min(railWidth, railDepth) * 0.5f);
        float outer = railOrigin;
        float innerX = railOrigin + railWidth;
        float innerZ = railOrigin + railDepth;

        if (!invertedRails)
        {
            Box(instance, toWorld, id,
                new Vector3(outer, 0f, innerZ - e),
                new Vector3(innerX, height, innerZ),
                FaceKind.ModuleBack, "Travessa canto esq.");
            Box(instance, toWorld, id,
                new Vector3(innerX - e, 0f, outer),
                new Vector3(innerX, height, innerZ - e),
                FaceKind.ModuleBack, "Travessa canto dir.");
        }
        else
        {
            Box(instance, toWorld, id,
                new Vector3(innerX - e, 0f, outer),
                new Vector3(innerX, height, innerZ),
                FaceKind.ModuleBack, "Travessa canto dir.");
            Box(instance, toWorld, id,
                new Vector3(outer, 0f, innerZ - e),
                new Vector3(innerX - e, height, innerZ),
                FaceKind.ModuleBack, "Travessa canto esq.");
        }

        float rightBackStart = railOrigin + railWidth - backOverRail;
        Box(instance, toWorld, id,
            new Vector3(rightBackStart, y0, backRecess),
            new Vector3(rightEnd, height, backRecess + backThickness),
            FaceKind.ModuleBack, "Fundo dir.");

        float leftBackStart = railOrigin + railDepth - backOverRail;
        Box(instance, toWorld, id,
            new Vector3(backRecess, y0, leftBackStart),
            new Vector3(backRecess + backThickness, height, leftEnd),
            FaceKind.ModuleBack, "Fundo esq.");
    }

    private static void AddObliqueRearSarrafos(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        float width,
        float depth,
        float height,
        float panelThickness,
        float backFront,
        float railOuterX,
        float railOuterZ,
        bool useRails,
        float sarrafoHeight,
        float sarrafoThickness,
        bool visible,
        bool vertical)
    {
        if (!visible)
            return;

        float startX = useRails ? railOuterX : backFront;
        float startZ = useRails ? railOuterZ : backFront;
        if (!vertical)
        {
            Box(instance, toWorld, id,
                new Vector3(startX, height - sarrafoThickness, 0f),
                new Vector3(width - panelThickness, height, sarrafoHeight),
                FaceKind.ModuleTop, "Sarrafo traseiro dir.");
            Box(instance, toWorld, id,
                new Vector3(0f, height - sarrafoThickness, startZ),
                new Vector3(sarrafoHeight, height, depth - panelThickness),
                FaceKind.ModuleTop, "Sarrafo traseiro esq.");
        }
        else
        {
            Box(instance, toWorld, id,
                new Vector3(startX, height - sarrafoHeight, 0f),
                new Vector3(width - panelThickness, height, sarrafoThickness),
                FaceKind.ModuleTop, "Sarrafo traseiro dir.");
            Box(instance, toWorld, id,
                new Vector3(0f, height - sarrafoHeight, startZ),
                new Vector3(sarrafoThickness, height, depth - panelThickness),
                FaceKind.ModuleTop, "Sarrafo traseiro esq.");
        }
    }

    public static void BuildCurvedCorner(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        dimensionSettings ??= DimensionConfiguratorSettings.CreateDefault();
        BuildOblique(instance, definition, dimensionSettings, includeDiagonalFront: false);

        BoxAssemblyConfiguratorService.EnsureBoxInitialized(dimensionSettings);
        var box = dimensionSettings.CozinhaInferiorBox;
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = rules?.Structure ?? new ModulationStructure();
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float ft = Math.Clamp(structure.FrontThicknessMm, 12f, 30f);
        float cut = MathF.Min(w, d) * 0.38f;
        float sideGap = Math.Clamp(structure.FrontSideGapMm, -w * 0.25f, w * 0.25f);
        float y0 = Math.Clamp(structure.FrontBottomGapMm, -h * 0.25f, h - 2f);
        float y1 = h - Math.Clamp(structure.FrontTopGapMm, -h * 0.25f, h - y0 - 1f);
        float wallSide = ReadSignedCg(box, "cl-afa-lat", 0f, -w * 0.25f, w * 0.25f);
        float wallBack = ReadSignedCg(box, "cl-afa-tra", 0f, -d * 0.25f, d * 0.25f);
        int segments = 14;
        var id = instance.Id;
        Vector3 World(Vector3 p) => ModulePlacementService.TransformLocalPoint(
            p + new Vector3(wallSide, 0f, wallBack), instance.Position, instance.RotationYDegrees);

        float rx = MathF.Max(20f, w * 0.5f - cut - sideGap);
        float rz = MathF.Max(20f, cut - sideGap);
        float cx = w * 0.5f;
        float cz = d - cut;
        for (int i = 0; i < segments; i++)
        {
            float a0 = MathF.PI - MathF.PI * i / segments;
            float a1 = MathF.PI - MathF.PI * (i + 1) / segments;
            float x0 = cx + MathF.Cos(a0) * rx;
            float x1 = cx + MathF.Cos(a1) * rx;
            float z0 = cz + MathF.Sin(a0) * rz;
            float z1 = cz + MathF.Sin(a1) * rz;
            float minX = MathF.Min(x0, x1);
            float maxX = MathF.Max(x0, x1);
            float minZ = MathF.Min(z0, z1);
            float maxZ = MathF.Max(z0, z1) + ft;
            if (maxX - minX < 3f)
                maxX = minX + 3f;
            Box(instance, World, id, new Vector3(minX, y0, minZ),
                new Vector3(maxX, y1, maxZ), FaceKind.ModuleFront, "Porta curva");
        }
    }

    private static void AddPlanPrism(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Guid id,
        IReadOnlyList<Vector2> plan,
        float y0,
        float y1,
        FaceKind kind,
        string label)
    {
        if (plan.Count < 3 || y1 <= y0)
            return;

        var mesh = instance.Mesh;
        Vector3 P(int i, float y) => world(new Vector3(plan[i].X, y, plan[i].Y));

        var bottomLoop = new Vector3[plan.Count];
        var topLoop = new Vector3[plan.Count];
        for (int i = 0; i < plan.Count; i++)
        {
            bottomLoop[i] = P(i, y0);
            topLoop[i] = P(i, y1);
        }

        var bottomTriangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        var topTriangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        for (int i = 1; i < plan.Count - 1; i++)
        {
            bottomTriangles.Add((P(0, y0), P(i + 1, y0), P(i, y0)));
            topTriangles.Add((P(0, y1), P(i, y1), P(i + 1, y1)));
        }

        // Uma SelectableFace por superfície: a triangulação continua sendo
        // usada no preenchimento, mas o contorno desenha somente o perímetro.
        mesh.AddPolygonalFace(bottomLoop, bottomTriangles, kind, id, label);
        mesh.AddPolygonalFace(topLoop, topTriangles, kind, id, label);

        for (int i = 0; i < plan.Count; i++)
        {
            int next = (i + 1) % plan.Count;
            mesh.AddQuad(P(i, y0), P(next, y0), P(next, y1), P(i, y1), kind, id, label);
        }
    }

    private static Vector2 Normalize2(Vector2 value)
    {
        float length = value.Length;
        return length > 0.001f ? value / length : Vector2.UnitX;
    }

    private static Vector2 ResolveInwardNormal(
        Vector2 start,
        Vector2 end,
        Vector2 interiorReference)
    {
        Vector2 direction = Normalize2(end - start);
        Vector2 inward = new(-direction.Y, direction.X);
        Vector2 midpoint = (start + end) * 0.5f;
        return Vector2.Dot(inward, interiorReference - midpoint) < 0f
            ? -inward
            : inward;
    }

    /// <summary>
    /// Cria uma chapa prismática alinhada a um segmento da planta. O lado
    /// interno é identificado por um ponto de referência, permitindo usar a
    /// mesma aresta para sarrafo (para dentro) e porta (para fora).
    /// </summary>
    private static void AddSegmentStripPrism(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Guid id,
        Vector2 start,
        Vector2 end,
        Vector2 interiorReference,
        float depth,
        float y0,
        float y1,
        FaceKind kind,
        string label,
        bool outward)
    {
        Vector2 direction = Normalize2(end - start);
        if ((end - start).Length < 1f || depth <= 0f)
            return;

        Vector2 inward = ResolveInwardNormal(start, end, interiorReference);

        Vector2 normal = outward ? -inward : inward;
        Vector2[] strip =
        [
            start,
            end,
            end + normal * depth,
            start + normal * depth
        ];
        AddPlanPrism(instance, world, id, strip, y0, y1, kind, label);
    }

    /// <summary>
    /// Canto Gaveteiro inferior. As cinco medidas do nó Promob cg-* controlam
    /// travessas frontais, sarrafos de sustentação, travessas de fundo e afastamento.
    /// </summary>
    public static void BuildCornerDrawer(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        dimensionSettings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(dimensionSettings);
        var box = dimensionSettings.CozinhaInferiorBox;
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = rules?.Structure ?? new ModulationStructure();

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = Math.Clamp(structure.PanelThicknessMm, 12f, 30f);
        float ft = Math.Clamp(structure.FrontThicknessMm, 12f, 30f);
        float frontRail = ReadCg(box, "cg-dim-trav-fro", 50f, 12f, h * 0.25f);
        float supportWidth = ReadCg(box, "cg-larg-sar-sust", 70f, 12f, w * 0.35f);
        float backRailWidth = ReadCg(box, "cg-larg-trav-fun", 160f, 20f, w * 0.7f);
        float backRailDepth = ReadCg(box, "cg-prof-trav-fun", 70f, 12f, d * 0.35f);
        float wallOffset = ReadSignedCg(box, "cg-afa", 0f, -MathF.Min(w, d) * 0.25f, MathF.Min(w, d) * 0.25f);
        float cut = Math.Clamp(MathF.Min(w, d) * 0.42f, 220f, MathF.Min(w, d) * 0.65f);
        int drawers = Math.Max(1, definition.DrawerCount);
        var id = instance.Id;

        instance.GeometryEnvelopeLocalOffset = new Vector3(wallOffset, 0f, wallOffset);

        Vector3 World(Vector3 p) => ModulePlacementService.TransformLocalPoint(
            p + new Vector3(wallOffset, 0f, wallOffset),
            instance.Position, instance.RotationYDegrees);

        float lateralGap = Math.Clamp(structure.LateralDepthGapMm, -(d - cut) * 0.5f, MathF.Max(0f, d - cut - 10f));
        float lateralZ0 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Back => lateralGap,
            LateralDepthAlignment.Center => lateralGap * 0.5f,
            _ => 0f
        };
        float lateralZ1 = structure.LateralDepthAlignment switch
        {
            LateralDepthAlignment.Front => d - cut - lateralGap,
            LateralDepthAlignment.Center => d - cut - lateralGap * 0.5f,
            _ => d - cut
        };
        float lateralY0 = MathF.Abs(structure.LateralBottomRecessMm) >= MathF.Abs(structure.LateralBaseOverlapMm)
            ? structure.LateralBottomRecessMm
            : structure.LateralBaseOverlapMm;

        // Laterais e base bipartida formam o envelope em canto com frente diagonal.
        Box(instance, World, id, new Vector3(0f, lateralY0, lateralZ0), new Vector3(t, h, lateralZ1),
            FaceKind.ModuleLeft, "Lateral canto gaveteiro A");
        Box(instance, World, id, new Vector3(w - t, lateralY0, lateralZ0), new Vector3(w, h, lateralZ1),
            FaceKind.ModuleRight, "Lateral canto gaveteiro B");
        Box(instance, World, id, new Vector3(t, 0f, 0f), new Vector3(w - t, t, d - cut),
            FaceKind.ModuleBottom, "Base canto gaveteiro A");
        Box(instance, World, id, new Vector3(cut * 0.2f, 0f, d - cut),
            new Vector3(w - cut * 0.2f, t, d), FaceKind.ModuleBottom, "Base canto gaveteiro B");

        // Conjunto traseiro: a largura e a profundidade são independentes.
        float halfBack = backRailWidth * 0.5f;
        Box(instance, World, id, new Vector3(t, t, 0f),
            new Vector3(MathF.Min(w - t, t + halfBack), h - t, backRailDepth),
            FaceKind.ModuleBack, "Travessa fundo A");
        Box(instance, World, id, new Vector3(MathF.Max(t, w - t - halfBack), t, 0f),
            new Vector3(w - t, h - t, backRailDepth),
            FaceKind.ModuleBack, "Travessa fundo B");

        // Sarrafos sustentam as corrediças em duas linhas da caixa.
        float supportDepth = MathF.Max(40f, d - cut - backRailDepth);
        float supportX1 = MathF.Max(t, w * 0.32f - supportWidth * 0.5f);
        float supportX2 = MathF.Min(w - t - supportWidth, w * 0.68f - supportWidth * 0.5f);
        Box(instance, World, id, new Vector3(supportX1, t, backRailDepth),
            new Vector3(supportX1 + supportWidth, h - t, backRailDepth + supportDepth),
            FaceKind.ModuleRight, "Sarrafo sustentação A");
        Box(instance, World, id, new Vector3(supportX2, t, backRailDepth),
            new Vector3(supportX2 + supportWidth, h - t, backRailDepth + supportDepth),
            FaceKind.ModuleLeft, "Sarrafo sustentação B");

        float clearHeight = MathF.Max(30f, h - 2f * t);
        float drawerHeight = clearHeight / drawers;
        for (int i = 0; i < drawers; i++)
        {
            float y0 = t + i * drawerHeight + FrontGapMm;
            float y1 = t + (i + 1) * drawerHeight - FrontGapMm;
            AddSteppedDiagonal(instance, World, id, cut, w, d, y0, y1, ft,
                drawers == 1 ? "Frente gaveta" : $"Frente gaveta {i + 1}");

            if (i < drawers - 1)
            {
                float railY0 = t + (i + 1) * drawerHeight - frontRail * 0.5f;
                AddSteppedDiagonal(instance, World, id, cut, w, d, railY0,
                    railY0 + frontRail, t, $"Travessa frontal {i + 1}");
            }
        }
    }

    private static float ReadCg(
        BoxAssemblySectionSettings box,
        string key,
        float fallback,
        float min,
        float max) =>
        box.InferiorNumeric.TryGetValue(key, out var value) && value > 0f
            ? Math.Clamp(value, min, MathF.Max(min, max))
            : Math.Clamp(fallback, min, MathF.Max(min, max));

    private static float ReadSignedCg(
        BoxAssemblySectionSettings box,
        string key,
        float fallback,
        float min,
        float max) =>
        box.InferiorNumeric.TryGetValue(key, out var value) && float.IsFinite(value)
            ? Math.Clamp(value, min, MathF.Max(min, max))
            : Math.Clamp(fallback, min, MathF.Max(min, max));

    private static void AddSteppedDiagonal(
        ModuleInstance instance,
        Func<Vector3, Vector3> world,
        Guid id,
        float cut,
        float width,
        float depth,
        float y0,
        float y1,
        float thickness,
        string label)
    {
        const int steps = 8;
        for (int i = 0; i < steps; i++)
        {
            float u0 = (float)i / steps;
            float u1 = (float)(i + 1) / steps;
            float x0 = cut + (width - 2f * cut) * u0;
            float x1 = cut + (width - 2f * cut) * u1;
            float z0 = depth - cut + cut * u0;
            float z1 = depth - cut + cut * u1;
            float minX = MathF.Min(x0, x1);
            float maxX = MathF.Max(x0, x1);
            float minZ = MathF.Min(z0, z1);
            float maxZ = MathF.Max(z0, z1) + thickness;
            if (maxX - minX < 3f)
                maxX = minX + 3f;
            Box(instance, world, id, new Vector3(minX, y0, minZ),
                new Vector3(maxX, y1, maxZ), FaceKind.ModuleFront, label);
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
