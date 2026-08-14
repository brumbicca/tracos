using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class CameraControllerTests
{
    [Fact]
    public void FrameOnBounds_CentralizaE_AjustaDistancia()
    {
        var camera = new CameraController();
        var min = new Vector3(1000f, 0f, 2000f);
        var max = new Vector3(1900f, 860f, 2560f);

        camera.FrameOnBounds(min, max);

        Assert.Equal(1450f, camera.Target.X, precision: 1);
        Assert.Equal(430f, camera.Target.Y, precision: 1);
        Assert.Equal(2280f, camera.Target.Z, precision: 1);
        Assert.True(camera.Distance >= 1200f);
        Assert.True(camera.Distance <= 20000f);
    }

    [Fact]
    public void Zoom_PermiteAproximarDetalhesDeEngenharia()
    {
        var camera = new CameraController { Distance = 800f };

        for (int i = 0; i < 50; i++)
            camera.Zoom(120f);

        Assert.True(camera.Distance < 200f,
            $"Zoom deveria passar de 800 mm para close-up; Distance={camera.Distance}");
        Assert.Equal(CameraController.MinZoomDistance, camera.Distance, precision: 1);
    }

    [Fact]
    public void ZoomToward_PuxaAlvoParaOFoco()
    {
        var camera = new CameraController
        {
            Target = new Vector3(0f, 900f, 0f),
            Distance = 5000f
        };
        var focus = new Vector3(2500f, 400f, 2500f);
        float distBefore = Vector3.Distance(camera.Target, focus);

        camera.ZoomToward(120f, focus);

        float distAfter = Vector3.Distance(camera.Target, focus);
        Assert.True(camera.Distance < 5000f);
        Assert.True(distAfter < distBefore,
            $"Target deveria aproximar o foco (antes={distBefore}, depois={distAfter}).");
    }

    [Fact]
    public void PanPerspective_MantemSensibilidadeEmCloseUp()
    {
        var camera = new CameraController
        {
            Distance = CameraController.MinZoomDistance,
            Target = Vector3.Zero,
            Pitch = 30f,
            Yaw = 45f
        };

        camera.PanPerspective(100f, 0f);

        Assert.True(camera.Target.Length > 100f,
            $"Pan em close-up não pode zerar; deslocamento={camera.Target.Length}");
    }

    [Fact]
    public void Zoom_RespeitaLimiteMaximo()
    {
        var camera = new CameraController { Distance = 20000f };

        for (int i = 0; i < 20; i++)
            camera.Zoom(-120f);

        Assert.Equal(CameraController.MaxZoomDistance, camera.Distance, precision: 1);
    }

    [Fact]
    public void Orbit_PermiteOlharAbaixoDoPlano()
    {
        var camera = new CameraController { Pitch = 5f };

        // Arrasto grande para baixo deve levar o pitch a valor negativo (câmera sob o piso).
        camera.Orbit(0f, -400f);

        Assert.True(camera.Pitch < 0f, $"Pitch deveria ser negativo, foi {camera.Pitch}.");
        Assert.True(camera.Pitch >= -85f, $"Pitch deveria respeitar o limite inferior, foi {camera.Pitch}.");
    }

    [Fact]
    public void Orbit_MantemLimiteSuperior()
    {
        var camera = new CameraController { Pitch = 80f };

        camera.Orbit(0f, 400f);

        Assert.True(camera.Pitch <= 85f, $"Pitch deveria respeitar o limite superior, foi {camera.Pitch}.");
    }

    [Fact]
    public void PanTop_MouseParaBaixo_MoveTargetEmZNegativo()
    {
        var camera = new CameraController { Target = Vector3.Zero, Distance = 10000f };

        camera.PanTop(0f, 100f);

        Assert.True(camera.Target.Z < 0f, $"Z deveria diminuir ao arrastar para baixo, foi {camera.Target.Z}.");
        Assert.Equal(0f, camera.Target.X, precision: 3);
    }

    [Fact]
    public void PanOrthographic_Planta_MouseParaBaixo_MoveTargetEmZNegativo()
    {
        var camera = new CameraController
        {
            ViewMode = CameraViewMode.Top,
            Target = Vector3.Zero,
            Distance = 10000f
        };

        camera.PanOrthographic(0f, 100f);

        Assert.True(camera.Target.Z < 0f, $"Z deveria diminuir ao arrastar para baixo, foi {camera.Target.Z}.");
    }

    [Fact]
    public void PanPerspective_MouseVertical_MoveTargetNoEixoY()
    {
        var camera = new CameraController
        {
            Pitch = 35f,
            Yaw = 45f,
            Target = Vector3.Zero,
            Distance = 10000f
        };

        camera.PanPerspective(0f, 200f);

        Assert.NotEqual(0f, camera.Target.Y);
        Assert.True(MathF.Abs(camera.Target.Y) > MathF.Abs(camera.Target.X) * 0.5f,
            "Arrasto vertical em perspectiva deve deslocar principalmente no eixo Y da cena, não como dolly no plano XZ.");
    }
}
