using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Construtor paramétrico do Canto L 2P — peças independentes, sem Scale.
/// Contorno externo (planta): (0,0)-(Cd,0)-(Cd,Pd)-(Pe,Pd)-(Pe,Ce)-(0,Ce).
/// Nó Canto (Configurador): Tipo Travessas + largura/profundidade + avanço do fundo.
/// </summary>
public static class CornerLModuleBuilder
{
    private const float ShelfFrontSetbackMm = 2f;

    public static void Rebuild(
        ModuleInstance instance,
        ModuleDefinition definition,
        CornerLParams parameters,
        DimensionConfiguratorSettings? dimensionSettings = null)
    {
        instance.Mesh.Clear();

        var p = parameters.Clone();
        var rules = DimensionConfiguratorService.CreateEffectiveRules(definition, dimensionSettings);
        var structure = rules?.Structure;

        if (structure != null)
        {
            if (structure.PanelThicknessMm > 0f)
                p.EspessuraMdf = structure.PanelThicknessMm;
            if (structure.SarrafoHeightMm > 0f)
                p.AlturaSarrafo = structure.SarrafoHeightMm;
        }

        float frontThickness = structure?.FrontThicknessMm > 0f
            ? structure.FrontThicknessMm
            : (definition.FrontThickness > 0f ? definition.FrontThickness : p.EspessuraMdf);

        float fundoT = structure?.BackThicknessMm > 0f ? structure.BackThicknessMm : 6f;
        float backRecess = 0f;
        if (structure != null && structure.BackPanelType != BoxBackPanelType.Pregado)
            backRecess = MathF.Max(0f, structure.BackRecessMm);

        var canto = ResolveCantoLOptions(dimensionSettings);
        // Folgas internas A/B do configurador (mantém média em FolgaPortas para compat).
        p.FolgaPortas = MathF.Max(0.5f, (canto.FolgaPortaA + canto.FolgaPortaB) * 0.5f);

        float sFro = structure?.SarrafoHeightMm > 0f ? structure.SarrafoHeightMm : p.AlturaSarrafo;
        float sTra = structure?.SarrafoTraseiroHeightMm > 0f ? structure.SarrafoTraseiroHeightMm : sFro;
        float sarT = structure?.SarrafoThicknessMm > 0f ? structure.SarrafoThicknessMm : p.EspessuraMdf;
        float recuoFro = structure != null ? MathF.Max(0f, structure.SarrafoDianteiroRecessMm) : 0f;
        bool showSarrafos = structure == null || structure.SarrafoVisible;
        bool showFro = showSarrafos;
        bool showTra = showSarrafos;
        bool froVert = structure?.FrontSarrafoIsVertical == true;
        bool traVert = structure?.BackSarrafoIsVertical == true;

        p.Validate();
        p.AlturaSarrafo = sFro;

        var (ce, cd, pe, pd) = p.EffectiveSides();
        float h = p.Altura;
        float t = p.EspessuraMdf;
        int doorCount = Math.Max(1, definition.DoorCount);
        var id = instance.Id;

        fundoT = Math.Clamp(fundoT, 3f, MathF.Min(t, MathF.Min(pe, pd) * 0.4f));
        backRecess = Math.Clamp(backRecess, 0f, MathF.Min(pe, pd) * 0.35f);
        float largTrav = Math.Clamp(canto.LarguraTravMm, 20f, MathF.Min(pe, pd) * 0.85f);
        float profTrav = Math.Clamp(canto.ProfundidadeTravMm, 20f, MathF.Min(pe, pd) * 0.85f);
        float aftv = Math.Clamp(canto.AvancoFundoSobreTravMm, 0f, MathF.Min(largTrav, profTrav) * 0.9f);
        bool useTravessas = canto.Tipo is CantoLTipo.Travessas or CantoLTipo.TravessasInvertidas;
        bool invertidas = canto.Tipo == CantoLTipo.TravessasInvertidas;

        float travEsp = Math.Clamp(t, 12f, MathF.Min(largTrav, profTrav) * 0.5f);
        // Travessas alinhadas à traseira das laterais (X/Z = 0), não ao recuo do fundo.
        // Envelope = 0…larg / 0…prof — sarrafos começam após o envelope, no plano da parede (atrás do fundo).
        float travOrigin = useTravessas ? 0f : backRecess;
        float travInner = useTravessas ? travOrigin + travEsp : backRecess + fundoT;
        float travOuterX = useTravessas ? travOrigin + largTrav : backRecess + fundoT;
        float travOuterZ = useTravessas ? travOrigin + profTrav : backRecess + fundoT;
        float fundoFront = backRecess + fundoT;
        float innerStart = useTravessas ? MathF.Max(fundoFront, travInner) : fundoFront;

        sFro = Math.Clamp(sFro, 10f, MathF.Min(MathF.Min(pe, pd) * 0.55f, h * 0.5f));
        sTra = Math.Clamp(sTra, 10f, MathF.Min(MathF.Min(pe, pd) * 0.55f, h * 0.5f));
        sarT = Math.Clamp(sarT, 6f, MathF.Min(t, 50f));
        recuoFro = Math.Clamp(recuoFro, 0f, MathF.Min(pe, pd) * 0.25f);

        instance.Width = cd;
        instance.Depth = ce;
        instance.Height = h;

        Vector3 World(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        _ = BuildLContour(cd, ce, pd, pe);

        AddBox(instance, World, id,
            new Vector3(cd - t, 0f, 0f),
            new Vector3(cd, h, pd),
            FaceKind.ModuleRight, "Lateral dir.");
        AddBox(instance, World, id,
            new Vector3(0f, 0f, ce - t),
            new Vector3(pe, h, ce),
            FaceKind.ModuleLeft, "Lateral esq.");

        // Mesmos campos do configurador Inferior (Fixação Fundo-Lateral / Base-Fundo).
        float afl = structure != null
            ? Math.Clamp(structure.BackAdvanceOverLateralMm, 0f, t)
            : 0f;
        float alf = structure != null
            ? Math.Clamp(structure.LateralAdvanceOverBackMm, 0f, t)
            : 0f;
        float afb = structure != null
            ? Math.Clamp(structure.BackAdvanceOverBaseMm, 0f, t)
            : 0f;
        float abf = structure != null
            ? Math.Clamp(structure.BaseAdvanceOverBackMm, 0f, fundoT)
            : 0f;

        AddCornerBackAssembly(
            instance, World, id,
            useTravessas, invertidas,
            travOrigin, backRecess, fundoT, largTrav, profTrav, aftv,
            afl, alf, afb,
            cd, ce, t, h);

        // Base L: avanço sobre o fundo (abf) encurta o vão interno no canto.
        // Tipo Base Inteira = peça L contínua; Recortada = bipartida (com emenda).
        float baseInnerStart = MathF.Max(0f, innerStart - abf);
        var baseContour = BuildInternalLContour(cd, ce, pd, pe, t, baseInnerStart);
        ExtrudeInternalL(
            instance, World, id, baseContour, y0: 0f, y1: t,
            FaceKind.ModuleBottom, "Base L", wholePiece: canto.BaseInteira);

        float shelfY = Math.Clamp(h * 0.5f - t * 0.5f, t * 2f, h - t * 3f);
        float shelfInset = structure?.Shelves is { Count: > 0 }
            ? MathF.Max(ShelfFrontSetbackMm, structure.Shelves[0].DepthInsetMm)
            : ShelfFrontSetbackMm;
        var shelf = BuildInternalLContour(cd, ce, pd, pe, t, innerStart, shelfInset);
        // Tipo Tampo Inteiro = prateleira L contínua (Promob: peça única).
        ExtrudeInternalL(
            instance, World, id, shelf, y0: shelfY, y1: shelfY + t,
            FaceKind.ModuleTop, "Prateleira L", wholePiece: canto.TampoInteiro);

        AddSarrafos(
            instance, World, id, p.IsLeftHand,
            ce, cd, pe, pd, h, t, fundoFront,
            travOuterX, travOuterZ, useTravessas,
            sFro, sTra, sarT, recuoFro,
            showFro, showTra, froVert, traVert);

        float ft = Math.Clamp(frontThickness, 12f, 30f);
        AddCornerDoors(
            instance, World, id,
            doorCount, definition.ShapeKind,
            ce, cd, pe, pd, h, ft,
            canto.FolgaPortaA, canto.FolgaPortaB,
            canto.BordaLateralMm, canto.BordaInferiorMm, canto.BordaSuperiorMm);

        instance.CornerL = p;
    }

    /// <summary>
    /// Travessas de canto (Promob): L no canto com butt (cheia + secundária), lado parede.
    /// Face interior do L (r+e) = marca vermelha. Fundos no lado parede, avançam aftv.
    /// Travessas: esq. cheia. Invertidas: dir. cheia.
    /// </summary>
    private static void AddCornerBackAssembly(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        bool useTravessas,
        bool invertidas,
        float travOrigin,
        float fundoRecess,
        float fundoT,
        float larg,
        float prof,
        float aftv,
        float afl,
        float alf,
        float afb,
        float cd,
        float ce,
        float t,
        float h)
    {
        // Fundo assenta na base (y=t) e avança afb sobre ela; nas laterais: afl/alf.
        float y0 = MathF.Max(0f, t - afb);
        float latEndDir = Math.Clamp(cd - t + afl - alf, cd * 0.5f, cd);
        float latEndEsq = Math.Clamp(ce - t + afl - alf, ce * 0.5f, ce);

        if (!useTravessas)
        {
            float fundoFront = fundoRecess + fundoT;
            AddBox(instance, toWorld, id,
                new Vector3(fundoFront, y0, fundoRecess),
                new Vector3(latEndDir, h, fundoFront),
                FaceKind.ModuleBack, "Fundo dir.");
            AddBox(instance, toWorld, id,
                new Vector3(fundoRecess, y0, fundoFront),
                new Vector3(fundoFront, h, latEndEsq),
                FaceKind.ModuleBack, "Fundo esq.");
            return;
        }

        // Travessas alinhadas à traseira das laterais (travOrigin = 0).
        // L no bordo INTERNO do envelope — “L pra frente”, cada peça // à lateral.
        float e = Math.Clamp(t, 12f, MathF.Min(larg, prof) * 0.5f);
        float outer = travOrigin;
        float innerX = travOrigin + larg;
        float innerZ = travOrigin + prof;

        if (!invertidas)
        {
            AddBox(instance, toWorld, id,
                new Vector3(outer, 0f, innerZ - e),
                new Vector3(innerX, h, innerZ),
                FaceKind.ModuleBack, "Travessa canto esq.");
            AddBox(instance, toWorld, id,
                new Vector3(innerX - e, 0f, outer),
                new Vector3(innerX, h, innerZ - e),
                FaceKind.ModuleBack, "Travessa canto dir.");
        }
        else
        {
            AddBox(instance, toWorld, id,
                new Vector3(innerX - e, 0f, outer),
                new Vector3(innerX, h, innerZ),
                FaceKind.ModuleBack, "Travessa canto dir.");
            AddBox(instance, toWorld, id,
                new Vector3(outer, 0f, innerZ - e),
                new Vector3(innerX - e, h, innerZ),
                FaceKind.ModuleBack, "Travessa canto esq.");
        }

        // Fundos: aftv sobre travessa + ffl-afl/fbf-afb do configurador Inferior.
        float fundoDirX0 = travOrigin + larg - aftv;
        AddBox(instance, toWorld, id,
            new Vector3(fundoDirX0, y0, fundoRecess),
            new Vector3(latEndDir, h, fundoRecess + fundoT),
            FaceKind.ModuleBack, "Fundo dir.");

        float fundoEsqZ0 = travOrigin + prof - aftv;
        AddBox(instance, toWorld, id,
            new Vector3(fundoRecess, y0, fundoEsqZ0),
            new Vector3(fundoRecess + fundoT, h, latEndEsq),
            FaceKind.ModuleBack, "Fundo esq.");
    }

    private static CantoLOptions ResolveCantoLOptions(DimensionConfiguratorSettings? settings)
    {
        var opts = new CantoLOptions
        {
            Tipo = CantoLTipo.Travessas,
            LarguraTravMm = 88f,
            ProfundidadeTravMm = 88f,
            AvancoFundoSobreTravMm = 8f,
            FolgaPortaA = 2f,
            FolgaPortaB = 2f,
            BordaLateralMm = 4f,
            BordaInferiorMm = 4f,
            BordaSuperiorMm = 4f
        };

        settings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        FrentesPortasConfiguratorService.EnsureInitialized(settings);
        var box = settings.CozinhaInferiorBox;

        if (box.InferiorChoice.TryGetValue("cl-tipo", out var tipo))
        {
            opts.Tipo = tipo switch
            {
                "Sem travessas" => CantoLTipo.SemTravessas,
                "Travessas invertidas" => CantoLTipo.TravessasInvertidas,
                _ => CantoLTipo.Travessas
            };
        }

        if (box.InferiorNumeric.TryGetValue("cl-larg-trav", out var larg) && larg > 0f)
            opts.LarguraTravMm = larg;
        if (box.InferiorNumeric.TryGetValue("cl-prof-trav", out var pf) && pf > 0f)
            opts.ProfundidadeTravMm = pf;
        if (box.InferiorNumeric.TryGetValue("cl-aftv", out var aft) && aft >= 0f)
            opts.AvancoFundoSobreTravMm = aft;
        if (box.InferiorNumeric.TryGetValue("cl-folga-pa", out var fa) && fa >= 0f)
            opts.FolgaPortaA = fa;
        if (box.InferiorNumeric.TryGetValue("cl-folga-pb", out var fb) && fb >= 0f)
            opts.FolgaPortaB = fb;

        // Promob: Tipo Base / Tipo Tampo (prateleira) — única vs bipartida.
        if (box.InferiorChoice.TryGetValue("cl-tipo-base", out var tipoBase))
            opts.BaseInteira = !string.Equals(tipoBase, "Recortada", StringComparison.OrdinalIgnoreCase);
        if (box.InferiorChoice.TryGetValue("cl-tipo-tampo", out var tipoTampo))
            opts.TampoInteiro = !string.Equals(tipoTampo, "Recortado", StringComparison.OrdinalIgnoreCase);

        // Frentes | Portas → Inferiores (borda lateral / inferior / superior).
        var portas = settings.CozinhaFrentesPortas;
        opts.BordaLateralMm = ReadChoiceMm(portas, "inferiores", "borda-lat", opts.BordaLateralMm);
        opts.BordaInferiorMm = ReadChoiceMm(portas, "inferiores", "borda-inf", opts.BordaInferiorMm);
        opts.BordaSuperiorMm = ReadChoiceMm(portas, "inferiores", "borda-sup", opts.BordaSuperiorMm);

        return opts;
    }

    private static float ReadChoiceMm(
        CozinhaFrentesPortasSettings portas,
        string nodeId,
        string fieldKey,
        float fallback)
    {
        string key = FrentesPortasConfiguratorService.MakeKey(nodeId, fieldKey);
        if (portas.Choice.TryGetValue(key, out var text) &&
            float.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value))
            return MathF.Max(0f, value);

        return fallback;
    }

