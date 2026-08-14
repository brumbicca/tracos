namespace Tracos3DStudio;

/// <summary>
/// Espessuras e limites de chapa por tipo de peça (paridade Promob — Chapas).
/// </summary>
public sealed class ChapaPieceSettings
{
    public float MaxWidthMm { get; set; } = 2750f;

    public float MaxLengthMm { get; set; } = 1830f;

    public float ThicknessMm { get; set; } = 18f;

    public ChapaPieceSettings Clone() => new()
    {
        MaxWidthMm = MaxWidthMm,
        MaxLengthMm = MaxLengthMm,
        ThicknessMm = ThicknessMm
    };
}

/// <summary>Identificadores de peça na árvore Chapas (Promob).</summary>
public static class ChapaPieceKinds
{
    public const string Lateral = "lateral";
    public const string Divisoria = "divisoria";
    public const string Base = "base";
    public const string Fundo = "fundo";
    public const string FundoInferior = "fundo-inferior";
    public const string FundoSuperior = "fundo-superior";
    public const string Traseira = "traseira";
    public const string Travessas = "travessas";
    public const string Prateleira = "prateleira";
    public const string PortasFrentes = "portas-frentes";
    public const string PainelPortas = "painel-portas";
    public const string FrenteGavInterna = "frente-gav-interna";
    public const string Tampo = "tampo";
    public const string Tamponamento = "tamponamento";
    public const string Painel = "painel";
    public const string Especial = "especial";

    // Chapas > Componentes (paridade Promob)
    public const string CompSarrafo = "comp-sarrafo";
    public const string CompRodape = "comp-rodape";
    public const string CompMoldura = "comp-moldura";
    public const string CompVista = "comp-vista";
    public const string CompFechamento = "comp-fechamento";
    public const string CompFechamentoLateral = "comp-fechamento-lateral";
    public const string CompApoioEletros = "comp-apoio-eletros";
    public const string CompMolduraEngrossuramento = "comp-moldura-engrossuramento";
    public const string CompFrenteFalsa = "comp-frente-falsa";
    public const string CompFrenteFalsaParcial = "comp-frente-falsa-parcial";
    public const string CompDistanciadorCantoReto = "comp-distanciador-canto-reto";
    public const string CompDistanciador = "comp-distanciador";
    public const string CompFrenteAdegaCircular = "comp-frente-adega-circular";
    public const string CompAfastadorMontante = "comp-afastador-montante";
    public const string CompLateralMontante = "comp-lateral-montante";

    // Chapas > Gavetas (paridade Promob)
    public const string GavLateral = "gav-lateral";
    public const string GavLateralMetalica = "gav-lateral-metalica";
    public const string GavContraFrente = "gav-contra-frente";
    public const string GavPosterior = "gav-posterior";
    public const string GavFundo = "gav-fundo";

    public static readonly string[] CozinhaPieces =
    [
        Lateral, Divisoria, Base, FundoInferior, FundoSuperior, Traseira, Travessas,
        Prateleira, PortasFrentes, PainelPortas, FrenteGavInterna, Tampo, Tamponamento, Painel, Especial
    ];

    public static readonly string[] DormitorioPieces =
    [
        Lateral, Divisoria, Base, Fundo, Traseira, Travessas,
        Prateleira, PortasFrentes, PainelPortas, FrenteGavInterna, Tampo, Painel, Especial
    ];

    /// <summary>Chapas > Componentes — Dormitórios (paridade Promob; distinto de Cozinhas).</summary>
    public static readonly string[] DormitorioComponentes =
    [
        CompSarrafo, CompRodape, CompMoldura, CompVista, CompFechamento, CompFechamentoLateral,
        CompAfastadorMontante, CompLateralMontante,
        CompMolduraEngrossuramento, CompFrenteFalsa, CompFrenteFalsaParcial,
        CompDistanciadorCantoReto, CompDistanciador
    ];

    /// <summary>Chapas > Gavetas | Sapateiras — Dormitórios (paridade Promob; 4 folhas).</summary>
    public static readonly string[] DormitorioGavetasSapateiras =
    [
        GavLateral, GavContraFrente, GavPosterior, GavFundo
    ];

    /// <summary>Subárvore Chapas > Componentes (Cozinhas — paridade Promob).</summary>
    public static readonly string[] Componentes =
    [
        CompSarrafo, CompRodape, CompMoldura, CompVista, CompFechamento, CompFechamentoLateral,
        CompApoioEletros, CompMolduraEngrossuramento, CompFrenteFalsa, CompFrenteFalsaParcial,
        CompDistanciadorCantoReto, CompDistanciador, CompFrenteAdegaCircular
    ];

    /// <summary>Subárvore Chapas > Gavetas (compartilhada entre ambientes — paridade Promob).</summary>
    public static readonly string[] Gavetas =
    [
        GavLateral, GavLateralMetalica, GavContraFrente, GavPosterior, GavFundo
    ];

