using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class Geometry3D
{
    public static bool TryCreateWorldRay(
        double mouseX,
        double mouseY,
        double viewportWidth,
        double viewportHeight,
        Matrix4 view,
        Matrix4 projection,
        out Vector3 origin,
        out Vector3 direction)
    {
        origin = Vector3.Zero;
        direction = Vector3.UnitZ;

        if (viewportWidth < 1.0 || viewportHeight < 1.0)
            return false;

        float x = (float)((2.0 * mouseX) / viewportWidth - 1.0);
        float y = (float)(1.0 - (2.0 * mouseY) / viewportHeight);

        var nearPoint = new Vector4(x, y, -1f, 1f);
        var farPoint = new Vector4(x, y, 1f, 1f);

        Matrix4 viewProjection = view * projection;
        Matrix4 inverse = Matrix4.Invert(viewProjection);

        Vector4 nearWorld = Vector4.TransformRow(nearPoint, inverse);
        Vector4 farWorld = Vector4.TransformRow(farPoint, inverse);

        // Usar limiar relativo: far pode ter W muito pequeno (∝ near/far = 10/50000 = 2e-4)
        if (MathF.Abs(nearWorld.W) < 1e-9f || MathF.Abs(farWorld.W) < 1e-9f)
            return false;

        nearWorld /= nearWorld.W;
        farWorld /= farWorld.W;

        origin = nearWorld.Xyz;
        Vector3 farPos = farWorld.Xyz;
        direction = farPos - origin;

        if (direction.LengthSquared < 0.0001f)
            return false;

        direction = Vector3.Normalize(direction);
        return true;
    }

    public static bool TryRayTriangleIntersect(
        Vector3 origin,
        Vector3 direction,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        out float t)
    {
        t = 0f;
        const float epsilon = 0.0001f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (a > -epsilon && a < epsilon)
            return false;

        float f = 1.0f / a;
        Vector3 s = origin - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(direction, q);

        if (v < 0.0f || u + v > 1.0f)
            return false;

        t = f * Vector3.Dot(edge2, q);

        return t > epsilon;
    }

    public static bool TryRayQuadIntersect(
        Vector3 origin,
        Vector3 direction,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        out float t,
        out Vector3 hitPoint)
    {
        hitPoint = Vector3.Zero;
        t = float.MaxValue;

        if (TryRayTriangleIntersect(origin, direction, p0, p1, p2, out float t1) && t1 < t)
            t = t1;

        if (TryRayTriangleIntersect(origin, direction, p0, p2, p3, out float t2) && t2 < t)
            t = t2;

        if (t >= float.MaxValue)
            return false;

        hitPoint = origin + direction * t;
        return true;
    }

    public static Vector2 HitPointToFloor(Vector3 hitPoint)
    {
        return new Vector2(hitPoint.X, hitPoint.Z);
    }

    public static bool TryRayHorizontalPlane(
        Vector3 origin,
        Vector3 direction,
        float planeY,
        out float t,
        out Vector3 hitPoint)
    {
        t = 0f;
        hitPoint = Vector3.Zero;

        if (MathF.Abs(direction.Y) < 1e-6f)
            return false;

        t = (planeY - origin.Y) / direction.Y;

        if (t < 0f)
            return false;

        hitPoint = origin + direction * t;
        return true;
    }

    /// <summary>Projeta ponto 3D para coordenadas de tela (pixels, origem canto superior esquerdo).</summary>
    public static bool TryProjectToScreen(
        Vector3 world,
        Matrix4 view,
        Matrix4 projection,
        int viewportWidth,
        int viewportHeight,
        out double screenX,
        out double screenY,
        out bool inFront)
    {
        screenX = 0;
        screenY = 0;
        inFront = false;

        if (viewportWidth < 1 || viewportHeight < 1)
            return false;

        var clip = Vector4.TransformRow(new Vector4(world, 1f), view * projection);

        if (MathF.Abs(clip.W) < 1e-9f)
            return false;

        float ndcX = clip.X / clip.W;
        float ndcY = clip.Y / clip.W;
        inFront = clip.W > 0f && ndcX >= -1.05f && ndcX <= 1.05f && ndcY >= -1.05f && ndcY <= 1.05f;

        screenX = (ndcX * 0.5f + 0.5f) * viewportWidth;
        screenY = (1f - (ndcY * 0.5f + 0.5f)) * viewportHeight;
        return true;
    }
}
