namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Gavetas" (Cozinhas), levantada no Promob Plus 5.60 ao vivo.
/// 4 folhas: Folgas · Fixação Lateral Contra Frente · Fixação Lateral Posterior · Fundos.
/// </summary>
public static class CozinhaGavetasSchema
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
                Choice("folg-sup-cf", "C — Folga Superior Contra-Frente", "0"),
                Choice("folg-sup-lat", "D — Folga Superior Lateral", "0"),
                Choice("folg-sup-pos", "E — Folga Superior Posterior", "0"),
                Choice("folg-inf-cf", "F — Folga Inferior Contra-Frente", "0"),
                Choice("folg-inf-lat", "G — Folga Inferior Lateral", "0"),
                Choice("folg-inf-pos", "H — Folga Inferior Posterior", "0"),
                Choice("fgav-sup-cf", "I — Folga Superior Contra-Frente", "0"),
                Choice("fgav-sup-lat", "J — Folga Superior Lateral", "0"),
                Choice("fgav-sup-pos", "K — Folga Superior Posterior", "0"),
                Choice("fgav-inf-cf", "L — Folga Inferior Contra-Frente", "0"),
                Choice("fgav-inf-lat", "M — Folga Inferior Lateral", "0"),
                Choice("fgav-inf-pos", "N — Folga Inferior Posterior", "0")
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
