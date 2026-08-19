namespace Tracos3DStudio;

/// <summary>Tipo de campo de um nó da Montagem da Caixa (paridade Promob).</summary>
public enum BoxFieldKind
{
    Numeric,
    Choice
}

/// <summary>Definição declarativa de um campo (rótulo Promob A/B/C…).</summary>
public sealed class BoxFieldDef
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    /// <summary>Barra de grupo Promob (ex.: Fixação Lateral - Base Inferior).</summary>
    public string? Group { get; init; }
    public BoxFieldKind Kind { get; init; } = BoxFieldKind.Numeric;
    public float DefaultValue { get; init; }
    /// <summary>Permite valores assinados quando a medida representa avanço/recuo paramétrico.</summary>
    public bool AllowNegative { get; init; }
    public string[] Options { get; init; } = [];
    public string DefaultOption { get; init; } = "";
}

/// <summary>Nó folha da árvore (com seu conjunto de campos).</summary>
public sealed class BoxNodeDef
{
    public required string Id { get; init; }
    public required string Header { get; init; }
    public BoxFieldDef[] Fields { get; init; } = [];
}

/// <summary>Subgrupo (pasta) da árvore — sem campos próprios.</summary>
public sealed class BoxGroupDef
{
    public required string Header { get; init; }
    public BoxNodeDef[] Nodes { get; init; } = [];
}

/// <summary>
/// Estrutura completa de "Montagem da Caixa - Inferior" (Cozinhas), levantada ao vivo no Promob.
/// 13 folhas diretas + 3 subgrupos de canto.
/// </summary>
public static class BoxAssemblyInferiorSchema
{
    private static readonly string[] SimNao = ["Sim", "Não"];
    private static readonly string[] SentidoSarrafo = ["Horizontal", "Vertical"];

