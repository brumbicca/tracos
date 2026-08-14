using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class GridLayoutService
{
    /// <summary>
    /// Calcula divisões uniformes que preenchem exatamente largura × profundidade do piso.
    /// </summary>
    public static (int Cols, int Rows, float StepX, float StepY) ComputeUniformDivisions(
        Vector2 min,
        Vector2 max,
        float preferredStep)
    {
        float width = MathF.Max(1f, max.X - min.X);
        float height = MathF.Max(1f, max.Y - min.Y);
        float step = MathF.Max(1f, preferredStep);

        int cols = Math.Max(1, (int)MathF.Round(width / step));
        int rows = Math.Max(1, (int)MathF.Round(height / step));

        return (cols, rows, width / cols, height / rows);
    }
}
