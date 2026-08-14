using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class CameraController
{
    /// <summary>Distância mínima (mm) para close-up de engenharia.</summary>
    public const float MinZoomDistance = 120f;
    public const float MaxZoomDistance = 30000f;

    /// <summary>
    /// Referência mínima (mm) para velocidade do pan — evita sensibilidade zero no close-up.
    /// </summary>
    public const float MinPanReferenceDistance = 2000f;

    public float Distance { get; set; } = 7500f;
    public float Yaw { get; set; } = 45f;
    public float Pitch { get; set; } = 32f;
    public Vector3 Target { get; set; } = new(0, 900, 0);
    public CameraViewMode ViewMode { get; set; } = CameraViewMode.Perspective;
    public bool XRayEnabled { get; set; }

    public Matrix4 View { get; private set; }
    public Matrix4 Projection { get; private set; }

    public void FrameOnRoom(IReadOnlyList<WallSegment> walls)
    {
        RoomCameraBounds.Compute(walls, out Vector3 center, out float planExtent, out _);
        Target = center;
        Distance = Math.Clamp(planExtent, 3000f, 20000f);
    }

    public void FrameOnBounds(Vector3 min, Vector3 max)
    {
        Vector3 center = (min + max) * 0.5f;
        float sizeX = max.X - min.X;
        float sizeY = max.Y - min.Y;
        float sizeZ = max.Z - min.Z;
        float extent = MathF.Max(MathF.Max(sizeX, sizeY), sizeZ);

        Target = center;
        Distance = Math.Clamp(MathF.Max(extent * 2.5f, 1200f), 800f, 20000f);
    }

    public void SetupForViewport(int width, int height, bool forceTopView)
    {
        float aspect = width / (float)Math.Max(1, height);

        if (forceTopView)
        {
            SetupOrthographicTop(aspect);
            return;
        }

        switch (ViewMode)
        {
            case CameraViewMode.Top:
                SetupOrthographicTop(aspect);
                break;
            case CameraViewMode.Front:
                SetupOrthographicElevation(aspect, new Vector3(0f, 0f, 1f));
                break;
            case CameraViewMode.Left:
                SetupOrthographicElevation(aspect, new Vector3(-1f, 0f, 0f));
                break;
            case CameraViewMode.Right:
                SetupOrthographicElevation(aspect, new Vector3(1f, 0f, 0f));
                break;
            default:
                SetupPerspective(aspect);
                break;
        }
    }

    public void Orbit(float dx, float dy)
    {
        Yaw -= dx * 0.35f;
        Pitch += dy * 0.35f;
        // Permite orbitar por baixo do plano do piso (pitch negativo) para inspecionar
        // detalhes da engenharia, como no Promob. Limites evitam o gimbal em ±90°.
        Pitch = Math.Clamp(Pitch, -85f, 85f);
    }

    public void PanPerspective(float dx, float dy)
    {
        float speed = PanSpeed();
        float yawRad = MathHelper.DegreesToRadians(Yaw);
        float pitchRad = MathHelper.DegreesToRadians(Pitch);

        Vector3 forward = new(
            MathF.Cos(pitchRad) * MathF.Sin(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Cos(yawRad));

        Vector3 right = Vector3.Cross(forward, Vector3.UnitY);
        if (right.LengthSquared < 1e-8f)
            right = new Vector3(MathF.Cos(yawRad), 0f, -MathF.Sin(yawRad));
        else
            right = Vector3.Normalize(right);

        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        var target = Target;
        target += right * dx * speed;
        target += up * dy * speed;
        Target = target;
    }

    public void PanTop(float dx, float dy)
    {
        float speed = PanSpeed();
        var target = Target;
        target.X -= dx * speed;
        target.Z -= dy * speed;
        Target = target;
    }

    public void PanOrthographic(float dx, float dy)
    {
        float speed = PanSpeed();
        var target = Target;

        switch (ViewMode)
        {
            case CameraViewMode.Top:
                target.X -= dx * speed;
                target.Z -= dy * speed;
                break;
            case CameraViewMode.Front:
                target.X -= dx * speed;
                target.Y += dy * speed;
                break;
            case CameraViewMode.Left:
                target.Z -= dx * speed;
                target.Y += dy * speed;
                break;
            case CameraViewMode.Right:
                target.Z += dx * speed;
                target.Y += dy * speed;
                break;
        }

        Target = target;
    }

    public void Zoom(float delta)
    {
        float zoomFactor = delta > 0 ? 0.88f : 1.12f;
        Distance *= zoomFactor;
        Distance = Math.Clamp(Distance, MinZoomDistance, MaxZoomDistance);
    }

    /// <summary>
    /// Zoom no ponto sob o cursor: aproxima a câmera e puxa o alvo para o foco,
    /// permitindo "entrar" no módulo (comportamento CAD / Promob).
    /// </summary>
    public void ZoomToward(float delta, Vector3 focusPoint)
    {
        float oldDistance = Distance;
        float zoomFactor = delta > 0 ? 0.88f : 1.12f;
        float newDistance = Math.Clamp(oldDistance * zoomFactor, MinZoomDistance, MaxZoomDistance);

        if (MathF.Abs(newDistance - oldDistance) < 0.01f)
            return;

        // Mantém o foco estável na tela: Target caminha em direção ao ponto sob o cursor.
        float t = 1f - (newDistance / oldDistance);
        Target += (focusPoint - Target) * t;
        Distance = newDistance;
    }

    public static string GetViewLabel(CameraViewMode mode, bool xRay)
    {
        string baseLabel = mode switch
        {
            CameraViewMode.Perspective => "Perspectiva",
            CameraViewMode.Top => "Planta",
            CameraViewMode.Front => "Frontal",
            CameraViewMode.Left => "Esquerda",
            CameraViewMode.Right => "Direita",
            _ => "Perspectiva"
        };

        return xRay && mode == CameraViewMode.Perspective ? $"{baseLabel} (Raio X)" : baseLabel;
    }

    private float PanSpeed() => MathF.Max(Distance, MinPanReferenceDistance) * 0.0012f;

    private void SetupOrthographicTop(float aspect)
    {
        float orthoHeight = Distance;
        float orthoWidth = orthoHeight * aspect;

        Projection = Matrix4.CreateOrthographic(orthoWidth, orthoHeight, 10f, 50000f);

        View = Matrix4.LookAt(
            new Vector3(Target.X, 10000f, Target.Z),
            new Vector3(Target.X, 0f, Target.Z),
            new Vector3(0f, 0f, -1f));
    }

    private void SetupOrthographicElevation(float aspect, Vector3 cameraOffset)
    {
        float orthoHeight = Distance;
        float orthoWidth = orthoHeight * aspect;

        Projection = Matrix4.CreateOrthographic(orthoWidth, orthoHeight, 10f, 50000f);

        Vector3 eye = Target + Vector3.Normalize(cameraOffset) * Distance;
        View = Matrix4.LookAt(eye, Target, Vector3.UnitY);
    }

    private void SetupPerspective(float aspect)
    {
        Projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            aspect,
            10f,
            50000f);

        float yawRad = MathHelper.DegreesToRadians(Yaw);
        float pitchRad = MathHelper.DegreesToRadians(Pitch);

        Vector3 cameraPosition = new(
            Target.X + Distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad),
            Target.Y + Distance * MathF.Sin(pitchRad),
            Target.Z + Distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad));

        View = Matrix4.LookAt(cameraPosition, Target, Vector3.UnitY);
    }
}