    public static readonly BoxNodeDef[] DirectNodes =
    [
        new()
        {
            Id = "fix-lat-base",
            Header = "Fixação Lateral - Base",
            Fields =
            [
                new() { Key = "fix-lb-abl", Label = "A — Avanço Base sobre Lateral (mm)" },
                new() { Key = "fix-lb-alb", Label = "B — Avanço Lateral sobre Base (mm)" }
            ]
        },
        new()
        {
            Id = "fundo",
            Header = "Fundo",
            Fields =
            [
                new()
                {
                    Key = "fundo-tipo", Label = "A — Tipo Fundo", Kind = BoxFieldKind.Choice,
                    Options = ["Inteiro", "Rebaixado", "Trav Vertical", "Trav Horizontal", "Sem fundo"],
                    DefaultOption = "Inteiro"
                },
                new() { Key = "fundo-recuo", Label = "B — Recuo Fundo (mm)" },
                new() { Key = "fundo-rebaixo", Label = "C — Rebaixo Fundo (mm)" },
                new() { Key = "fundo-dim-trav", Label = "D — Dimensão Travessas (mm)" },
                new() { Key = "fundo-afa-sup", Label = "E — Afastamento Superior (mm)" },
                new() { Key = "fundo-afa-inf", Label = "F — Afastamento Inferior (mm)" },
                new()
                {
                    Key = "fundo-trav-sust", Label = "G — Travessas de Sustentação", Kind = BoxFieldKind.Choice,
                    Options = ["Não", "1", "2"], DefaultOption = "Não"
                },
                new() { Key = "fundo-dim-trav-sust", Label = "H — Dimensão Travessas (sustentação) (mm)" }
            ]
        },
        new()
        {
            Id = "fix-base-fundo",
            Header = "Fixação Base - Fundo",
            Fields =
            [
                new() { Key = "fbf-afb", Label = "A — Avanço Fundo sobre Base (mm)" },
                new() { Key = "fbf-abf", Label = "B — Avanço Base sobre Fundo (mm)" },
                new() { Key = "fbf-rec-base", Label = "C — Recuo Base (mm)" }
            ]
        },
        new()
        {
            Id = "fix-fundo-lateral",
            Header = "Fixação Fundo - Lateral",
            Fields =
            [
                new() { Key = "ffl-afl", Label = "E — Avanço Fundo sobre Lateral (mm)" },
                new() { Key = "ffl-alf", Label = "F — Avanço Lateral sobre Fundo (mm)" }
            ]
        },
        new()
        {
            Id = "fix-fundo-divisoria",
            Header = "Fixação Fundo - Divisória",
            Fields =
            [
                new() { Key = "ffd-afd", Label = "A — Avanço Fundo sobre Divisória (mm)" }
            ]
        },
        new()
        {
            Id = "lateral",
            Header = "Lateral",
            Fields =
            [
                new() { Key = "lat-rebaixo", Label = "A — Rebaixo de Lateral (mm)" },
                new() { Key = "lat-folga", Label = "B — Folga Lateral (mm)" },
                new()
                {
                    Key = "lat-alinhamento", Label = "C — Alinhamento Lateral",
                    Kind = BoxFieldKind.Choice,
                    Options = ["Traseira", "Frente", "Centro"], DefaultOption = "Traseira"
                }
            ]
        },
        new()
        {
            Id = "sarrafo",
            Header = "Sarrafo",
            Fields =
            [
                new()
                {
                    Key = "sar-tipo", Label = "A — Tipo Sarrafo", Kind = BoxFieldKind.Choice,
                    Options = ["Frontal", "Traseiro", "Ambos", "Inteiro", "Sem sarrafo"],
                    DefaultOption = "Frontal"
                },
                new()
                {
                    Key = "sar-seg", Label = "B — Sarrafo Segmentado", Kind = BoxFieldKind.Choice,
                    Options = ["Não Segmentado", "Frontal", "Traseiro", "Ambos", "Inteiro"],
                    DefaultOption = "Não Segmentado"
                },
                new() { Key = "sar-prof-fro", Label = "C — Profundidade Sarrafo Frontal (mm)", DefaultValue = 70f },
                new() { Key = "sar-prof-tra", Label = "D — Profundidade Sarrafo Traseiro (mm)", DefaultValue = 70f },
                new()
                {
                    Key = "sar-sent-fro", Label = "E — Sentido Sarrafo Frontal", Kind = BoxFieldKind.Choice,
                    Options = SentidoSarrafo, DefaultOption = "Horizontal"
                },
                new()
                {
                    Key = "sar-sent-tra", Label = "F — Sentido Sarrafo Traseiro", Kind = BoxFieldKind.Choice,
                    Options = SentidoSarrafo, DefaultOption = "Horizontal"
                },
                new() { Key = "sar-recuo-fro", Label = "G — Recuo Sarrafo Frontal (mm)" },
                new()
                {
                    Key = "sar-formato", Label = "H — Formato Sarrafo", Kind = BoxFieldKind.Choice,
                    Options = ["Chanfrado", "Reto"], DefaultOption = "Reto"
                }
            ]
        },
        new()
        {
            Id = "fix-sarrafo-lateral",
            Header = "Fixação Sarrafo - Lateral",
            Fields =
            [
                new() { Key = "fsl-asl", Label = "A — Avanço Sarrafo sobre Lateral (mm)" }
            ]
        },
        new()
        {
            Id = "fix-sarrafo-fundo-inteiro",
            Header = "Fixação Sarrafo - Fundo Inteiro",
            Fields =
            [
                new() { Key = "fsfi-asf", Label = "A — Avanço Sarrafo sobre Fundo (mm)" },
                new() { Key = "fsfi-afs", Label = "B — Avanço Fundo sobre Sarrafo (mm)" }
            ]
        },
        new()
        {
            Id = "fix-sarrafo-fundo-rebaixado",
            Header = "Fixação Sarrafo - Fundo Rebaixado | Travessas | Sem fundo",
            Fields =
            [
                new() { Key = "fsfr-recuo", Label = "A — Recuo Sarrafo (mm)" },
                new() { Key = "fsfr-rebaixo", Label = "B — Rebaixo Sarrafo (mm)" }
            ]
        },
        new()
        {
            Id = "fix-painel-frontal-lateral",
            Header = "Fixação Painel Frontal - Lateral",
            Fields =
            [
                new() { Key = "fpfl-alf", Label = "A — Avanço Lateral sobre Frontal (mm)" },
                new() { Key = "fpfl-afl", Label = "B — Avanço Frontal sobre Lateral (mm)" }
            ]
        },
        new()
        {
            Id = "divisoria",
            Header = "Divisória",
            Fields =
            [
                new() { Key = "div-recuo-fro", Label = "A — Recuo Frontal Divisória (mm)" },
                new() { Key = "div-recuo-tra-mov", Label = "B — Recuo Traseiro Divisória Móvel (mm)" },
                new() { Key = "div-recuo-tra-fix", Label = "C — Recuo Traseiro Divisória Fixa (mm)" },
                new() { Key = "div-rebaixo", Label = "D — Rebaixo Divisória (mm)" },
                new() { Key = "div-dim-dist", Label = "E — Dimensão Distanciador (mm)" }
            ]
        },
        new()
        {
            Id = "prateleira",
            Header = "Prateleira",
            Fields =
            [
                new() { Key = "prat-recuo", Label = "A — Recuo Frontal Prateleira (mm)", DefaultValue = 20f },
                new() { Key = "prat-recuo-tra-mov", Label = "B — Recuo Traseiro Prateleira Móvel (mm)" },
                new() { Key = "prat-recuo-tra-fix", Label = "C — Recuo Traseiro Prateleira Fixa (mm)" },
                new() { Key = "prat-folga", Label = "D — Folga Lateral (mm)", DefaultValue = 4f }
            ]
        }
    ];

