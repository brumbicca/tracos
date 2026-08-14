using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ModuleCollisionServiceTests
{
    [Fact]
    public void FindCollidingModuleIds_DoisModulosSobrepostos_DetectaPar()
    {
        var a = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var b = ModuleCatalog.CreateInstance("gaveteiro", new Vector3(100, 0, 50));

        var colliding = ModuleCollisionService.FindCollidingModuleIds([a, b]);

        Assert.Equal(2, colliding.Count);
        Assert.Contains(a.Id, colliding);
        Assert.Contains(b.Id, colliding);
    }

    [Fact]
    public void FindCollidingModuleIds_ModulosSeparados_SemColisao()
    {
        var a = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var b = ModuleCatalog.CreateInstance("gaveteiro", new Vector3(3000, 0, 0));

        var colliding = ModuleCollisionService.FindCollidingModuleIds([a, b]);

        Assert.Empty(colliding);
    }

    [Fact]
    public void WouldCollide_PlacementPreview_DetectaAntesDeInserir()
    {
        var existing = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        bool collides = ModuleCollisionService.WouldCollide(
            new Vector3(200, 0, 0),
            definition.DefaultWidth,
            definition.DefaultHeight,
            definition.DefaultDepth,
            0f,
            [existing]);

        Assert.True(collides);
    }

    [Fact]
    public void FindCollidingModuleIds_ModulosEncostadosNaMesmaParede_SemColisao()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        float width = definition.DefaultWidth;

        var a = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var b = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(width, 0, 0));

        var wallId = Guid.NewGuid();
        a.AttachedWallId = wallId;
        b.AttachedWallId = wallId;
        a.DistanceAlongWall = width * 0.5f;
        b.DistanceAlongWall = width + width * 0.5f;

        var colliding = ModuleCollisionService.FindCollidingModuleIds([a, b]);

        Assert.Empty(colliding);
    }

    [Fact]
    public void WouldCollide_ModulosEmParedesDiferentes_IgnoraPar()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var a = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        a.AttachedWallId = Guid.NewGuid();

        var wallB = Guid.NewGuid();

        bool collides = ModuleCollisionService.WouldCollide(
            Vector3.Zero,
            definition.DefaultWidth,
            definition.DefaultHeight,
            definition.DefaultDepth,
            0f,
            [a],
            candidateWallId: wallB);

        Assert.False(collides);
    }

    [Fact]
    public void WouldCollide_AereoSobreBalcao_MesmaFaixaHorizontal_SemColisao()
    {
        var wallId = Guid.NewGuid();
        var balcao = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        balcao.AttachedWallId = wallId;
        balcao.DistanceAlongWall = 400f;

        var aereo = ModuleCatalog.CreateInstance("aereo", new Vector3(0, 1400, 0));
        aereo.AttachedWallId = wallId;
        aereo.DistanceAlongWall = 400f;

        Assert.False(ModuleCollisionService.WouldCollide(aereo, [balcao]));
    }

    [Fact]
    public void ApplyEdgeSnaps_AproximaTopo_AlinhaTopoComTopo()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 1200f;
        other.Height = 850f;

        (float along, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            1200f,
            100f,
            800f,
            700f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [other],
            5f);

        Assert.Equal(150f, mountY, precision: 1);
        Assert.Equal(1200f, along, precision: 1);
    }

    [Fact]
    public void ApplyEdgeSnaps_Descendo_AlinhaBaseComBase()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 1200f;
        other.Height = 850f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            2000f,
            45f,
            800f,
            700f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [other],
            -8f);

        Assert.Equal(0f, mountY, precision: 1);
    }

    [Fact]
    public void ApplyEdgeSnaps_Subindo_AlinhaTopoComTopo_VizinhoLado()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 400f;
        other.Height = 700f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            1200f,
            50f,
            800f,
            700f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [other],
            10f);

        Assert.Equal(0f, mountY, precision: 1);
    }

    [Fact]
    public void ApplyEdgeSnaps_DentroDaFaixaAmpliada_AlinhaBaseComBase()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 400f;
        other.Height = 700f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            1500f,
            220f,
            1200f,
            700f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [other],
            -5f);

        Assert.Equal(0f, mountY, precision: 1);
    }

    [Fact]
    public void ResolveVerticalDirection_RaySobeMouseDesce_PrefereMouse()
    {
        Assert.True(ModuleWallFaceService.ResolveVerticalDirection(5f, 8f) < 0f);
    }

    [Fact]
    public void ApplyEdgeSnaps_Descendo_AtraiBaseNoPiso()
    {
        var wallId = Guid.NewGuid();

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            2000f,
            420f,
            800f,
            850f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [],
            -10f,
            wallFloorY: 0f);

        Assert.Equal(0f, mountY, precision: 1);
    }

    [Fact]
    public void ResolveVerticalDirection_MouseParaCima_RetornaPositivo()
    {
        Assert.True(ModuleWallFaceService.ResolveVerticalDirection(0f, -8f) > 0f);
    }

    [Fact]
    public void ApplyEdgeSnaps_AlturasDiferentes_AlinhaTopoComTopo_Promob()
    {
        var wallId = Guid.NewGuid();
        var tall = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        tall.AttachedWallId = wallId;
        tall.DistanceAlongWall = 400f;
        tall.Height = 820f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            1200f,
            200f,
            800f,
            500f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [tall],
            12f);

        Assert.Equal(320f, mountY, precision: 1);
    }

    [Fact]
    public void ApplyEdgeSnaps_EncostadoLateral_AproximaBase_AlinhaBase()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 400f;
        other.Height = 820f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            1200f,
            280f,
            800f,
            600f,
            wallId,
            isWallMounted: false,
            movingModuleId: Guid.NewGuid(),
            [other],
            -6f);

        Assert.Equal(0f, mountY, precision: 1);
    }

    [Fact]
    public void ApplyEdgeSnaps_DescendoAposTopoAlinhado_NaoPrendeNoTopo()
    {
        var wallId = Guid.NewGuid();
        var other = ModuleCatalog.CreateInstance("aereo", new Vector3(0, 800, 0));
        other.AttachedWallId = wallId;
        other.DistanceAlongWall = 1200f;
        other.Height = 700f;

        (float _, float mountY) = ModuleWallFaceService.ApplyEdgeSnaps(
            2000f,
            740f,
            800f,
            700f,
            wallId,
            isWallMounted: true,
            movingModuleId: Guid.NewGuid(),
            [other],
            -12f);

        Assert.Equal(740f, mountY, precision: 1);
    }

    [Fact]
    public void WouldCollide_BalcõesEncostadosLateralmente_DiferenteAltura_SemColisao()
    {
        var wallId = Guid.NewGuid();
        var left = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        left.AttachedWallId = wallId;
        left.DistanceAlongWall = 400f;

        var right = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(0, 589, 0));
        right.AttachedWallId = wallId;
        right.DistanceAlongWall = 1200f;

        Assert.False(ModuleCollisionService.WouldCollide(right, [left]));

        right.Position = new Vector3(right.Position.X, 400f, right.Position.Z);
        Assert.False(ModuleCollisionService.WouldCollide(right, [left]));

        right.Position = new Vector3(right.Position.X, 0f, right.Position.Z);
        Assert.False(ModuleCollisionService.WouldCollide(right, [left]));
    }

    [Fact]
    public void WouldCollide_AereosEncostadosLadoALado_DescerUm_SemColisao()
    {
        var wallId = Guid.NewGuid();
        var left = ModuleCatalog.CreateInstance("aereo", new Vector3(0, 800, 0));
        left.AttachedWallId = wallId;
        left.DistanceAlongWall = 1100f;

        var right = ModuleCatalog.CreateInstance("aereo", new Vector3(0, 600, 0));
        right.AttachedWallId = wallId;
        right.DistanceAlongWall = 1900f;

        Assert.False(ModuleCollisionService.WouldCollide(right, [left]));
    }
}
