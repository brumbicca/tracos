namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Gavetas Internas | Auxiliares" (Cozinhas), alinhada ao Promob Plus 5.60
/// e à documentação oficial (folhas Folgas · Fixação Lateral · Fundos).
/// </summary>
public static class CozinhaGavetasInternasSchema
{
    private static BoxFieldDef Num(string key, string label, float defaultValue = 0f) => new()
    {
        Key = key,
        Label = label,
        Kind = BoxFieldKind.Numeric,
        DefaultValue = defaultValue,
        AllowNegative = true
    };

    public static readonly BoxNodeDef[] DirectNodes =
    [
        new()
        {
            Id = "folgas",
            Header = "Folgas",
            Fields =
            [
                Num("folg-cor-tel", "A1 — Folga Corrediça Telescópica", 13.5f, "Gavetas Internas | Auxiliares"),
                Num("folg-cor-inv", "A2 — Folga Corrediça Invisível", 5f, "Gavetas Internas | Auxiliares"),
                Num("folg-fundo", "B — Folga até Fundo da Caixa", 40f, "Gavetas Internas | Auxiliares"),
                Num("av-lat-frente", "C — Avanço Lateral da Frente", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-sup-cf", "D — Folga Superior Contra-Frente", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-sup-lat", "E — Folga Superior Lateral", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-sup-pos", "F — Folga Superior Posterior", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-inf-cf", "G — Folga Inferior Contra-Frente", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-inf-lat", "H — Folga Inferior Lateral", 0f, "Gavetas Internas | Auxiliares"),
                Num("folg-inf-pos", "I — Folga Inferior Posterior", 0f, "Gavetas Internas | Auxiliares"),
                Num("gint-sup", "J — Folga Superior", 0f, "Gavetas Internas"),
                Num("gint-inf", "K — Folga Inferior", 0f, "Gavetas Internas"),
                Num("gint-entre", "L — Folga Entre Gavetas", 0f, "Gavetas Internas"),
                Num("gaux-sup", "M — Folga Superior", 0f, "Gavetas Auxiliares"),
                Num("gaux-inf", "N — Folga Inferior", 0f, "Gavetas Auxiliares")
            ]
        },
        new()
        {
            Id = "fix-contra-frente",
            Header = "Fixação Lateral - Contra Frente",
            Fields =
            [
                Num("av-lat-cf", "A — Avanço Lateral sobre Contra Frente"),
                Num("av-cf-lat", "B — Avanço Contra Frente sobre Lateral")
            ]
        },
        new()
        {
            Id = "fix-posterior",
            Header = "Fixação Lateral - Posterior",
            Fields =
            [
                Num("av-lat-pos", "A — Avanço Lateral sobre Posterior"),
                Num("av-pos-lat", "B — Avanço Posterior sobre Lateral")
            ]
        },
        new()
        {
            Id = "fundos",
            Header = "Fundos",
            Fields =
            [
                Num("av-fun-lat", "A — Avanço Fundo sobre Lateral"),
                Num("av-fun-cf", "B — Avanço Fundo sobre Frente/Contra Frente"),
                Num("av-fun-pos", "C — Avanço Fundo sobre Posterior"),
                Num("recuo-fundo", "D — Recuo Fundo")
            ]
        }
    ];

    private static BoxFieldDef Num(string key, string label, float defaultValue, string group) => new()
    {
        Key = key,
        Label = label,
        Group = group,
        Kind = BoxFieldKind.Numeric,
        DefaultValue = defaultValue,
        AllowNegative = true
    };

    public static IEnumerable<BoxNodeDef> AllNodes()
    {
        foreach (var node in DirectNodes)
            yield return node;
    }

    public static BoxNodeDef? FindNode(string id) =>
        AllNodes().FirstOrDefault(n => n.Id == id);
}
