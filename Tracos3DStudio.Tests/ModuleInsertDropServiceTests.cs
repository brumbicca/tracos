using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleInsertDropServiceTests
{
    [Fact]
    public void TryInsertFromScreen_SemParedes_Falha()
    {
        var project = new Project();

        bool ok = ModuleInsertDropService.TryInsertFromScreen(
            project,
            "gaveteiro",
            640,
            360,
            1280,
            720,
            Matrix4.Identity,
            Matrix4.Identity,
            Array.Empty<WallPickTarget>(),
            collisionEnabled: true,
            ignoreCollision: false,
            DimensionConfiguratorSettings.CreateDefault(),
            out ModuleInstance? instance,
            out string? error);

        Assert.False(ok);
        Assert.Null(instance);
        Assert.Equal("Desenhe paredes antes de inserir módulos.", error);
    }

    [Fact]
    public void TryInsertFromScreen_ComParedeEAlvo_InsereModulo()
    {
        var project = new Project();
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Height = 2600f,
            Thickness = 150f
        };
        project.Room.AddWall(wall);

        var target = WallPickService.FromSegment(
            wall,
            new Vector2(0, 0),
            new Vector2(3000, 0),
            new Vector2(0, 150),
            new Vector2(3000, 150));

        var camera = new CameraController
        {
            ViewMode = CameraViewMode.Front,
            Target = new Vector3(1500, 850, 400),
            Distance = 5000f
        };
        camera.SetupForViewport(1280, 720, forceTopView: false);

        bool ok = ModuleInsertDropService.TryInsertFromScreen(
            project,
            "balcao-2-portas",
            640,
            360,
            1280,
            720,
            camera.View,
            camera.Projection,
            new[] { target },
            collisionEnabled: true,
            ignoreCollision: false,
            DimensionConfiguratorSettings.CreateDefault(),
            out ModuleInstance? instance,
            out string? error);

        Assert.True(ok, error);
        Assert.NotNull(instance);
        Assert.Single(project.Modules);
        Assert.Equal(150f, instance!.Position.Z, precision: 1);
    }
}
