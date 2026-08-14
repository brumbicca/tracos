using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class ModulePickService
{
    public static bool TryPickRay(
        Vector3 origin,
        Vector3 direction,
        IReadOnlyList<ModuleInstance> modules,
        out ModuleInstance? picked,
        out float distance,
        Guid? preferModuleId = null)
    {
        picked = null;
        distance = float.MaxValue;

        ModuleInstance? closest = null;
        float closestDistance = float.MaxValue;
        ModuleInstance? preferred = null;
        float preferredDistance = float.MaxValue;

        foreach (var module in modules)
        {
            if (!TryRayBounds(origin, direction, module, out float t))
                continue;

            if (preferModuleId.HasValue && module.Id == preferModuleId.Value)
            {
                preferred = module;
                preferredDistance = t;
            }

            if (t >= closestDistance)
                continue;

            closestDistance = t;
            closest = module;
        }

        const float preferEpsilon = 2f;

        if (preferred != null && preferredDistance <= closestDistance + preferEpsilon)
        {
            picked = preferred;
            distance = preferredDistance;
            return true;
        }

        picked = closest;
        distance = closestDistance;
        return picked != null;
    }

    public static bool TryRayBounds(Vector3 origin, Vector3 direction, ModuleInstance module, out float t)
    {
        var (min, max) = module.GetBounds();
        return TryRayAxisAlignedBox(origin, direction, min, max, out t);
    }

    public static bool TryRayAxisAlignedBox(
        Vector3 origin,
        Vector3 direction,
        Vector3 min,
        Vector3 max,
        out float t)
    {
        t = 0f;
        const float epsilon = 0.0001f;

        float tMin = 0f;
        float tMax = float.MaxValue;

        for (int axis = 0; axis < 3; axis++)
        {
            float originComponent = origin[axis];
            float directionComponent = direction[axis];
            float minComponent = min[axis];
            float maxComponent = max[axis];

            if (MathF.Abs(directionComponent) < epsilon)
            {
                if (originComponent < minComponent || originComponent > maxComponent)
                    return false;

                continue;
            }

            float inv = 1f / directionComponent;
            float t1 = (minComponent - originComponent) * inv;
            float t2 = (maxComponent - originComponent) * inv;

            if (t1 > t2)
                (t1, t2) = (t2, t1);

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);

            if (tMin > tMax)
                return false;
        }

        t = tMin;
        if (t < 0f)
            t = tMax;

        return t >= 0f;
    }
}
