namespace Tracos3DStudio;

/// <summary>
/// Furos minifix básicos (sistema 15): cabo Ø5 e excêntrico Ø15 em laterais e horizontais.
/// </summary>
public static class MinifixDrillingService
{
    public const float DowelDiameterMm = 5f;
    public const float DowelDepthMm = 10f;
    public const float CamDiameterMm = 15f;
    public const float CamDepthMm = 13f;
    public const float FrontOffsetMm = 37f;
    public const float EndMarginMm = 50f;
    public const float MidHeightThresholdMm = 900f;

    public static IReadOnlyList<DrillHole> Calculate(PartPiece piece) =>
        piece.Name switch
        {
            "Lateral" => CalculateLateral(piece),
            "Base inferior" or "Tampo interno" or "Prateleira" => CalculateHorizontal(piece),
            _ => Array.Empty<DrillHole>()
        };

    public static IReadOnlyList<DrillHole> CalculateLateral(PartPiece piece)
    {
        float depth = piece.LengthMm;
        float height = piece.WidthMm;

        if (depth <= FrontOffsetMm + EndMarginMm || height <= EndMarginMm * 2)
            return Array.Empty<DrillHole>();

        var yPositions = BuildVerticalPositions(height);
        var holes = new List<DrillHole>();

        foreach (float y in yPositions)
        {
            holes.Add(new DrillHole
            {
                Kind = DrillHoleKind.MinifixCam,
                Edge = DrillHoleEdge.Front,
                PosXmm = FrontOffsetMm,
                PosYmm = y,
                DiameterMm = CamDiameterMm,
                DepthMm = CamDepthMm
            });
        }

        return holes;
    }

    public static IReadOnlyList<DrillHole> CalculateHorizontal(PartPiece piece)
    {
        float length = piece.LengthMm;
        float depth = piece.WidthMm;

        if (length <= EndMarginMm * 2 || depth <= FrontOffsetMm + 10f)
            return Array.Empty<DrillHole>();

        float y = FrontOffsetMm;
        var xPositions = new List<float> { EndMarginMm };

        if (length > EndMarginMm * 2 + 100f)
            xPositions.Add(length - EndMarginMm);

        return xPositions
            .Select(x => new DrillHole
            {
                Kind = DrillHoleKind.MinifixDowel,
                Edge = DrillHoleEdge.Front,
                PosXmm = x,
                PosYmm = y,
                DiameterMm = DowelDiameterMm,
                DepthMm = DowelDepthMm
            })
            .ToList();
    }

    private static List<float> BuildVerticalPositions(float height)
    {
        var positions = new List<float> { EndMarginMm };

        if (height >= MidHeightThresholdMm)
            positions.Add(height / 2f);

        float top = height - EndMarginMm;

        if (top > EndMarginMm + 1f)
            positions.Add(top);

        return positions;
    }
}
