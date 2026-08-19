namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Gavetas" (Cozinhas), levantada no Promob Plus 5.60 ao vivo.
/// 4 folhas: Folgas · Fixação Lateral Contra Frente · Fixação Lateral Posterior · Fundos.
/// </summary>
public static class CozinhaGavetasSchema
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
                Num("folg-cor-tel", "A1 — Folga Corrediça Telescópica", 13.5f, "Gavetas Externas"),
                Num("folg-cor-inv", "A2 — Folga Corrediça Invisível", 5f, "Gavetas Externas"),
                Num("folg-fundo", "B — Folga até Fundo da Caixa", 40f, "Gavetas Externas"),
                Num("folg-sup-cf", "C — Folga Superior Contra-Frente", 30f, "Gavetas"),
                Num("folg-sup-lat", "D — Folga Superior Lateral", 27f, "Gavetas"),
                Num("folg-sup-pos", "E — Folga Superior Posterior", 30f, "Gavetas"),
                Num("folg-inf-cf", "F — Folga Inferior Contra-Frente", 22f, "Gavetas"),
                Num("folg-inf-lat", "G — Folga Inferior Lateral", 22f, "Gavetas"),
                Num("folg-inf-pos", "H — Folga Inferior Posterior", 22f, "Gavetas"),
                Num("fgav-sup-cf", "I — Folga Superior Contra-Frente", 43f, "Gavetão"),
                Num("fgav-sup-lat", "J — Folga Superior Lateral", 40f, "Gavetão"),
                Num("fgav-sup-pos", "K — Folga Superior Posterior", 42f, "Gavetão"),
                Num("fgav-inf-cf", "L — Folga Inferior Contra-Frente", 22f, "Gavetão"),
                Num("fgav-inf-lat", "M — Folga Inferior Lateral", 22f, "Gavetão"),
                Num("fgav-inf-pos", "N — Folga Inferior Posterior", 22f, "Gavetão"),
                Num("pl-inf-sup-cf", "I — Recuo Superior Contra-Frente", 3f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-sup-lat-dir", "J — Recuo Superior Lateral Direita", 0f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-sup-lat-esq", "K — Recuo Superior Lateral Esquerda", 0f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-sup-pos", "L — Recuo Superior Posterior", 3f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-inf-cf", "M — Recuo Inferior Contra-Frente", 0f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-inf-lat", "N — Recuo Inferior Lateral", 0f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-inf-inf-pos", "O — Recuo Inferior Posterior", 0f, "Gaveta Inferior | Porta-Latas MDF"),
                Num("pl-sup-sup-cf", "I — Recuo Superior Contra-Frente", 3f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-sup-lat-dir", "J — Recuo Superior Lateral Direita", 0f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-sup-lat-esq", "K — Recuo Superior Lateral Esquerda", 0f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-sup-pos", "L — Recuo Superior Posterior", 3f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-inf-cf", "M — Recuo Inferior Contra-Frente", 0f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-inf-lat", "N — Recuo Inferior Lateral", 0f, "Gaveta Superior | Porta-Latas MDF"),
                Num("pl-sup-inf-pos", "O — Recuo Inferior Posterior", 0f, "Gaveta Superior | Porta-Latas MDF")
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
