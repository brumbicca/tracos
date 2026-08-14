namespace Tracos3DStudio;

public enum DrillHoleKind
{
    HingeCup,
    MinifixDowel,
    MinifixCam
}

public enum DrillHoleEdge
{
    Left,
    Right,
    Front,
    Back
}

public sealed class DrillHole
{
    public DrillHoleKind Kind { get; init; }

    public DrillHoleEdge Edge { get; init; }

    public float PosXmm { get; init; }

    public float PosYmm { get; init; }

    public float DiameterMm { get; init; }

    public float DepthMm { get; init; }

    public string Summary => Kind switch
    {
        DrillHoleKind.HingeCup => $"Ø{DiameterMm:0} a {PosYmm:0} mm ({EdgeLabel})",
        DrillHoleKind.MinifixDowel => $"Minifix cabo Ø{DiameterMm:0} ({PosXmm:0},{PosYmm:0})",
        DrillHoleKind.MinifixCam => $"Minifix exc Ø{DiameterMm:0} a {PosYmm:0} mm ({EdgeLabel})",
        _ => $"Ø{DiameterMm:0}"
    };

    private string EdgeLabel => Edge switch
    {
        DrillHoleEdge.Left => "esq.",
        DrillHoleEdge.Right => "dir.",
        DrillHoleEdge.Front => "frente",
        DrillHoleEdge.Back => "fundo",
        _ => ""
    };
}
