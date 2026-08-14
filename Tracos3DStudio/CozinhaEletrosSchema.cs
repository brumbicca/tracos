namespace Tracos3DStudio;

/// <summary>
/// Estrutura de "Eletros" (Cozinhas), levantada no Promob Plus 5.60 e na documentação oficial.
/// Pasta Eletros → folha Eletros (seções Vão A–I e Apoio J–M).
/// </summary>
public static class CozinhaEletrosSchema
{
    private static readonly string[] ApoioOptions =
        ["80", "100", "120", "140", "160", "180", "200"];

    public const string LeafId = "eletros";

    public static readonly BoxNodeDef Leaf = new()
    {
        Id = LeafId,
        Header = "Eletros",
        Fields =
        [
            new() { Key = "vao-fog-lar", Label = "A — Fogão — Largura Vão (mm)", DefaultValue = 540f },
            new() { Key = "vao-for-lar", Label = "B — Forno — Largura Vão (mm)", DefaultValue = 500f },
            new() { Key = "vao-for-alt", Label = "C — Forno — Altura Vão (mm)", DefaultValue = 350f },
            new() { Key = "vao-mic-lar", Label = "D — Microondas — Largura Vão (mm)", DefaultValue = 500f },
            new() { Key = "vao-mic-alt", Label = "E — Microondas — Altura Vão (mm)", DefaultValue = 350f },
            new() { Key = "vao-lav-lar", Label = "F — Lava louças — Largura Vão (mm)", DefaultValue = 450f },
            new() { Key = "vao-lav-alt", Label = "G — Lava louças — Altura Vão (mm)", DefaultValue = 520f },
            new() { Key = "afast-entre-vao", Label = "H — Afastamento entre Vãos (mm)", DefaultValue = 170f },
            new() { Key = "afast-inf", Label = "I — Afastamento Inferior (mm)", DefaultValue = 100f },
            new()
            {
                Key = "apo-fog", Label = "J — Fogão — Dimensão Apoio (mm)", Kind = BoxFieldKind.Choice,
                Options = ApoioOptions, DefaultOption = "120"
            },
            new()
            {
                Key = "apo-for", Label = "K — Forno — Dimensão Apoio (mm)", Kind = BoxFieldKind.Choice,
                Options = ApoioOptions, DefaultOption = "160"
            },
            new()
            {
                Key = "apo-mic", Label = "L — Microondas — Dimensão Apoio (mm)", Kind = BoxFieldKind.Choice,
                Options = ApoioOptions, DefaultOption = "160"
            },
            new()
            {
                Key = "apo-lav", Label = "M — Lava louças — Dimensão Apoio (mm)", Kind = BoxFieldKind.Choice,
                Options = ApoioOptions, DefaultOption = "160"
            }
        ]
    };

    public static BoxNodeDef? FindNode(string id) =>
        id == LeafId ? Leaf : null;
}
