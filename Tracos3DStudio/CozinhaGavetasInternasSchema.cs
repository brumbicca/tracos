namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Gavetas Internas | Auxiliares" (Cozinhas), alinhada ao Promob Plus 5.60
/// e à documentação oficial (folhas Folgas · Fixação Lateral · Fundos).
/// </summary>
public static class CozinhaGavetasInternasSchema
{
    private static readonly string[] FolgaMmOptions =
        Enumerable.Range(0, 31).Select(i => i.ToString()).ToArray();

    private static BoxFieldDef Choice(string key, string label, string defaultOption = "0") => new()
    {
        Key = key,
        Label = label,
        Kind = BoxFieldKind.Choice,
        Options = FolgaMmOptions,
        DefaultOption = defaultOption
    };

    public static readonly BoxNodeDef[] DirectNodes =
    [
        new()
        {
            Id = "folgas",
            Header = "Folgas",
            Fields =
            [
                Choice("folg-cor", "A — Folga Corrediça", "4"),
                Choice("folg-fundo", "B — Folga até Fundo da Caixa", "0"),
                Choice("av-lat-frente", "C — Avanço Lateral da Frente", "0"),
                Choice("folg-sup-cf", "D — Folga Superior Contra-Frente", "0"),
                Choice("folg-sup-lat", "E — Folga Superior Lateral", "0"),
                Choice("folg-sup-pos", "F — Folga Superior Posterior", "0"),
                Choice("folg-inf-cf", "G — Folga Inferior Contra-Frente", "0"),
                Choice("folg-inf-lat", "H — Folga Inferior Lateral", "0"),
                Choice("folg-inf-pos", "I — Folga Inferior Posterior", "0"),
                Choice("gint-sup", "J — Folga Superior (Gavetas Internas)", "0"),
                Choice("gint-inf", "K — Folga Inferior (Gavetas Internas)", "0"),
                Choice("gint-entre", "L — Folga Entre Gavetas (Gavetas Internas)", "0"),
                Choice("gaux-sup", "M — Folga Superior (Gavetas Auxiliares)", "0"),
                Choice("gaux-inf", "N — Folga Inferior (Gavetas Auxiliares)", "0")
            ]
        },
        new()
        {
            Id = "fix-contra-frente",
            Header = "Fixação Lateral - Contra Frente",
            Fields =
            [
                Choice("av-lat-cf", "A — Avanço Lateral sobre Contra Frente"),
                Choice("av-cf-lat", "B — Avanço Contra Frente sobre Lateral")
            ]
        },
        new()
        {
            Id = "fix-posterior",
            Header = "Fixação Lateral - Posterior",
            Fields =
            [
                Choice("av-lat-pos", "A — Avanço Lateral sobre Posterior"),
                Choice("av-pos-lat", "B — Avanço Posterior sobre Lateral")
            ]
        },
        new()
        {
            Id = "fundos",
            Header = "Fundos",
            Fields =
            [
                Choice("av-fun-lat", "A — Avanço Fundo sobre Lateral"),
                Choice("av-fun-cf", "B — Avanço Fundo sobre Frente/Contra Frente"),
                Choice("av-fun-pos", "C — Avanço Fundo sobre Posterior"),
                Choice("recuo-fundo", "D — Recuo Fundo")
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
