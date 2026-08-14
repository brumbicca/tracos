namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Montagem da Caixa - Despenseiros | Torres" (Cozinhas), levantada no Promob Plus 5.60
/// e na documentação oficial Promob.
/// 9 folhas diretas (sem subgrupos de canto).
/// </summary>
public static class BoxAssemblyDespenseirosSchema
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
                new() { Key = "fbf-rec-inf", Label = "C — Recuo Base Inferior (mm)" },
                new() { Key = "fbf-rec-sup", Label = "D — Recuo Base Superior (mm)" }
            ]
        },
        new()
        {
            Id = "fix-fundo-lateral",
            Header = "Fixação Fundo - Lateral",
            Fields =
            [
                new() { Key = "ffl-afl", Label = "A — Avanço Fundo sobre Lateral (mm)" }
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
            Id = "divisoria",
            Header = "Divisória",
            Fields =
            [
                new() { Key = "div-recuo-fro", Label = "A — Recuo Frontal Divisória (mm)" },
                new() { Key = "div-recuo-tra-mov", Label = "B — Recuo Traseiro Divisórias Móveis (mm)" },
                new() { Key = "div-recuo-tra-fix", Label = "C — Recuo Traseiro Divisórias Fixas (mm)" },
                new() { Key = "div-dim-dist", Label = "D — Dimensão Distanciador (mm)" }
            ]
        },
        new()
        {
            Id = "prateleira",
            Header = "Prateleira",
            Fields =
            [
                new() { Key = "prat-recuo-fro", Label = "A — Recuo Frontal Prateleiras (mm)", DefaultValue = 20f },
                new() { Key = "prat-recuo-tra-mov", Label = "B — Recuo Traseiro Prateleiras Móveis (mm)" },
                new() { Key = "prat-recuo-tra-fix", Label = "C — Recuo Traseiro Prateleiras Fixas (mm)" },
                new() { Key = "prat-folga", Label = "D — Folga Lateral (mm)", DefaultValue = 4f }
            ]
        },
        new()
        {
            Id = "superior-recuado",
            Header = "Superior Recuado",
            Fields =
            [
                new() { Key = "sr-recuo-tra", Label = "A — Recuo Traseiro (mm)" },
                new() { Key = "sr-afb", Label = "B — Avanço Fundo sobre Base (mm)" },
                new() { Key = "sr-afp", Label = "C — Avanço Fundo sobre Prateleira (mm)" },
                new() { Key = "sr-recuo-fundo", Label = "D — Recuo Fundo (mm)" }
            ]
        }
    ];

    public static IEnumerable<BoxNodeDef> AllNodes()
    {
        foreach (var node in DirectNodes)
            yield return node;
    }

    public static BoxNodeDef? FindNode(string id) =>
        AllNodes().FirstOrDefault(n => n.Id == id);
}
