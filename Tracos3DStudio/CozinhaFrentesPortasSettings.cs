namespace Tracos3DStudio;

/// <summary>Configurações de Frentes | Portas (Cozinhas) — paridade Promob Configurador de Dimensões.</summary>
public sealed class CozinhaFrentesPortasSettings
{
    public Dictionary<string, float> Numeric { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaFrentesPortasSettings Clone() => new()
    {
        Numeric = new Dictionary<string, float>(Numeric, StringComparer.Ordinal),
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
