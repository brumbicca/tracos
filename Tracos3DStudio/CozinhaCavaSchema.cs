namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Cozinhas Cava" (Cozinhas), alinhada ao Promob Plus 5.60
/// e à documentação oficial (7 folhas + subgrupo Frentes | Portas).
/// </summary>
public static class CozinhaCavaSchema
{
    private static readonly string[] FolgaMmOptions =
        Enumerable.Range(0, 31).Select(i => i.ToString()).ToArray();

    private static readonly string[] SimNao = ["Sim", "Não"];

    private static readonly string[] TipoPuxador = ["Alumínio", "Madeira", "Listagem"];

    private static readonly string[] DimensaoBarra =
        ["0", "1000", "1500", "2000", "2500", "3000", "3500", "4000"];

    private static readonly string[] QuantidadePuxador = ["Barra", "Metro", "Unidade"];

    private static readonly string[] TipoLateral = ["Padrão", "Recuada"];

    private static BoxFieldDef Choice(string key, string label, string[] options, string defaultOption) => new()
    {
        Key = key,
        Label = label,
        Kind = BoxFieldKind.Choice,
        Options = options,
        DefaultOption = defaultOption
    };

    private static BoxFieldDef Num(string key, string label, float defaultValue = 0f) => new()
    {
        Key = key,
        Label = label,
        DefaultValue = defaultValue
    };

    private static BoxFieldDef FolgaChoice(string key, string label, string defaultOption = "4") =>
        Choice(key, label, FolgaMmOptions, defaultOption);

    public static readonly BoxNodeDef[] DirectNodes =
    [
        new()
        {
            Id = "tipo-puxador",
            Header = "Tipo Puxador",
            Fields =
            [
                Choice("tipo-puxador", "A — Tipo Puxador", TipoPuxador, "Alumínio"),
                Choice("dim-barra", "B — Dimensão Barra", DimensaoBarra, "3000"),
                Choice("quantidade", "C — Quantidade", QuantidadePuxador, "Barra")
            ]
        },
        new()
        {
            Id = "tipo-lateral",
            Header = "Tipo Lateral",
            Fields =
            [
                Choice("tipo-lateral", "A — Tipo Lateral", TipoLateral, "Padrão")
            ]
        },
        new()
        {
            Id = "inferiores",
            Header = "Inferiores",
            Fields =
            [
                Num("alu-alt-pux", "A — Altura Puxador (Puxador Cava Alumínio)"),
                Num("alu-alt-pux-int", "B — Altura Puxador Intermediário (Alumínio)"),
                Num("alu-prof-cava", "C — Profundidade Puxador Cava (Alumínio)"),
                Num("mad-alt-pux", "D — Altura Puxador (Puxador Cava Madeira)"),
                Num("mad-alt-pux-int", "E — Altura Puxador Intermediário (Madeira)"),
                Num("mad-prof-cava", "F — Profundidade Puxador Cava (Madeira)"),
                Num("dist-pux-int", "G — Distância até Puxador Intermediário (Gaveteiros)"),
                Num("alt-1gav", "H — Altura 1ª Gaveta (Módulo 1G + 2Gav)")
            ]
        },
        new()
        {
            Id = "superiores",
            Header = "Superiores",
            Fields =
            [
                Num("prof-pux-rec-base", "A — Prof Puxador / Recuo Base"),
                Num("alt-pux", "B — Altura Puxador"),
                Num("alu-alt-pux-int", "C — Altura Puxador Intermediário (Alumínio)"),
                Num("alu-prof-pux-int", "D — Profundidade Puxador Intermediário (Alumínio)"),
                Num("mad-alt-pux-int", "E — Altura Puxador Intermediário (Madeira)"),
                Num("mad-prof-cava", "F — Profundidade Puxador Cava (Madeira)")
            ]
        },
        new()
        {
            Id = "despenseiros",
            Header = "Despenseiros",
            Fields =
            [
                Num("alu-larg-pux", "A — Largura Puxador (Alumínio)"),
                Num("alu-larg-pux-int", "B — Largura Puxador Intermediário (Alumínio)"),
                Num("alu-prof-cava", "C — Profundidade Puxador Cava (Alumínio)"),
                Num("rec-lat", "D — Recuo da Lateral (Módulos com Lateral Recuada)"),
                Num("mad-larg-pux", "E — Largura Puxador (Madeira)"),
                Num("mad-larg-pux-int", "F — Largura Puxador Intermediário (Madeira)"),
                Num("mad-prof-cava", "G — Profundidade Puxador Cava (Madeira)")
            ]
        },
        new()
        {
            Id = "canto-l",
            Header = "Canto L",
            Fields =
            [
                Choice("trav-front", "A — Travessas Frontais (Inferiores)", SimNao, "Sim"),
                Num("trav-larg", "B — Largura Travessas"),
                Num("trav-prof", "C — Profundidade Travessas")
            ]
        }
    ];

