namespace Tracos3DStudio;

/// <summary>
/// Regras do Editor de Paredes (Promob P4): vista 2D dedicada em Planta, foco em geometria de paredes.
/// </summary>
public static class WallEditorService
{
    public const string ModeLabel = "Editor de Paredes";

    public static bool CanSwitchToView(CameraViewMode targetMode, bool editorActive) =>
        !editorActive || targetMode == CameraViewMode.Top;

    public static bool ShouldHideModules(bool editorActive) => editorActive;

    public static bool ShouldHideCeiling(bool editorActive) => editorActive;

    public static string GetViewLabel(bool editorActive, CameraViewMode mode, bool xRay)
    {
        if (editorActive)
            return $"{ModeLabel} (Planta)";

        return CameraController.GetViewLabel(mode, xRay);
    }
}
