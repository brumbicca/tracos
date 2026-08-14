namespace Tracos3DStudio;

public static class DoorHingeDrillingService
{
    public const float CupDiameterMm = 35f;
    public const float CupDepthMm = 13f;
    public const float EdgeOffsetMm = 22f;
    public const float MarginFromEndMm = 100f;
    public const float MinHeightForThreeHingesMm = 1600f;

    public static IReadOnlyList<DrillHole> Calculate(PartPiece piece)
    {
        if (!TryParseDoorIndex(piece.Name, out int doorIndex))
            return Array.Empty<DrillHole>();

        float doorWidth = piece.LengthMm;
        float doorHeight = piece.WidthMm;

        if (doorWidth <= 0 || doorHeight <= MarginFromEndMm * 2)
            return Array.Empty<DrillHole>();

        var edge = doorIndex % 2 == 1 ? DrillHoleEdge.Right : DrillHoleEdge.Left;
        float posX = edge == DrillHoleEdge.Right
            ? doorWidth - EdgeOffsetMm
            : EdgeOffsetMm;

        var positionsY = BuildHingePositions(doorHeight);

        return positionsY
            .Select(y => new DrillHole
            {
                Kind = DrillHoleKind.HingeCup,
                Edge = edge,
                PosXmm = posX,
                PosYmm = y,
                DiameterMm = CupDiameterMm,
                DepthMm = CupDepthMm
            })
            .ToList();
    }

    public static bool IsDoorPiece(string pieceName) =>
        pieceName.StartsWith("Frente porta", StringComparison.Ordinal);

    private static List<float> BuildHingePositions(float doorHeight)
    {
        var positions = new List<float> { MarginFromEndMm };

        if (doorHeight >= MinHeightForThreeHingesMm)
            positions.Add(doorHeight / 2f);

        float bottom = doorHeight - MarginFromEndMm;

        if (bottom > MarginFromEndMm + 1f)
            positions.Add(bottom);

        return positions;
    }

    private static bool TryParseDoorIndex(string pieceName, out int doorIndex)
    {
        doorIndex = 0;

        if (!IsDoorPiece(pieceName))
            return false;

        int lastSpace = pieceName.LastIndexOf(' ');

        if (lastSpace < 0 || lastSpace >= pieceName.Length - 1)
            return false;

        return int.TryParse(pieceName[(lastSpace + 1)..], out doorIndex) && doorIndex > 0;
    }
}
