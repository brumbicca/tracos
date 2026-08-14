namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Frentes | Portas" (Cozinhas), levantada no Promob Plus 5.60.
/// 7 folhas diretas + subgrupo Folgas Painel (2 folhas).
/// </summary>
public static class CozinhaFrentesPortasSchema
{
    private static readonly string[] FolgaMmOptions =
        Enumerable.Range(0, 31).Select(i => i.ToString()).ToArray();

    private static readonly string[] SimNao = ["Sim", "Não"];

    private static readonly string[] PuxadorQuantidade = ["1", "2", "3", "4"];

    private static readonly string[] PuxadorAltura =
        ["0", "50", "100", "150", "200", "250", "300", "350", "400"];

    private static readonly string[] PuxadorEspessura =
        ["0", "1", "2", "3", "4", "5", "6", "8", "10"];

    private static readonly string[] PuxadorBarra =
        ["0", "1000", "1500", "2000", "2500", "3000", "3500", "4000"];

    public static readonly BoxNodeDef[] DirectNodes =
    [
        BordaNode("inferiores", "Inferiores", "A — Entre Portas/Frentes",
            "4", "4", "4", "4", "4"),
        BordaNode("superiores", "Superiores", "A — Entre Portas/Frentes",
            "5", "5", "10", "10", "11"),
        BordaNode("despenseiros", "Despenseiros", "A — Entre Portas",
            "4", "4", "4", "4", "4"),
        BordaNode("embutidas", "Embutidas", "A — Entre Portas/Frentes",
            "4", "4", "4", "4", "4"),
        BordaNode("torres", "Torres", "A — Entre Portas/Frentes",
            "4", "4", "4", "4", "4"),
        new()
        {
            Id = "puxador-gola",
            Header = "Puxador Gola",
            Fields =
            [
                new()
                {
                    Key = "altura", Label = "A — Altura", Kind = BoxFieldKind.Choice,
                    Options = PuxadorAltura, DefaultOption = "0"
                },
                new()
                {
                    Key = "ponteiras", Label = "B — Ponteiras", Kind = BoxFieldKind.Choice,
                    Options = SimNao, DefaultOption = "Sim"
                },
                new()
                {
                    Key = "esp-ponteiras", Label = "C — Espessura Ponteiras", Kind = BoxFieldKind.Choice,
                    Options = PuxadorEspessura, DefaultOption = "0"
                },
                new()
                {
                    Key = "quantidade", Label = "D — Quantidade", Kind = BoxFieldKind.Choice,
                    Options = PuxadorQuantidade, DefaultOption = "1"
                },
                new()
                {
                    Key = "dim-barra", Label = "E — Dimensão Barra", Kind = BoxFieldKind.Choice,
                    Options = PuxadorBarra, DefaultOption = "0"
                }
            ]
        }
    ];

    public static readonly BoxGroupDef[] Groups =
    [
        new()
        {
            Header = "Folgas Painel",
            Nodes =
            [
                new()
                {
                    Id = "portas-aluminio",
                    Header = "Portas Alumínio",
                    Fields =
                    [
                        new() { Key = "fol-p10", Label = "A — Folga Perfil 10 (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p20", Label = "A — Folga Perfil 20 (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p40", Label = "A — Folga Perfil 40 (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p45", Label = "A — Folga Perfil 45 (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p45b", Label = "A — Folga Perfil 45 Boleado (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p45l", Label = "A — Folga Perfil 45 Liso (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p45lb", Label = "A — Folga Perfil 45 Liso Boleado (mm)", DefaultValue = 2f },
                        new() { Key = "fol-p50", Label = "A — Folga Perfil 50 (mm)", DefaultValue = 2f },
                        new() { Key = "fol-per-bor", Label = "A — Folga Perfil Borda (mm)", DefaultValue = 2f },
                        new() { Key = "fol-gol-alu", Label = "B — Folga Gola Alumínio (mm)", DefaultValue = 2f }
                    ]
                },
                new()
                {
                    Id = "portas-vidro",
                    Header = "Portas Vidro",
                    Fields =
                    [
                        new() { Key = "fol-vid-r4", Label = "A — 4 Perfis (mm)", DefaultValue = 2f },
                        new() { Key = "fol-vid-r2", Label = "B — 2 Perfis (mm)", DefaultValue = 2f }
                    ]
                }
            ]
        }
    ];

    private static BoxNodeDef BordaNode(
        string id,
        string header,
        string entreLabel,
        string entreDefault,
        string latDefault,
        string supDefault,
        string infDefault,
        string divDefault) => new()
    {
        Id = id,
        Header = header,
        Fields =
        [
            new()
            {
                Key = "entre-portas", Label = entreLabel, Kind = BoxFieldKind.Choice,
                Options = FolgaMmOptions, DefaultOption = entreDefault
            },
            new()
            {
                Key = "borda-lat", Label = "B — Borda Lateral", Kind = BoxFieldKind.Choice,
                Options = FolgaMmOptions, DefaultOption = latDefault
            },
            new()
            {
                Key = "borda-sup", Label = "C — Borda Superior", Kind = BoxFieldKind.Choice,
                Options = FolgaMmOptions, DefaultOption = supDefault
            },
            new()
            {
                Key = "borda-inf", Label = "D — Borda Inferior", Kind = BoxFieldKind.Choice,
                Options = FolgaMmOptions, DefaultOption = infDefault
            },
            new()
            {
                Key = "borda-div", Label = "E — Borda Divisória", Kind = BoxFieldKind.Choice,
                Options = FolgaMmOptions, DefaultOption = divDefault
            }
        ]
    };

    public static IEnumerable<BoxNodeDef> AllNodes()
    {
        foreach (var node in DirectNodes)
            yield return node;

        foreach (var group in Groups)
        {
            foreach (var node in group.Nodes)
                yield return node;
        }
    }

    public static BoxNodeDef? FindNode(string id) =>
        AllNodes().FirstOrDefault(n => n.Id == id);
}
