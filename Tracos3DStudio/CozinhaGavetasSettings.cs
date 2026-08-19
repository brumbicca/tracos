namespace Tracos3DStudio;

/// <summary>Configurações de Gavetas (Cozinhas) — paridade Promob Configurador de Dimensões.</summary>
public sealed class CozinhaGavetasSettings
{
    public Dictionary<string, float> Numeric { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Legado de perfis anteriores; migrado automaticamente para Numeric.</summary>
    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaGavetasSettings Clone() => new()
    {
        Numeric = new Dictionary<string, float>(Numeric, StringComparer.Ordinal),
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
