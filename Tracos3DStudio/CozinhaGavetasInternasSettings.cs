namespace Tracos3DStudio;

/// <summary>Configurações de Gavetas Internas | Auxiliares (Cozinhas) — paridade Promob.</summary>
public sealed class CozinhaGavetasInternasSettings
{
    public Dictionary<string, float> Numeric { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Legado de perfis anteriores; migrado automaticamente para Numeric.</summary>
    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaGavetasInternasSettings Clone() => new()
    {
        Numeric = new Dictionary<string, float>(Numeric, StringComparer.Ordinal),
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
