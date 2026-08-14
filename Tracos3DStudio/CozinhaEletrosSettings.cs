namespace Tracos3DStudio;

/// <summary>Configurações de Eletros (Cozinhas) — paridade Promob Configurador de Dimensões.</summary>
public sealed class CozinhaEletrosSettings
{
    public Dictionary<string, float> Numeric { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaEletrosSettings Clone() => new()
    {
        Numeric = new Dictionary<string, float>(Numeric, StringComparer.Ordinal),
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
