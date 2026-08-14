using OpenTK.Mathematics;

namespace Tracos3DStudio;

public enum PartHandleAxis
{
    Width,
    Height,
    Depth
}

/// <summary>
/// Seta de dimensão de uma peça: eixo (largura/altura/profundidade) e sentido.
/// A face oposta à seta é o ponto de referência (lado que permanece fixo).
/// </summary>
public readonly record struct PartHandle(PartHandleAxis Axis, bool Positive);

/// <summary>
/// Posiciona e seleciona (ray picking) as setas de dimensão da peça selecionada.
/// </summary>
public static class ModulePartHandleService
{
    /// <summary>Deslocamento da seta para fora da face, em mm.</summary>
    public const float HandleOffsetMm = 70f;

    /// <summary>Meio-comprimento da haste da seta, em mm.</summary>
    public const float HandleHalfLengthMm = 55f;

    /// <summary>Tolerância de clique (distância do raio ao centro da seta), em mm.</summary>
    public const float PickToleranceMm = 130f;

    public static IEnumerable<PartHandle> AllHandles()
    {
        yield return new PartHandle(PartHandleAxis.Width, true);
        yield return new PartHandle(PartHandleAxis.Width, false);
        yield return new PartHandle(PartHandleAxis.Height, true);
        yield return new PartHandle(PartHandleAxis.Height, false);
        yield return new PartHandle(PartHandleAxis.Depth, true);
        yield return new PartHandle(PartHandleAxis.Depth, false);
    }

    /// <summary>Centro da seta (em mundo) para a peça, ou falso se a peça não existe.</summary>
    public static bool TryGetHandleCenter(
        ModuleInstance module,
        string partLabel,
        PartHandle handle,
        out Vector3 worldCenter)
    {
        worldCenter = Vector3.Zero;

        if (!ModulePartDimensionService.TryComputeLocalBounds(module, partLabel, out var min, out var max))
            return false;

        Vector3 center = (min + max) * 0.5f;
        Vector3 local = center;

        switch (handle.Axis)
        {
            case PartHandleAxis.Width:
                local.X = handle.Positive ? max.X + HandleOffsetMm : min.X - HandleOffsetMm;
                break;
            case PartHandleAxis.Height:
                local.Y = handle.Positive ? max.Y + HandleOffsetMm : min.Y - HandleOffsetMm;
                break;
            case PartHandleAxis.Depth:
                local.Z = handle.Positive ? max.Z + HandleOffsetMm : min.Z - HandleOffsetMm;
                break;
        }

        worldCenter = ModulePlacementService.TransformLocalPoint(local, module.Position, module.RotationYDegrees);
        return true;
    }

    /// <summary>Extremidades (mundo) da haste da seta, para desenho.</summary>
    public static bool TryGetHandleSegment(
        ModuleInstance module,
        string partLabel,
        PartHandle handle,
        out Vector3 worldA,
        out Vector3 worldB)
    {
        worldA = Vector3.Zero;
        worldB = Vector3.Zero;

        if (!ModulePartDimensionService.TryComputeLocalBounds(module, partLabel, out var min, out var max))
            return false;

        Vector3 center = (min + max) * 0.5f;
        Vector3 a = center;
        Vector3 b = center;

        switch (handle.Axis)
        {
            case PartHandleAxis.Width:
                float cx = handle.Positive ? max.X + HandleOffsetMm : min.X - HandleOffsetMm;
                a.X = cx - HandleHalfLengthMm;
                b.X = cx + HandleHalfLengthMm;
                break;
            case PartHandleAxis.Height:
                float cy = handle.Positive ? max.Y + HandleOffsetMm : min.Y - HandleOffsetMm;
                a.Y = cy - HandleHalfLengthMm;
                b.Y = cy + HandleHalfLengthMm;
                break;
            case PartHandleAxis.Depth:
                float cz = handle.Positive ? max.Z + HandleOffsetMm : min.Z - HandleOffsetMm;
                a.Z = cz - HandleHalfLengthMm;
                b.Z = cz + HandleHalfLengthMm;
                break;
        }

        worldA = ModulePlacementService.TransformLocalPoint(a, module.Position, module.RotationYDegrees);
        worldB = ModulePlacementService.TransformLocalPoint(b, module.Position, module.RotationYDegrees);
        return true;
    }

    /// <summary>Seleciona a seta cujo centro esteja mais próximo do raio (dentro da tolerância).</summary>
    public static bool TryPickHandle(
        Vector3 origin,
        Vector3 direction,
        ModuleInstance module,
        string partLabel,
        out PartHandle picked)
    {
        picked = default;
        float bestDist = PickToleranceMm;
        bool found = false;

        Vector3 dir = direction.LengthSquared > 1e-6f ? Vector3.Normalize(direction) : direction;

        foreach (var handle in AllHandles())
        {
            if (!TryGetHandleCenter(module, partLabel, handle, out var center))
                continue;

            float t = Vector3.Dot(center - origin, dir);
            if (t <= 0f)
                continue;

            Vector3 closest = origin + dir * t;
            float dist = (center - closest).Length;

            if (dist < bestDist)
            {
                bestDist = dist;
                picked = handle;
                found = true;
            }
        }

        return found;
    }
}
