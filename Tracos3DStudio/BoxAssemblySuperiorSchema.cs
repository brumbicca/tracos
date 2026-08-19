namespace Tracos3DStudio;

/// <summary>
/// Estrutura completa de "Montagem da Caixa - Superior" (Cozinhas), levantada ao vivo no Promob.
/// 8 folhas diretas + 2 subgrupos de canto (5 folhas).
/// </summary>
public static class BoxAssemblySuperiorSchema
{
    private static readonly string[] SimNao = ["Sim", "Não"];

    public static readonly BoxNodeDef[] DirectNodes =
    [
        new()
        {
            Id = "fix-lat-base-inf",
            Header = "Fixação Lateral - Base Inferior",
            Fields =
            [
                new() { Key = "flbi-abl", Label = "A — Avanço Base sobre Lateral (mm)" },
                new() { Key = "flbi-alb", Label = "B — Avanço Lateral sobre Base (mm)" }
            ]
        },
        new()
        {
            Id = "fix-lat-base-sup",
            Header = "Fixação Lateral - Base Superior",
            Fields =
            [
                new() { Key = "flbs-abl", Label = "A — Avanço Base sobre Lateral (mm)" },
                new() { Key = "flbs-alb", Label = "B — Avanço Lateral sobre Base (mm)" }
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
                    Options = ["Inteiro", "Sem fundo"], DefaultOption = "Inteiro"
                },
                new() { Key = "fundo-recuo", Label = "B — Recuo Fundo (mm)" },
                new()
                {
                    Key = "fundo-trav-sust", Label = "C — Travessas de Sustentação", Kind = BoxFieldKind.Choice,
                    Options = SimNao, DefaultOption = "Não"
                },
                new() { Key = "fundo-dim-trav", Label = "D — Dimensão Travessas (mm)" }
            ]
        },
        new()
        {
            Id = "fix-base-fundo",
            Header = "Fixação Base - Fundo",
            Fields =
            [
                new() { Key = "fbf-afb-inf", Label = "A — Avanço Fundo sobre Base Inferior (mm)" },
                new() { Key = "fbf-afb-sup", Label = "B — Avanço Fundo sobre Base Superior (mm)" },
                new() { Key = "fbf-abf", Label = "C — Avanço Base sobre Fundo (mm)" },
                new() { Key = "fbf-rec-inf", Label = "D — Recuo Base Inferior (mm)" },
                new() { Key = "fbf-rec-sup", Label = "E — Recuo Base Superior (mm)" }
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
            Id = "prateleira",
            Header = "Prateleira",
            Fields =
            [
                new() { Key = "prat-recuo", Label = "A — Recuo Prateleira (mm)", DefaultValue = 20f },
                new() { Key = "prat-folga", Label = "B — Folga Lateral (mm)", DefaultValue = 4f }
            ]
        },
        new()
        {
            Id = "divisoria",
            Header = "Divisória",
            Fields =
            [
                new() { Key = "div-recuo-fro", Label = "A — Recuo Frontal Divisória (mm)" },
                new() { Key = "div-dim-dist", Label = "B — Dimensão Distanciador (mm)" }
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
                    Id = "canto-l-cantos",
                    Header = "Cantos",
                    Fields =
                    [
                        new()
                        {
                            Key = "cl-tipo", Label = "A — Tipo Canto", Kind = BoxFieldKind.Choice,
                            Options = ["Sem travessas", "Travessas", "Travessas invertidas"],
                            DefaultOption = "Travessas"
                        },
                        new() { Key = "cl-larg-trav", Label = "B — Largura Travessas (mm)" },
                        new() { Key = "cl-prof-trav", Label = "C — Profundidade Travessas (mm)" },
                        new() { Key = "cl-aftv", Label = "D — Avanço Fundo sobre Travessa (mm)" },
                        new()
                        {
                            Key = "cl-tipo-base", Label = "E — Tipo Base", Kind = BoxFieldKind.Choice,
                            Options = ["Inteira", "Recortada"], DefaultOption = "Inteira"
                        },
                        new() { Key = "cl-folga-pa", Label = "F — Folga Esquerda da Porta (mm)", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-folga-pb", Label = "G — Folga Direita da Porta (mm)", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-folga-entre", Label = "H — Folga entre Portas (mm)", DefaultValue = 2f, AllowNegative = true },
                        new() { Key = "cl-abt", Label = "I — Avanço Base sobre Traseira (mm)" },
                        new() { Key = "cl-atb", Label = "J — Avanço Traseira sobre Base (mm)" },
                        new() { Key = "cl-aft", Label = "K — Avanço Fundo sobre Traseira (mm)" },
                        new() { Key = "cl-prof-dist", Label = "L — Profundidade Distanciador (mm)" }
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
                        new() { Key = "cr-affb-sup", Label = "B — Avanço Frente Falsa sobre Base Superior (mm)" },
                        new() { Key = "cr-affb-inf", Label = "C — Avanço Frente Falsa sobre Base Inferior (mm)" },
                        new() { Key = "cr-affl", Label = "D — Avanço Frente Falsa sobre Lateral (mm)" },
                        new() { Key = "cr-rff", Label = "E — Recuo Frente Falsa (mm)" },
                        new()
                        {
                            Key = "cr-uso-dist", Label = "F — Utilização do Distanciador", Kind = BoxFieldKind.Choice,
                            Options = SimNao, DefaultOption = "Não"
                        },
                        new() { Key = "cr-affd", Label = "G — Avanço Frente Falsa Inteira sobre Distanciador (mm)" },
                        new() { Key = "cr-adff", Label = "H — Avanço Distanciador sobre Frente Falsa (mm)" },
                        new() { Key = "cr-adp", Label = "I — Avanço Distanciador sobre Prateleira (mm)" },
                        new() { Key = "cr-rec-prat", Label = "J — Recuo Prateleira (mm)" },
                        new() { Key = "cr-ava-por", Label = "K — Avanço Porta sobre Frente Falsa / Parcial (mm)" }
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
                            Options = ["Lateral", "Frontal"], DefaultOption = "Lateral"
                        },
                        new()
                        {
                            Key = "crf-recuo-fro", Label = "B — Recuo Fechamento Frontal | Dimensão FF Parcial Dupla (mm)"
                        },
                        new() { Key = "crf-dim-fro", Label = "C — Dimensão Fechamento Frontal (mm)" },
                        new()
                        {
                            Key = "crf-sup", Label = "D — Fechamento Superior", Kind = BoxFieldKind.Choice,
                            Options = SimNao, DefaultOption = "Não"
                        },
                        new()
                        {
                            Key = "crf-inf", Label = "E — Fechamento Inferior", Kind = BoxFieldKind.Choice,
                            Options = SimNao, DefaultOption = "Não"
                        },
                        new()
                        {
                            Key = "crf-tra", Label = "F — Fechamento Traseiro", Kind = BoxFieldKind.Choice,
                            Options = SimNao, DefaultOption = "Não"
                        }
                    ]
                },
                new()
                {
                    Id = "canto-reto-afastamento",
                    Header = "Afastamento Parede",
                    Fields =
                    [
                        new() { Key = "cr-afa-lat", Label = "A — Afastamento Lateral (mm)" },
                        new() { Key = "cr-afa-tra", Label = "B — Afastamento Traseiro (mm)" }
                    ]
                }
            ]
        }
    ];

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
}