    public static readonly BoxGroupDef[] Groups =
    [
        new()
        {
            Header = "Frentes | Portas",
            Nodes =
            [
                new()
                {
                    Id = "portas-inferiores",
                    Header = "Inferiores",
                    Fields =
                    [
                        Num("av-porta-pux-sup", "A — Avanço Porta/Frente sobre Pux Superior"),
                        Num("av-frente-sup-pux-int", "B — Avanço Frente Superior sobre Pux Intermediário"),
                        Num("av-frente-inf-pux-int", "C — Avanço Frente Inferior sobre Pux Intermediário"),
                        FolgaChoice("entre-portas", "D — Entre Portas/Frentes"),
                        FolgaChoice("borda-lat", "E — Borda Lateral"),
                        FolgaChoice("borda-inf", "F — Borda Inferior"),
                        FolgaChoice("borda-div", "G — Borda Divisória")
                    ]
                },
                new()
                {
                    Id = "portas-superiores",
                    Header = "Superiores",
                    Fields =
                    [
                        Num("av-frente-sup-pux-int", "A — Avanço Frente Superior sobre Pux Intermediário"),
                        Num("av-frente-inf-pux-int", "B — Avanço Frente Inferior sobre Pux Intermediário"),
                        FolgaChoice("entre-portas", "C — Entre Portas", "5"),
                        FolgaChoice("borda-lat", "D — Borda Lateral", "5"),
                        FolgaChoice("borda-inf", "E — Borda Inferior", "10"),
                        FolgaChoice("borda-sup", "F — Borda Superior", "11")
                    ]
                },
                new()
                {
                    Id = "portas-despenseiros",
                    Header = "Despenseiros",
                    Fields =
                    [
                        Num("av-porta-pux-lat", "A — Avanço Porta sobre Pux Lateral"),
                        Num("av-porta-pux-int", "B — Avanço Porta sobre Pux Intermediário"),
                        FolgaChoice("borda-lat", "C — Borda Lateral"),
                        FolgaChoice("borda-sup", "D — Borda Superior"),
                        FolgaChoice("borda-inf", "E — Borda Inferior")
                    ]
                },
                new()
                {
                    Id = "portas-torres",
                    Header = "Torres",
                    Fields =
                    [
                        Num("av-porta-pux", "A — Avanço Porta/Frente sobre Pux"),
                        Num("av-frente-sup-pux-int", "B — Avanço Frente Superior sobre Pux Intermediário"),
                        Num("av-frente-inf-pux-int", "C — Avanço Frente Inferior sobre Pux Intermediário"),
                        FolgaChoice("entre-portas", "D — Entre Portas/Frentes"),
                        FolgaChoice("borda-lat", "E — Borda Lateral"),
                        FolgaChoice("borda-sup", "F — Borda Superior"),
                        FolgaChoice("borda-inf", "G — Borda Inferior")
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
        {
            foreach (var node in group.Nodes)
                yield return node;
        }
    }

    public static BoxNodeDef? FindNode(string id) =>
        AllNodes().FirstOrDefault(n => n.Id == id);
}
