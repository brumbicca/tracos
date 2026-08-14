namespace Tracos3DStudio;

/// <summary>
/// Silhueta / tipo de montagem 3D — paridade visual com ícones do Promob Plus (Cozinhas → Inferiores).
/// </summary>
public enum ModuleShapeKind
{
    /// <summary>Caixote retangular padrão com frentes.</summary>
    Standard = 0,

    /// <summary>Canto reto cego — porta na perna esquerda.</summary>
    BlindCornerLeft,

    /// <summary>Canto reto cego — porta na perna direita.</summary>
    BlindCornerRight,

    /// <summary>Canto em L — perna esquerda.</summary>
    CornerLLeft,

    /// <summary>Canto em L — perna direita.</summary>
    CornerLRight,

    /// <summary>Canto oblíquo / diagonal de frente.</summary>
    Oblique,

    /// <summary>Frente curva (balcão/gaveteiro curvo).</summary>
    CurvedFront,

    /// <summary>Portas bifold (dobradiça em painéis).</summary>
    Bifold,

    /// <summary>Coluna vertical entre/nas portas (Especiais).</summary>
    ColumnDoors,

    /// <summary>Módulo estreito extrator (toalheiro, temperos, porta-latas).</summary>
    PullOutNarrow,

    /// <summary>Adega (prateleiras densas / furos).</summary>
    WineRack,

    /// <summary>Nicho de eletro (forno, lava-louça, fogão).</summary>
    ApplianceBay,

    /// <summary>Terminal diagonal / chanfro / Z na ponta.</summary>
    EndDiagonal,
    EndCurved,
    EndChamfer,
    EndZ,

    /// <summary>Cantoneira aberta (prateleiras sem porta).</summary>
    OpenCornerShelves,

    /// <summary>Painel de fechamento / complemento.</summary>
    Filler
}
