namespace Tracos3DStudio;

public sealed class ViewportCaptureRequest
{
    public float Scale { get; init; } = 1f;

    public bool PresentationOnly { get; init; }

    public int TargetMinWidthPx { get; init; } = 1920;
}
