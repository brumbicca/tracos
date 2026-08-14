namespace Tracos3DStudio;

public static class CabinetDrillingService
{
    public static IReadOnlyList<DrillHole> Calculate(PartPiece piece)
    {
        var pattern = piece.DrillingPattern ?? ModulationDrillingPattern.Auto;

        return pattern switch
        {
            ModulationDrillingPattern.None => Array.Empty<DrillHole>(),
            ModulationDrillingPattern.Lateral => MinifixDrillingService.CalculateLateral(piece),
            ModulationDrillingPattern.Horizontal => MinifixDrillingService.CalculateHorizontal(piece),
            ModulationDrillingPattern.HingeDoor => DoorHingeDrillingService.Calculate(piece),
            _ => CalculateAuto(piece)
        };
    }

    private static IReadOnlyList<DrillHole> CalculateAuto(PartPiece piece)
    {
        var holes = new List<DrillHole>();
        holes.AddRange(DoorHingeDrillingService.Calculate(piece));
        holes.AddRange(MinifixDrillingService.Calculate(piece));
        return holes;
    }
}
