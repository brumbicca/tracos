namespace Tracos3DStudio;

/// <summary>Faixa horizontal ou vertical na parede (Editor de Faixas Promob — MVP horizontal).</summary>
public sealed class WallBand
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool IsHorizontal { get; set; } = true;

    /// <summary>Para faixa horizontal: base em mm desde o afastamento piso da parede.</summary>
    public float StartMm { get; set; }

    /// <summary>Para faixa horizontal: topo em mm desde o afastamento piso da parede.</summary>
    public float EndMm { get; set; }

    public string? MaterialId { get; set; }
}
