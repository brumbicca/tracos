namespace Tracos3DStudio;

public static class EdgeBandService
{
    public static string? ComputeEdgeBand(PartPiece piece)
    {
        if (piece.EdgeBandingSpec != null)
            return FormatFromSpec(piece.EdgeBandingSpec);

        return ComputeLegacyByName(piece.Name);
    }

    public static string? FormatFromSpec(ModulationEdgeBanding spec)
    {
        if (spec.Front && spec.Back && spec.Top && spec.Bottom)
            return "4 lados";

        if (spec.Front && spec.Top && !spec.Back && !spec.Bottom)
            return "Frente + topo";

        if (spec.Front && !spec.Back && !spec.Top && !spec.Bottom)
            return "Frente";

        var parts = new List<string>(4);
        if (spec.Front) parts.Add("Frente");
        if (spec.Back) parts.Add("Fundo");
        if (spec.Top) parts.Add("Topo");
        if (spec.Bottom) parts.Add("Base");

        return parts.Count > 0 ? string.Join(" + ", parts) : null;
    }

    private static string? ComputeLegacyByName(string pieceName)
    {
        if (pieceName.StartsWith("Frente", StringComparison.OrdinalIgnoreCase))
            return "4 lados";

        if (pieceName.Equals("Lateral", StringComparison.OrdinalIgnoreCase))
            return "Frente + topo";

        if (pieceName is "Base inferior" or "Tampo interno" or "Prateleira")
            return "Frente";

        return null;
    }
}
