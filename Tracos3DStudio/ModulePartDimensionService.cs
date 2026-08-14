using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Calcula as dimensões (mm) de uma peça individual de um módulo a partir da
/// caixa envolvente das faces com o mesmo <see cref="SelectableFace.Label"/>,
/// medidas nos eixos locais do módulo (X = largura, Y = altura, Z = profundidade).
/// </summary>
public static class ModulePartDimensionService
{
    public static bool TryComputeLocalDimensions(
        ModuleInstance module,
        string partLabel,
        out Vector3 dimensions)
    {
        dimensions = Vector3.Zero;

        if (!TryComputeLocalBounds(module, partLabel, out var min, out var max))
            return false;

        dimensions = new Vector3(
            MathF.Abs(max.X - min.X),
            MathF.Abs(max.Y - min.Y),
            MathF.Abs(max.Z - min.Z));

        return true;
    }

    /// <summary>Caixa envolvente da peça em coordenadas locais do módulo.</summary>
    public static bool TryComputeLocalBounds(
        ModuleInstance module,
        string partLabel,
        out Vector3 min,
        out Vector3 max)
    {
        min = Vector3.Zero;
        max = Vector3.Zero;

        if (string.IsNullOrEmpty(partLabel))
            return false;

        bool any = false;

        foreach (var face in module.Mesh.Faces)
        {
            if (face.Label != partLabel)
                continue;

            foreach (var vertex in face.Vertices)
            {
                Vector3 local = ModulePlacementService.InverseTransformPoint(
                    vertex, module.Position, module.RotationYDegrees);

                if (!any)
                {
                    min = local;
                    max = local;
                    any = true;
                }
                else
                {
                    min = Vector3.ComponentMin(min, local);
                    max = Vector3.ComponentMax(max, local);
                }
            }
        }

        return any;
    }
}
