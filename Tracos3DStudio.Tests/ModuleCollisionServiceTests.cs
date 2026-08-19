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
    public void WouldCollide_CantoReto_NaoCriaVaoNoLadoDaPorta()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 30f;

        var cornerDefinition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var corner = ModuleCatalog.CreateInstance(cornerDefinition.Id, Vector3.Zero);
        corner.SetDimensions(950f, 720f, 550f, cornerDefinition, settings, respectCatalogLimits: false);

        var next = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(950f, 0f, 0f));
        var wallId = Guid.NewGuid();
        corner.AttachedWallId = wallId;
        next.AttachedWallId = wallId;
        corner.DistanceAlongWall = 475f;
        next.DistanceAlongWall = 1350f;

        Assert.False(ModuleCollisionService.WouldCollide(next, [corner]));
    }

    [Fact]
    public void WouldCollide_CantoRetoReservaAfastamentoTambemNoLadoEsquerdo()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 30f;

        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var corner = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        corner.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);
        var left = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(-790f, 0f, 0f));
        var wallId = Guid.NewGuid();
        corner.AttachedWallId = wallId;
        left.AttachedWallId = wallId;

        // O balcão ocupa até X=10. O envelope antigo começava em X=30 e
        // deixava passar; o espaço nominal do canto começa em X=0.
        Assert.True(ModuleCollisionService.WouldCollide(left, [corner]));
    }

    [Fact]
    public void WouldCollide_CantoRetoComModuloDaOutraParede_DetectaSobreposicao()
    {
        var corner = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        var perpendicular = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(400f, 0f, 100f));
        corner.AttachedWallId = Guid.NewGuid();
        perpendicular.AttachedWallId = Guid.NewGuid();

        Assert.True(ModuleCollisionService.WouldCollide(perpendicular, [corner]));
        var colliding = ModuleCollisionService.FindCollidingModuleIds([corner, perpendicular]);
        Assert.Contains(corner.Id, colliding);
        Assert.Contains(perpendicular.Id, colliding);
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

    [Fact]
    public void WouldCollide_CantoObliquo_VerificaModuloDaParedePerpendicular()
    {
        var wallA = Guid.NewGuid();
        var wallB = Guid.NewGuid();
        var corner = ModuleCatalog.CreateInstance("canto-obliquo-1p-900", Vector3.Zero);
        corner.AttachedWallId = wallA;

        var other = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(600f, 0f, 600f));
        other.AttachedWallId = wallB;

        Assert.True(ModuleCollisionService.WouldCollide(corner, [other]));

        other.Position = new Vector3(900f, 0f, 900f);
        Assert.False(ModuleCollisionService.WouldCollide(corner, [other]));
    }
}
