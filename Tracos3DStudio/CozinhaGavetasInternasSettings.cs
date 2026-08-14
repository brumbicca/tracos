namespace Tracos3DStudio;

/// <summary>Configurações de Gavetas Internas | Auxiliares (Cozinhas) — paridade Promob.</summary>
public sealed class CozinhaGavetasInternasSettings
{
    public Dictionary<string, string> Choice { get; set; } = new(StringComparer.Ordinal);

    public CozinhaGavetasInternasSettings Clone() => new()
    {
        Choice = new Dictionary<string, string>(Choice, StringComparer.Ordinal)
    };
}