    /// <summary>
    /// Portas do Canto L 2P — peças individuais à frente da caixaria (paridade Promob).
    /// Porta dir. na face Z=Pd; Porta esq. na face X=Pe. Folgas A/B = canto interno.
    /// </summary>
    private static void AddCornerDoors(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        int doorCount,
        ModuleShapeKind shapeKind,
        float ce, float cd, float pe, float pd,
        float h, float ft,
        float folgaA, float folgaB,
        float bordaLat, float bordaInf, float bordaSup)
    {
        folgaA = Math.Clamp(folgaA, 0f, 20f);
        folgaB = Math.Clamp(folgaB, 0f, 20f);
        bordaLat = Math.Clamp(bordaLat, 0f, 20f);
        bordaInf = Math.Clamp(bordaInf, 0f, 30f);
        bordaSup = Math.Clamp(bordaSup, 0f, 30f);

        float y0 = bordaInf;
        float y1 = h - bordaSup;
        if (y1 <= y0 + 20f)
            return;

        bool twoDoors = doorCount >= 2;
        bool leftOnly = !twoDoors && shapeKind == ModuleShapeKind.CornerLLeft;
        bool rightOnly = !twoDoors && !leftOnly;

        // Porta do lado A (asa direita): fora da caixa em +Z (z = pd … pd+ft).
        if (twoDoors || rightOnly)
        {
            float x0 = pe + folgaA;
            float x1 = cd - bordaLat;
            if (x1 > x0 + 10f)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(x0, y0, pd),
                    new Vector3(x1, y1, pd + ft),
                    FaceKind.ModuleFront, "Porta dir.");
            }
        }

        // Porta do lado B (asa esquerda): fora da caixa em +X (x = pe … pe+ft).
        if (twoDoors || leftOnly)
        {
            float z0 = pd + folgaB;
            float z1 = ce - bordaLat;
            if (z1 > z0 + 10f)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(pe, y0, z0),
                    new Vector3(pe + ft, y1, z1),
                    FaceKind.ModuleFront, "Porta esq.");
            }
        }
    }

    private static void AddSarrafos(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        bool leftHand,
        float ce, float cd, float pe, float pd,
        float h, float t, float fundoFront,
        float travOuterX, float travOuterZ, bool useTravessas,
        float sFro, float sTra, float sarT, float recuoFro,
        bool showFro, bool showTra,
        bool froVert, bool traVert)
    {
        float y0 = h - sarT;
        float y1 = h;

        if (showTra)
        {
            if (useTravessas)
            {
                // Sarrafos ATRÁS dos fundos (plano da parede Z/X=0), como balcão reto —
                // começam após o envelope das travessas (travOuter).
                if (!traVert)
                {
                    AddBox(instance, toWorld, id,
                        new Vector3(travOuterX, y0, 0f),
                        new Vector3(cd - t, y1, sTra),
                        FaceKind.ModuleTop, "Sarrafo traseiro dir.");
                    AddBox(instance, toWorld, id,
                        new Vector3(0f, y0, travOuterZ),
                        new Vector3(sTra, y1, ce - t),
                        FaceKind.ModuleTop, "Sarrafo traseiro esq.");
                }
                else
                {
                    AddBox(instance, toWorld, id,
                        new Vector3(travOuterX, h - sTra, 0f),
                        new Vector3(cd - t, h, sarT),
                        FaceKind.ModuleTop, "Sarrafo traseiro dir.");
                    AddBox(instance, toWorld, id,
                        new Vector3(0f, h - sTra, travOuterZ),
                        new Vector3(sarT, h, ce - t),
                        FaceKind.ModuleTop, "Sarrafo traseiro esq.");
                }
            }
            else if (!traVert)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(fundoFront, y0, 0f),
                    new Vector3(cd - t, y1, sTra),
                    FaceKind.ModuleTop, "Sarrafo traseiro dir.");
                AddBox(instance, toWorld, id,
                    new Vector3(0f, y0, fundoFront),
                    new Vector3(sTra, y1, ce - t),
                    FaceKind.ModuleTop, "Sarrafo traseiro esq.");
            }
            else
            {
                AddBox(instance, toWorld, id,
                    new Vector3(fundoFront, h - sTra, 0f),
                    new Vector3(cd - t, h, sarT),
                    FaceKind.ModuleTop, "Sarrafo traseiro dir.");
                AddBox(instance, toWorld, id,
                    new Vector3(0f, h - sTra, fundoFront),
                    new Vector3(sarT, h, ce - t),
                    FaceKind.ModuleTop, "Sarrafo traseiro esq.");
            }
        }

        if (!showFro)
            return;

        float froZ = pd - recuoFro;
        float froX = pe - recuoFro;
        bool continuousOnRight = !leftHand;

        if (!froVert)
        {
            if (continuousOnRight)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(fundoFront, y0, froZ - sFro),
                    new Vector3(cd - t, y1, froZ),
                    FaceKind.ModuleTop, "Sarrafo dianteiro dir.");
                AddBox(instance, toWorld, id,
                    new Vector3(froX - sFro, y0, froZ),
                    new Vector3(froX, y1, ce - t),
                    FaceKind.ModuleTop, "Sarrafo dianteiro esq.");
            }
            else
            {
                AddBox(instance, toWorld, id,
                    new Vector3(froX - sFro, y0, fundoFront),
                    new Vector3(froX, y1, ce - t),
                    FaceKind.ModuleTop, "Sarrafo dianteiro esq.");
                AddBox(instance, toWorld, id,
                    new Vector3(froX, y0, froZ - sFro),
                    new Vector3(cd - t, y1, froZ),
                    FaceKind.ModuleTop, "Sarrafo dianteiro dir.");
            }
        }
        else
        {
            float yV0 = h - sFro;
            if (continuousOnRight)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(fundoFront, yV0, froZ - sarT),
                    new Vector3(cd - t, y1, froZ),
                    FaceKind.ModuleTop, "Sarrafo dianteiro dir.");
                AddBox(instance, toWorld, id,
                    new Vector3(froX - sarT, yV0, froZ),
                    new Vector3(froX, y1, ce - t),
                    FaceKind.ModuleTop, "Sarrafo dianteiro esq.");
            }
            else
            {
                AddBox(instance, toWorld, id,
                    new Vector3(froX - sarT, yV0, fundoFront),
                    new Vector3(froX, y1, ce - t),
                    FaceKind.ModuleTop, "Sarrafo dianteiro esq.");
                AddBox(instance, toWorld, id,
                    new Vector3(froX, yV0, froZ - sarT),
                    new Vector3(cd - t, y1, froZ),
                    FaceKind.ModuleTop, "Sarrafo dianteiro dir.");
            }
        }
    }

    public static Vector2[] BuildLContour(
        float comprimentoDireito,
        float comprimentoEsquerdo,
        float profundidadeDireita,
        float profundidadeEsquerda) =>
    [
        new(0f, 0f),
        new(comprimentoDireito, 0f),
        new(comprimentoDireito, profundidadeDireita),
        new(profundidadeEsquerda, profundidadeDireita),
        new(profundidadeEsquerda, comprimentoEsquerdo),
        new(0f, comprimentoEsquerdo)
    ];

    public static Vector2[] BuildInternalLContour(
        float cd,
        float ce,
        float pd,
        float pe,
        float panelT,
        float fundoT,
        float frontSetback = 0f)
    {
        float ix0 = fundoT;
        float iz0 = fundoT;
        float ix1 = cd - panelT;
        float iz1 = ce - panelT;
        float fx = MathF.Max(ix0 + 1f, pe - frontSetback);
        float fz = MathF.Max(iz0 + 1f, pd - frontSetback);

        ix1 = MathF.Max(ix1, ix0 + 10f);
        iz1 = MathF.Max(iz1, iz0 + 10f);
        fx = Math.Clamp(fx, ix0 + 10f, ix1);
        fz = Math.Clamp(fz, iz0 + 10f, iz1);

        return
        [
            new(ix0, iz0),
            new(ix1, iz0),
            new(ix1, fz),
            new(fx, fz),
            new(fx, iz1),
            new(ix0, iz1)
        ];
    }

    private static void ExtrudeInternalL(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        Vector2[] contour,
        float y0,
        float y1,
        FaceKind kind,
        string label,
        bool wholePiece)
    {
        float ix0 = contour[0].X;
        float iz0 = contour[0].Y;
        float ix1 = contour[1].X;
        float fz = contour[2].Y;
        float fx = contour[3].X;
        float iz1 = contour[4].Y;

        if (!wholePiece)
        {
            // Recortada/Recortado: bipartida — duas chapas com emenda (Promob).
            AddBox(instance, toWorld, id,
                new Vector3(ix0, y0, iz0),
                new Vector3(ix1, y1, fz),
                kind, label);
            if (iz1 > fz + 0.5f && fx > ix0 + 0.5f)
            {
                AddBox(instance, toWorld, id,
                    new Vector3(ix0, y0, fz),
                    new Vector3(fx, y1, iz1),
                    kind, label);
            }

            return;
        }

        // Inteira/Inteiro: prisma L sem faces internas na emenda.
        ExtrudeLPrismSeamless(instance, toWorld, id, ix0, iz0, ix1, fz, fx, iz1, y0, y1, kind, label);
    }

    /// <summary>
    /// Extruda o contorno L como peça única: topo/fundo do L + paredes só no perímetro
    /// (sem parede vertical na linha de emenda das duas pernas).
    /// </summary>
    private static void ExtrudeLPrismSeamless(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        float ix0, float iz0, float ix1, float fz, float fx, float iz1,
        float y0, float y1,
        FaceKind kind,
        string label)
    {
        // Contorno externo do L em planta (X,Z): 6 vértices CCW visto de cima.
        var ring =
            new Vector2[]
            {
                new(ix0, iz0),
                new(ix1, iz0),
                new(ix1, fz),
                new(fx, fz),
                new(fx, iz1),
                new(ix0, iz1)
            };

        Vector3 At(float x, float y, float z) => toWorld(new Vector3(x, y, z));
        bool hasArm = iz1 > fz + 0.5f && fx > ix0 + 0.5f;

        // Topo: uma face L (perímetro 6 pts) — LineLoop sem aresta de emenda.
        var topLoop = new[]
        {
            At(ix0, y1, iz0), At(ix1, y1, iz0), At(ix1, y1, fz),
            At(fx, y1, fz), At(fx, y1, iz1), At(ix0, y1, iz1)
        };
        var topP = At(ix0, y1, fz);
        var topTris = new List<(Vector3, Vector3, Vector3)>
        {
            (topLoop[0], topLoop[1], topLoop[2]),
            (topLoop[0], topLoop[2], topP)
        };
        if (hasArm)
        {
            topTris.Add((topP, topLoop[3], topLoop[4]));
            topTris.Add((topP, topLoop[4], topLoop[5]));
        }

        instance.Mesh.AddPolygonalFace(topLoop, topTris, kind, id, label);

        // Fundo: perímetro CW (normal para baixo).
        var botLoop = new[]
        {
            At(ix0, y0, iz0), At(ix0, y0, iz1), At(fx, y0, iz1),
            At(fx, y0, fz), At(ix1, y0, fz), At(ix1, y0, iz0)
        };
        var botTris = new List<(Vector3, Vector3, Vector3)>
        {
            (At(ix0, y0, iz0), At(ix0, y0, fz), At(ix1, y0, fz)),
            (At(ix0, y0, iz0), At(ix1, y0, fz), At(ix1, y0, iz0))
        };
        if (hasArm)
        {
            botTris.Add((At(ix0, y0, fz), At(ix0, y0, iz1), At(fx, y0, iz1)));
            botTris.Add((At(ix0, y0, fz), At(fx, y0, iz1), At(fx, y0, fz)));
        }

        instance.Mesh.AddPolygonalFace(botLoop, botTris, kind, id, label);

        // Paredes laterais só no perímetro externo.
        for (int i = 0; i < ring.Length; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Length];
            instance.Mesh.AddQuad(
                At(a.X, y0, a.Y), At(b.X, y0, b.Y),
                At(b.X, y1, b.Y), At(a.X, y1, a.Y),
                kind, id, label);
        }
    }

    private static void AddBox(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        Vector3 min,
        Vector3 max,
        FaceKind kind,
        string label)
    {
        ApplyPartOverride(instance, label, ref min, ref max);

        if (max.X - min.X < 0.5f || max.Y - min.Y < 0.5f || max.Z - min.Z < 0.5f)
            return;

        var mesh = instance.Mesh;
        var a = toWorld(new Vector3(min.X, min.Y, min.Z));
        var b = toWorld(new Vector3(max.X, min.Y, min.Z));
        var c = toWorld(new Vector3(max.X, max.Y, min.Z));
        var d = toWorld(new Vector3(min.X, max.Y, min.Z));
        var e = toWorld(new Vector3(min.X, min.Y, max.Z));
        var f = toWorld(new Vector3(max.X, min.Y, max.Z));
        var g = toWorld(new Vector3(max.X, max.Y, max.Z));
        var hv = toWorld(new Vector3(min.X, max.Y, max.Z));

        mesh.AddQuad(a, b, c, d, kind, id, label);
        mesh.AddQuad(e, f, g, hv, kind, id, label);
        mesh.AddQuad(a, e, hv, d, kind, id, label);
        mesh.AddQuad(b, f, g, c, kind, id, label);
        mesh.AddQuad(d, c, g, hv, kind, id, label);
        mesh.AddQuad(a, b, f, e, kind, id, label);
    }

    /// <summary>Aplica seta/override de peça (mesma regra do ModuleMeshBuilder).</summary>
    private static void ApplyPartOverride(
        ModuleInstance instance,
        string label,
        ref Vector3 min,
        ref Vector3 max)
    {
        if (string.IsNullOrEmpty(label) ||
            !instance.PartOverrides.TryGetValue(label, out var ov) ||
            ov == null ||
            !ov.HasAny)
            return;

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

        static void EnsureMinSize(ref float lo, ref float hi, float size)
        {
            if (hi - lo >= size)
                return;

            float center = (lo + hi) * 0.5f;
            lo = center - size * 0.5f;
            hi = center + size * 0.5f;
        }
    }

    private enum CantoLTipo
    {
        SemTravessas,
        Travessas,
        TravessasInvertidas
    }

    private sealed class CantoLOptions
    {
        public CantoLTipo Tipo { get; set; }
        public float LarguraTravMm { get; set; }
        public float ProfundidadeTravMm { get; set; }
        public float AvancoFundoSobreTravMm { get; set; }
        public float FolgaPortaA { get; set; }
        public float FolgaPortaB { get; set; }
        public float BordaLateralMm { get; set; }
        public float BordaInferiorMm { get; set; }
        public float BordaSuperiorMm { get; set; }
        public bool BaseInteira { get; set; } = true;
        public bool TampoInteiro { get; set; } = true;
    }
}
