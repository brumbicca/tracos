using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Seleção de módulos — paridade Promob
/// (https://suporte.promob.com/hc/pt-br/articles/31120597069713).
/// </summary>
public static class SceneModuleSelectionService
{
    public static bool CanRename(int selectedCount) => selectedCount == 1;

    public static bool CanDelete(int selectedCount) => selectedCount > 0;

    public static string? FormatMultiSelectHint(int selectedCount) =>
        selectedCount switch
        {
            <= 1 => null,
            _ => $"{selectedCount} módulos selecionados."
        };

    /// <summary>Ctrl+clique: adiciona ou remove o módulo da seleção (seleção alternada).</summary>
    public static bool ToggleId(ISet<Guid> selectedIds, Guid moduleId)
    {
        if (selectedIds.Remove(moduleId))
            return false;

        selectedIds.Add(moduleId);
        return true;
    }

    public static (double MinX, double MinY, double MaxX, double MaxY) NormalizeScreenRect(
        double x0,
        double y0,
        double x1,
        double y1)
    {
        return (
            Math.Min(x0, x1),
            Math.Min(y0, y1),
            Math.Max(x0, x1),
            Math.Max(y0, y1));
    }

    /// <summary>
    /// Módulos cuja projeção AABB na tela intersecta o retângulo da caixa (Ctrl+arraste).
    /// </summary>
    public static List<ModuleInstance> FindModulesIntersectingScreenRect(
        IReadOnlyList<ModuleInstance> modules,
        double rectMinX,
        double rectMinY,
        double rectMaxX,
        double rectMaxY,
        Matrix4 view,
        Matrix4 projection,
        int viewportWidth,
        int viewportHeight)
    {
        var result = new List<ModuleInstance>();

        if (viewportWidth < 1 || viewportHeight < 1 || rectMaxX - rectMinX < 1 && rectMaxY - rectMinY < 1)
            return result;

        foreach (var module in modules)
        {
            if (!TryGetModuleScreenBounds(
                    module,
                    view,
                    projection,
                    viewportWidth,
                    viewportHeight,
                    out double minX,
                    out double minY,
                    out double maxX,
                    out double maxY))
                continue;

            if (maxX < rectMinX || minX > rectMaxX || maxY < rectMinY || minY > rectMaxY)
                continue;

            result.Add(module);
        }

        return result;
    }

    public static bool TryGetModuleScreenBounds(
        ModuleInstance module,
        Matrix4 view,
        Matrix4 projection,
        int viewportWidth,
        int viewportHeight,
        out double minX,
        out double minY,
        out double maxX,
        out double maxY)
    {
        minX = double.PositiveInfinity;
        minY = double.PositiveInfinity;
        maxX = double.NegativeInfinity;
        maxY = double.NegativeInfinity;

        var (boundsMin, boundsMax) = module.GetBounds();
        bool any = false;

        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) == 0 ? boundsMin.X : boundsMax.X,
                (i & 2) == 0 ? boundsMin.Y : boundsMax.Y,
                (i & 4) == 0 ? boundsMin.Z : boundsMax.Z);

            if (!Geometry3D.TryProjectToScreen(
                    corner,
                    view,
                    projection,
                    viewportWidth,
                    viewportHeight,
                    out double sx,
                    out double sy,
                    out bool inFront) || !inFront)
                continue;

            any = true;
            minX = Math.Min(minX, sx);
            minY = Math.Min(minY, sy);
            maxX = Math.Max(maxX, sx);
            maxY = Math.Max(maxY, sy);
        }

        return any;
    }
}