    public static string DisplayName(string kind, bool dormitorio = false) => kind switch
    {
        Lateral => "Lateral",
        Divisoria => "Divisória",
        Base => "Base",
        Fundo => "Fundo",
        FundoInferior => "Fundo — Inferiores",
        FundoSuperior => "Fundo — Superiores",
        Traseira => "Traseira",
        Travessas => "Travessas",
        Prateleira => "Prateleira",
        PortasFrentes => dormitorio ? "Porta | Frentes" : "Portas | Frentes",
        PainelPortas => "Painel p/ Portas",
        FrenteGavInterna => dormitorio ? "Frente Gav Interna" : "Frente Gav. Interna",
        Tampo => "Tampo",
        Tamponamento => "Tamponamento",
        Painel => "Painel",
        Especial => "Especial",
        CompSarrafo => "Sarrafo",
        CompRodape => "Rodapé",
        CompMoldura => "Moldura",
        CompVista => "Vista",
        CompFechamento => "Fechamento",
        CompFechamentoLateral => "Fechamento Lateral",
        CompApoioEletros => "Apoio Eletros",
        CompMolduraEngrossuramento => "Moldura Engrossuramento",
        CompFrenteFalsa => "Frente Falsa",
        CompFrenteFalsaParcial => "Frente Falsa Parcial",
        CompDistanciadorCantoReto => "Distanciador - Canto Reto",
        CompDistanciador => "Distanciador",
        CompAfastadorMontante => "Afastador Montante",
        CompLateralMontante => "Lateral Montante",
        CompFrenteAdegaCircular => "Frente Adega Circular",
        GavLateral => "Lateral",
        GavLateralMetalica => "Lateral Metálica",
        GavContraFrente => "Contra Frente",
        GavPosterior => "Posterior",
        GavFundo => "Fundo",
        _ => kind
    };
}

public sealed class CategoryChapaSettings
{
    public Dictionary<string, ChapaPieceSettings> Pieces { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public ChapaPieceSettings GetOrCreate(string kind)
    {
        if (!Pieces.TryGetValue(kind, out var settings))
        {
            settings = new ChapaPieceSettings();
            Pieces[kind] = settings;
        }

        return settings;
    }

    public CategoryChapaSettings Clone()
    {
        var clone = new CategoryChapaSettings();
        foreach (var (key, value) in Pieces)
            clone.Pieces[key] = value.Clone();

        return clone;
    }

    public static CategoryChapaSettings CreateCozinhaDefaults(
        float panelThicknessMm = 18f,
        float backThicknessMm = 6f,
        float frontThicknessMm = 18f)
    {
        var settings = new CategoryChapaSettings();
        foreach (var kind in ChapaPieceKinds.CozinhaPieces)
        {
            var piece = settings.GetOrCreate(kind);
            piece.ThicknessMm = kind switch
            {
                ChapaPieceKinds.FundoInferior or ChapaPieceKinds.FundoSuperior => backThicknessMm,
                ChapaPieceKinds.PortasFrentes or ChapaPieceKinds.FrenteGavInterna => frontThicknessMm,
                _ => panelThicknessMm
            };
        }

        ApplyComponentesGavetasDefaults(settings, panelThicknessMm, backThicknessMm);
        return settings;
    }

    private static void ApplyComponentesGavetasDefaults(
        CategoryChapaSettings settings,
        float panelThicknessMm,
        float backThicknessMm)
    {
        foreach (var kind in ChapaPieceKinds.Componentes)
            settings.GetOrCreate(kind).ThicknessMm = panelThicknessMm;

        foreach (var kind in ChapaPieceKinds.Gavetas)
        {
            settings.GetOrCreate(kind).ThicknessMm = kind == ChapaPieceKinds.GavFundo
                ? backThicknessMm
                : panelThicknessMm;
        }
    }

    public static CategoryChapaSettings CreateDormitorioDefaults(
        float panelThicknessMm = 18f,
        float backThicknessMm = 6f,
        float frontThicknessMm = 18f)
    {
        var settings = new CategoryChapaSettings();
        foreach (var kind in ChapaPieceKinds.DormitorioPieces)
        {
            var piece = settings.GetOrCreate(kind);
            piece.ThicknessMm = kind switch
            {
                ChapaPieceKinds.Fundo => backThicknessMm,
                ChapaPieceKinds.PortasFrentes or ChapaPieceKinds.FrenteGavInterna => frontThicknessMm,
                _ => panelThicknessMm
            };
        }

        ApplyDormitorioComponentesGavetasDefaults(settings, panelThicknessMm, backThicknessMm);
        return settings;
    }

    private static void ApplyDormitorioComponentesGavetasDefaults(
        CategoryChapaSettings settings,
        float panelThicknessMm,
        float backThicknessMm)
    {
        foreach (var kind in ChapaPieceKinds.DormitorioComponentes)
            settings.GetOrCreate(kind).ThicknessMm = panelThicknessMm;

        foreach (var kind in ChapaPieceKinds.DormitorioGavetasSapateiras)
        {
            settings.GetOrCreate(kind).ThicknessMm = kind == ChapaPieceKinds.GavFundo
                ? backThicknessMm
                : panelThicknessMm;
        }
    }
}
