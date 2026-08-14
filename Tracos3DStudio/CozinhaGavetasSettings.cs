namespace Tracos3DStudio;

/// <summary>Configurações de Gavetas (Cozinhas) — paridade Promob Configurador de Dimensões.</summary>
public sealed class CozinhaGavetasSettings
{
    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaGavetasSettings Clone() => new()
    {
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
