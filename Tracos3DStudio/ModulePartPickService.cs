using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Seleção de peça individual dentro de um módulo (grupo). Cada peça do mesh
/// compartilha o mesmo <see cref="SelectableFace.Label"/>, usado como identidade.
/// </summary>
public static class ModulePartPickService
{
    public static bool TryPickPart(
        Vector3 origin,
        Vector3 direction,
        ModuleInstance module,
        out string partLabel,
        out float distance)
    {
        partLabel = string.Empty;
        distance = float.MaxValue;

        foreach (var face in module.Mesh.Faces)
        {
            var v = face.Vertices;
            bool hit = false;
            float t = float.MaxValue;

            if (v.Length == 4)
                hit = Geometry3D.TryRayQuadIntersect(origin, direction, v[0], v[1], v[2], v[3], out t, out _);
            else if (v.Length == 3)
                hit = Geometry3D.TryRayTriangleIntersect(origin, direction, v[0], v[1], v[2], out t);

            if (hit && t < distance)
            {
                distance = t;
                partLabel = face.Label;
            }
        }

        return distance < float.MaxValue && !string.IsNullOrEmpty(partLabel);
    }
}
