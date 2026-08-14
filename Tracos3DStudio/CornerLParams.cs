namespace Tracos3DStudio;

/// <summary>
/// Parâmetros do Canto L (Cozinhas → Inferiores → Cantos).
/// Coordenadas em planta (X,Z): canto externo das paredes em (0,0);
/// asa direita ao longo de +X, asa esquerda ao longo de +Z.
/// <para>
/// Paridade Promob (Avançado): Largura/Profundidade = comprimentos das asas (envelope);
/// Medida A / Medida B = profundidades das asas (não confundir com o envelope).
/// </para>
/// </summary>
public sealed class CornerLParams
{
    public float ComprimentoEsquerdo { get; set; } = 950f;
    public float ComprimentoDireito { get; set; } = 950f;
    public float ProfundidadeEsquerda { get; set; } = 550f;
    public float ProfundidadeDireita { get; set; } = 550f;
    public float Altura { get; set; } = 850f;
    public float EspessuraMdf { get; set; } = 18f;
    public float AlturaSarrafo { get; set; } = 80f;
    public float FolgaPortas { get; set; } = 2f;

    /// <summary>Esq espelha a geometria em planta (troca X↔Z dos parâmetros efetivos).</summary>
    public bool IsLeftHand { get; set; }

    public static CornerLParams FromModuleDefaults(
        float envelopeWidth,
        float armDepth,
        float height,
        float panelMm,
        bool leftHand)
    {
        float length = MathF.Max(400f, envelopeWidth);
        float depth = MathF.Max(250f, armDepth);
        return new CornerLParams
        {
            ComprimentoDireito = length,
            ComprimentoEsquerdo = length,
            ProfundidadeDireita = depth,
            ProfundidadeEsquerda = depth,
            Altura = MathF.Max(400f, height),
            EspessuraMdf = panelMm > 0f ? panelMm : 18f,
            IsLeftHand = leftHand
        };
    }

    /// <summary>
    /// Inserção / aplicar configurador: largura do catálogo → comprimentos das asas;
    /// profundidade do configurador (Inferiores B) → Medida A e B.
    /// </summary>
    public void ApplyInsertion(float armLength, float height, float armDepth)
    {
        ComprimentoDireito = MathF.Max(400f, armLength);
        ComprimentoEsquerdo = MathF.Max(400f, armLength);
        ProfundidadeDireita = MathF.Max(250f, armDepth);
        ProfundidadeEsquerda = MathF.Max(250f, armDepth);
        Altura = MathF.Max(400f, height);
    }

    /// <summary>
    /// Painel: Largura A = comprimento asa direita (Cd), Largura B = asa esquerda (Ce).
    /// Não altera Medida A/B (Pe/Pd).
    /// </summary>
    public void ApplyEnvelopeLengths(float larguraA, float larguraB, float height)
    {
        ComprimentoDireito = MathF.Max(400f, larguraA);
        ComprimentoEsquerdo = MathF.Max(400f, larguraB);
        Altura = MathF.Max(400f, height);
    }

    /// <summary>
    /// Converte envelope efetivo (instance.Width/Depth após rebuild) em Largura A/B lógicas.
    /// No L esquerdo o rebuild troca os eixos — precisa inverter na leitura.
    /// </summary>
    public void ApplyEffectiveEnvelope(float envelopeWidth, float envelopeDepth, float height)
    {
        if (IsLeftHand)
            ApplyEnvelopeLengths(envelopeDepth, envelopeWidth, height);
        else
            ApplyEnvelopeLengths(envelopeWidth, envelopeDepth, height);
    }

    /// <summary>Promob Medida A / Medida B — profundidades das asas.</summary>
    public void ApplyArmDepths(float depthA, float depthB)
    {
        ProfundidadeDireita = MathF.Max(250f, depthA);
        ProfundidadeEsquerda = MathF.Max(250f, depthB);
    }

    /// <summary>Compat: envelope efetivo → Largura A/B lógicas (respeita L esquerdo).</summary>
    public void SyncFromEnvelope(float width, float height, float depth)
    {
        ApplyEffectiveEnvelope(width, depth, height);
    }

    public void Validate()
    {
        ComprimentoEsquerdo = Math.Clamp(ComprimentoEsquerdo, 400f, 3000f);
        ComprimentoDireito = Math.Clamp(ComprimentoDireito, 400f, 3000f);
        ProfundidadeEsquerda = Math.Clamp(ProfundidadeEsquerda, 250f, 800f);
        ProfundidadeDireita = Math.Clamp(ProfundidadeDireita, 250f, 800f);
        Altura = Math.Clamp(Altura, 400f, 1200f);
        EspessuraMdf = Math.Clamp(EspessuraMdf, 12f, 30f);
        AlturaSarrafo = Math.Clamp(AlturaSarrafo, 40f, MathF.Min(200f, Altura * 0.4f));
        FolgaPortas = Math.Clamp(FolgaPortas, 0.5f, 10f);

        // Consistência: profundidade de cada asa < comprimento correspondente.
        if (ProfundidadeEsquerda >= ComprimentoEsquerdo)
            ProfundidadeEsquerda = ComprimentoEsquerdo * 0.55f;
        if (ProfundidadeDireita >= ComprimentoDireito)
            ProfundidadeDireita = ComprimentoDireito * 0.55f;
    }

    /// <summary>Valores efetivos já considerando espelho Esq.</summary>
    public (float Ce, float Cd, float Pe, float Pd) EffectiveSides()
    {
        if (!IsLeftHand)
            return (ComprimentoEsquerdo, ComprimentoDireito, ProfundidadeEsquerda, ProfundidadeDireita);

        return (ComprimentoDireito, ComprimentoEsquerdo, ProfundidadeDireita, ProfundidadeEsquerda);
    }

    public CornerLParams Clone() =>
        new()
        {
            ComprimentoEsquerdo = ComprimentoEsquerdo,
            ComprimentoDireito = ComprimentoDireito,
            ProfundidadeEsquerda = ProfundidadeEsquerda,
            ProfundidadeDireita = ProfundidadeDireita,
            Altura = Altura,
            EspessuraMdf = EspessuraMdf,
            AlturaSarrafo = AlturaSarrafo,
            FolgaPortas = FolgaPortas,
            IsLeftHand = IsLeftHand
        };
}