    public static readonly BoxGroupDef[] Groups =
    [
        new()
        {
            Header = "Canto L | Oblíquo",
            Nodes =
            [
                new()
                {
                    Id = "canto-l-canto",
                    Header = "Canto",
                    Fields =
                    [
                        new()
                        {
                            Key = "cl-tipo", Label = "A — Tipo Canto", Kind = BoxFieldKind.Choice,
                            Options = ["Sem travessas", "Travessas", "Travessas invertidas"],
                            DefaultOption = "Travessas"
                        },
                        new() { Key = "cl-larg-trav", Label = "B — Largura Travessas (mm)", DefaultValue = 88f },
                        new() { Key = "cl-prof-trav", Label = "C — Profundidade Travessas (mm)", DefaultValue = 88f },
                        new() { Key = "cl-aftv", Label = "D — Avanço Fundo sobre Travessa (mm)", DefaultValue = 8f },
                        new()
                        {
                            Key = "cl-tipo-tampo", Label = "E — Tipo Prateleira", Kind = BoxFieldKind.Choice,
                            Options = ["Única", "Bipartida"], DefaultOption = "Única"
                        },
                        new()
                        {
                            Key = "cl-tipo-base", Label = "F — Tipo Base", Kind = BoxFieldKind.Choice,
                            Options = ["Única", "Bipartida"], DefaultOption = "Única"
                        },
                        new() { Key = "cl-folga-pa", Label = "G — Folga Esquerda da Porta (mm)", Group = "Portas — Canto Oblíquo", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-folga-pb", Label = "H — Folga Direita da Porta (mm)", Group = "Portas — Canto Oblíquo", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-folga-entre", Label = "I — Folga entre Portas (mm)", Group = "Portas — Canto Oblíquo", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl2-folga-pa", Label = "J — Folga Interna Porta A (mm)", Group = "Portas — Canto L 2P", DefaultValue = 5f, AllowNegative = true },
                        new() { Key = "cl2-folga-pb", Label = "K — Folga Interna Porta B (mm)", Group = "Portas — Canto L 2P", DefaultValue = 5f, AllowNegative = true },
                        new() { Key = "cl3-folga-pa", Label = "L — Folga Interna Porta A (mm)", Group = "Portas — Canto L 3P", DefaultValue = 5f, AllowNegative = true },
                        new() { Key = "cl3-folga-pb", Label = "M — Folga Interna Porta B (mm)", Group = "Portas — Canto L 3P", DefaultValue = 5f, AllowNegative = true },
                        new() { Key = "cl3-folga-entre", Label = "N — Folga entre as duas folhas (mm)", Group = "Portas — Canto L 3P", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-abt", Label = "O — Avanço Base sobre Traseira (mm)", Group = "Montagem" },
                        new() { Key = "cl-atb", Label = "P — Avanço Traseira sobre Base (mm)", Group = "Montagem" },
                        new() { Key = "cl-aft", Label = "Q — Avanço Fundo sobre Traseira (mm)", Group = "Montagem" },
                        new() { Key = "cl-prof-dist", Label = "R — Profundidade Distanciador (mm)", Group = "Montagem" }
                    ]
                },
                new()
                {
                    Id = "canto-l-afastamento",
                    Header = "Afastamento Parede",
                    Fields =
                    [
                        new() { Key = "cl-afa-lat", Label = "A — Afastamento Lateral (mm)" },
                        new() { Key = "cl-afa-tra", Label = "B — Afastamento Traseiro (mm)" }
                    ]
                }
            ]
        },
        new()
        {
            Header = "Canto Reto",
            Nodes =
            [
                new()
                {
                    Id = "canto-reto-canto",
                    Header = "Canto",
                    Fields =
                    [
                        new()
                        {
                            Key = "cr-tipo-ff", Label = "A — Tipo Frente Falsa", Kind = BoxFieldKind.Choice,
                            Options = ["Inteira", "Parcial Dupla"], DefaultOption = "Inteira"
                        },
                        new() { Key = "cr-affb", Label = "B — Avanço Frente Falsa sobre Base (mm)", DefaultValue = 18f, AllowNegative = true },
                        new() { Key = "cr-affs", Label = "C — Avanço Frente Falsa sobre Sarrafo (mm)", DefaultValue = 18f, AllowNegative = true },
                        new() { Key = "cr-affl", Label = "D — Avanço Frente Falsa sobre Lateral (mm)", DefaultValue = 18f, AllowNegative = true },
                        new() { Key = "cr-affffp", Label = "E — Avanço Frente Falsa sobre Frente Falsa Parcial (mm)", DefaultValue = 0f, AllowNegative = true },
                        new() { Key = "cr-rff", Label = "F — Recuo Frente Falsa (mm)", DefaultValue = 0f },
                        new() { Key = "cr-rffp", Label = "G — Recuo Frente Falsa Parcial (mm)", DefaultValue = 0f },
                        new() { Key = "cr-dim-ffp", Label = "H — Dimensão Frente Falsa Parcial (mm)", DefaultValue = 0f },
                        new()
                        {
                            Key = "cr-uso-dist", Label = "I — Utilização do Distanciador", Kind = BoxFieldKind.Choice,
                            Options = ["Usar", "Não usar"], DefaultOption = "Usar"
                        },
                        new() { Key = "cr-affd", Label = "J — Avanço Frente Falsa Inteira sobre Distanciador (mm)", DefaultValue = -12f, AllowNegative = true },
                        new() { Key = "cr-adff", Label = "K — Avanço Distanciador sobre Frente Falsa (mm)", DefaultValue = 0f, AllowNegative = true },
                        new() { Key = "cr-adp", Label = "L — Avanço Distanciador sobre Prateleira (mm)", DefaultValue = 0f, AllowNegative = true },
                        new() { Key = "cr-rec-prat", Label = "M — Recuo Prateleira (mm)", DefaultValue = 0f },
                        new() { Key = "cr-ava-por", Label = "N — Avanço Porta sobre Frente Falsa / Parcial (mm)", DefaultValue = 27f, AllowNegative = true }
                    ]
                },
                new()
                {
                    Id = "canto-reto-fechamentos",
                    Header = "Fechamentos",
                    Fields =
                    [
                        new()
                        {
                            Key = "crf-tipo", Label = "A — Tipo Fechamento", Kind = BoxFieldKind.Choice,
                            // Promob: Lateral = aleta na dobradiça (face p/ módulo sequencial);
                            // Frontal = faixa no plano da porta.
                            Options = ["Lateral", "Frontal"], DefaultOption = "Lateral"
                        },
                        new()
                        {
                            Key = "crf-recuo-fro",
                            Label = "B — Distância Lateral do Fechamento Frontal (mm)",
                            DefaultValue = 80f, AllowNegative = true
                        },
                        new() { Key = "crf-dim-fro", Label = "C — Dimensão Fechamento Frontal (mm)", DefaultValue = 30f },
                        new()
                        {
                            Key = "crf-pos-lat",
                            Label = "D — Avanço do Fechamento Frontal (mm)",
                            DefaultValue = 18f,
                            AllowNegative = true
                        },
                        new()
                        {
                            Key = "crf-sup", Label = "E — Fechamento Superior", Kind = BoxFieldKind.Choice,
                            Options = ["Usar", "Não usar"], DefaultOption = "Não usar"
                        },
                        new()
                        {
                            Key = "crf-inf", Label = "F — Fechamento Inferior", Kind = BoxFieldKind.Choice,
                            Options = ["Usar", "Não usar"], DefaultOption = "Não usar"
                        },
                        new()
                        {
                            Key = "crf-tra", Label = "G — Fechamento Traseiro", Kind = BoxFieldKind.Choice,
                            Options = ["Usar", "Não usar"], DefaultOption = "Não usar"
                        }
                    ]
                },
                new()
                {
                    Id = "canto-reto-sarrafo",
                    Header = "Sarrafo",
                    Fields =
                    [
                        new()
                        {
                            Key = "crs-tipo-fro", Label = "A — Tipo Sarrafo Frontal", Kind = BoxFieldKind.Choice,
                            Options = ["Total", "Parcial", "Sem sarrafo"], DefaultOption = "Total"
                        }
                    ]
                },
                new()
                {
                    Id = "canto-reto-afastamento",
                    Header = "Afastamento Parede",
                    Fields =
                    [
                        new() { Key = "cr-afa-lat", Label = "A — Afastamento Lateral (mm)", DefaultValue = 30f },
                        new() { Key = "cr-afa-tra", Label = "B — Afastamento Traseiro (mm)" }
                    ]
                }
            ]
        }
    ];

    /// <summary>Todos os nós (diretos + de subgrupos) para lookup por Id.</summary>
    public static IEnumerable<BoxNodeDef> AllNodes()
    {
        foreach (var node in DirectNodes)
            yield return node;
        foreach (var group in Groups)
            foreach (var node in group.Nodes)
                yield return node;
    }

    public static BoxNodeDef? FindNode(string id) =>
        AllNodes().FirstOrDefault(n => n.Id == id);

    public static BoxFieldDef? FindField(string key) =>
        AllNodes().SelectMany(n => n.Fields).FirstOrDefault(f => f.Key == key);

    // Valores numéricos são assinados por regra global. A propriedade
    // AllowNegative permanece no contrato somente para compatibilidade com
    // catálogos já gravados; ela não restringe mais a edição.
    public static bool AllowsNegative(string key) => FindField(key)?.Kind == BoxFieldKind.Numeric;
}
