namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Montagem de Caixa - Armários" (Dormitórios), levantada ao vivo no Promob Plus 5.60.
/// 3 folhas diretas + 2 subgrupos de canto (8 folhas). Distinto de Inferior/Bancadas|Criados.
/// </summary>
public static class BoxAssemblyArmarioSchema
{
    private static readonly string[] TipoLateral = ["Auto", "Fixo"];
    private static readonly string[] TipoRodape = ["Auto", "Fixo"];
    private static readonly string[] AlinhamentoLateral = ["Traseiro", "Central", "Frontal"];

    private static readonly BoxNodeDef LateralNode = new()
    {
        Id = "lateral",
        Header = "Lateral",
        Fields =
        [
            new()
            {
                Group = "Tipo Lateral",
                Key = "tip-lat",
                Label = "A — Lateral",
                Kind = BoxFieldKind.Choice,
                Options = TipoLateral,
                DefaultOption = "Fixo"
            },
            new()
            {
                Group = "Fixação Lateral - Base Inferior",
                Key = "arm-rbl",
                Label = "B — Avanço Base sobre Lateral (mm)",
                DefaultValue = 0f
            },
            new()
            {
                Group = "Fixação Lateral - Base Inferior",
                Key = "arm-rlb",
                Label = "C — Avanço Lateral Fixo sobre Base (mm)",
                DefaultValue = 58f
            },
            new()
            {
                Group = "Fixação Lateral - Base Superior",
                Key = "arm-rbl-sup",
                Label = "D — Avanço Base sobre Lateral (mm)",
                DefaultValue = 0f
            },
            new()
            {
                Group = "Fixação Lateral - Base Superior",
                Key = "arm-rlb-sup",
                Label = "E — Avanço Lateral sobre Base (mm)",
                DefaultValue = 10f
            },
            new()
            {
                Group = "Folga - Alinhamento",
                Key = "lat-fol",
                Label = "F — Folga Lateral (mm)",
                DefaultValue = 0f
            },
            new()
            {
                Group = "Folga - Alinhamento",
                Key = "lat-ali",
                Label = "G — Alinhamento",
                Kind = BoxFieldKind.Choice,
                Options = AlinhamentoLateral,
                DefaultOption = "Central"
            }
        ]
    };

    private static readonly BoxNodeDef RodapeNode = new()
    {
        Id = "rodape",
        Header = "Rodapé",
        Fields =
        [
            new()
            {
                Group = "Tipo Rodapé",
                Key = "tip-rod",
                Label = "A — Rodapé",
                Kind = BoxFieldKind.Choice,
                Options = TipoRodape,
                DefaultOption = "Fixo"
            },
            new()
            {
                Group = "Rodapé",
                Key = "rod-rec-fro",
                Label = "B — Recuo Rodapé Frontal (mm)",
                DefaultValue = 50f
            },
            new()
            {
                Group = "Rodapé",
                Key = "rod-rec-tra",
                Label = "C — Recuo Rodapé Traseiro (mm)",
                DefaultValue = 0f
            },
            new()
            {
                Group = "Rodapé Fixo",
                Key = "rod-alt-fix",
                Label = "D — Altura Rodapé Fixo (mm)",
                DefaultValue = 80f
            }
        ]
    };

    public static readonly BoxNodeDef[] DirectNodes =
    [
        LateralNode,
        RodapeNode,
        NodeFromInferior("fundo")
    ];

    public static readonly BoxGroupDef[] Groups =
    [
        new()
        {
            Header = "Canto L | Oblíquo",
            Nodes =
            [
                NodeFromInferior("canto-l-canto"),
                NodeFromInferior("canto-l-afastamento")
            ]
        },
        new()
        {
            Header = "Canto Reto",
            Nodes =
            [
                NodeFromInferior("canto-reto-canto"),
                NodeFromInferior("canto-reto-fechamentos"),
                NodeFromInferior("canto-reto-afastamento")
            ]
        }
    ];

    private static BoxNodeDef NodeFromInferior(string id)
    {
        var node = BoxAssemblyInferiorSchema.FindNode(id)
            ?? throw new InvalidOperationException($"Nó Inferior '{id}' não encontrado para Armário.");
        return new BoxNodeDef
        {
            Id = node.Id,
            Header = node.Header,
            Fields = node.Fields
        };
    }

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
