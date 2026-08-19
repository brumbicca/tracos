using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ModulePlacementServiceTests
{
    [Fact]
    public void PlaceAgainstWall_ParedeHorizontal_EncostaFundoNaFaceInterna()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(1500, 400),
            wall,
            walls,
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth,
            1500f);

        Assert.True(placement.SnappedToWall);
        Assert.Equal(0f, placement.RotationYDegrees, precision: 1);
        Assert.Equal(150f, placement.Position.Z, precision: 1);
        Assert.Equal(1100f, placement.Position.X, precision: 1);
        Assert.Equal(0f, placement.Position.Y);
    }

    [Fact]
    public void ComputeBackCornerOnInnerFace_AmbienteFechado_EncostaNaFaceVisivel()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(4000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        Vector2 interiorNormal = new(0, 1);
        Vector2 back = ModulePlacementService.ComputeBackCornerOnInnerFace(wall, walls, 1100f, interiorNormal);

        Assert.Equal(1100f, back.X, precision: 1);
        Assert.Equal(150f, back.Y, precision: 1);
    }

    [Fact]
    public void PlaceAgainstWall_AmbienteEmL_FrentePerpendicularNoEsquadro()
    {
        // Duas paredes abertas (L): o centro do ambiente fica na diagonal.
        // A frente do módulo deve ficar perpendicular à parede, não inclinada.
        var wallTop = new WallSegment(new Vector2(0, 0), new Vector2(4500, 0)) { Thickness = 150f };
        var wallLeft = new WallSegment(new Vector2(0, 0), new Vector2(0, 4500)) { Thickness = 150f };
        var walls = new[] { wallTop, wallLeft };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var placementTop = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2000, 400), wallTop, walls,
            definition, definition.DefaultWidth, definition.DefaultDepth, 2000f);

        var placementLeft = ModulePlacementService.PlaceAgainstWall(
            new Vector2(400, 2000), wallLeft, walls,
            definition, definition.DefaultWidth, definition.DefaultDepth, 2000f);

        // Rotação deve ser múltiplo de 90° (esquadro), não um ângulo diagonal.
        AssertMultiploDe90(placementTop.RotationYDegrees);
        AssertMultiploDe90(placementLeft.RotationYDegrees);
    }

    private static void AssertMultiploDe90(float angle)
    {
        float norm = ((angle % 360f) + 360f) % 360f;
        float nearest = MathF.Round(norm / 90f) * 90f;
        Assert.True(MathF.Abs(norm - nearest) < 0.5f, $"Ângulo {angle}° não está no esquadro.");
    }

    [Fact]
    public void PlaceAgainstWall_Aereo_UsaAlturaDeParede()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(3000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("aereo");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(1500, 400),
            wall,
            walls,
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth,
            1500f);

        Assert.Equal(1400f, placement.Position.Y);
    }

    [Fact]
    public void Compute_SemParedes_CentralizaNoClique()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var placement = ModulePlacementService.Compute(
            new Vector2(1000, 800),
            Array.Empty<WallSegment>(),
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth);

        Assert.False(placement.SnappedToWall);
        Assert.Equal(600f, placement.Position.X, precision: 1);
        Assert.Equal(525f, placement.Position.Z, precision: 1);
    }

    [Fact]
    public void ComputeBounds_ComRotacao90_ExpandeAabb()
    {
        var bounds = ModulePlacementService.ComputeBounds(
            new Vector3(0, 0, 0),
            800f,
            850f,
            550f,
            90f);

        Assert.Equal(0f, bounds.Min.X, precision: 1);
        Assert.InRange(bounds.Max.X, 540f, 560f);
        Assert.InRange(bounds.Min.Z, -810f, -790f);
        Assert.Equal(0f, bounds.Max.Z, precision: 1);
        Assert.Equal(850f, bounds.Max.Y, precision: 1);
    }

    [Fact]
    public void TryApplyWallCota_Anterior_ReposicionaNaFaceInterna()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2500, 400),
            wall,
            walls,
            definition,
            1200f,
            definition.DefaultDepth,
            2500f);

        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 1200f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth
        };
        module.ApplyPlacement(
            placement.Position,
            placement.RotationYDegrees,
            definition,
            placement.WallId,
            placement.DistanceAlongWall);

        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Anterior, 2300f, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);
        Assert.NotNull(cotas);
        Assert.Equal(2300f, cotas.Value.Anterior, precision: 1);
        Assert.Equal(1500f, cotas.Value.Posterior, precision: 1);
    }

    [Fact]
    public void TryApplyWallCota_Posterior_ReposicionaNaFaceInterna()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f,
            Height = 2600f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 800f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth
        };

        var initial = ModulePlacementService.PlaceAgainstWall(
            new Vector2(1500, 400),
            wall,
            walls,
            definition,
            module.Width,
            module.Depth,
            1500f);
        module.ApplyPlacement(
            initial.Position,
            initial.RotationYDegrees,
            definition,
            initial.WallId,
            initial.DistanceAlongWall);

        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Posterior, 2700f, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);
        Assert.NotNull(cotas);
        Assert.Equal(2700f, cotas.Value.Posterior, precision: 1);
    }

    [Fact]
    public void FindBackingWall_ModuloSemVinculo_ResolveParedeMaisProxima()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2500, 400),
            wall,
            walls,
            definition,
            definition.DefaultWidth,
            definition.DefaultDepth,
            2500f);

        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = definition.DefaultWidth,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth,
            Position = placement.Position,
            RotationYDegrees = placement.RotationYDegrees
            // AttachedWallId propositalmente NÃO definido (módulo órfão).
        };
        module.RebuildMesh(definition);

        var backing = ModulePlacementService.FindBackingWall(module, walls);
        Assert.NotNull(backing);
        Assert.Equal(wall.Id, backing!.Id);
    }

    [Fact]
    public void ComputeDisplayWallCotas_ModuloSemVinculo_UsaCentroProjetado()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2500, 400),
            wall,
            walls,
            definition,
            800f,
            definition.DefaultDepth,
            2500f);

        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 800f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth,
            Position = placement.Position,
            RotationYDegrees = placement.RotationYDegrees
        };
        module.RebuildMesh(definition);

        var cotas = ModulePlacementService.ComputeDisplayWallCotas(module, wall, walls);

        // Centro em 2500 numa face de 5000, módulo de 800 → 2100 de cada lado.
        Assert.Equal(2100f, cotas.Anterior, precision: 1);
        Assert.Equal(2100f, cotas.Posterior, precision: 1);
    }

    [Fact]
    public void AttachModuleToWall_ModuloSemVinculo_HabilitaEdicaoDeCota()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var placement = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2500, 400),
            wall,
            walls,
            definition,
            800f,
            definition.DefaultDepth,
            2500f);

        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 800f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth,
            Position = placement.Position,
            RotationYDegrees = placement.RotationYDegrees
        };
        module.RebuildMesh(definition);

        ModulePlacementService.AttachModuleToWall(module, wall, walls, definition);
        Assert.Equal(wall.Id, module.AttachedWallId);

        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Anterior, 300f, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);
        Assert.NotNull(cotas);
        Assert.Equal(300f, cotas!.Value.Anterior, precision: 1);
    }

    [Fact]
    public void TryApplyWallCota_EditarHorizontal_PreservaAltura()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f,
            Height = 2600f
        };
        var walls = new[] { wall };

        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var initial = ModulePlacementService.PlaceAgainstWall(
            new Vector2(1500, 400),
            wall,
            walls,
            definition,
            800f,
            definition.DefaultDepth,
            1500f);

        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 800f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth
        };
        module.ApplyPlacement(
            initial.Position,
            initial.RotationYDegrees,
            definition,
            initial.WallId,
            initial.DistanceAlongWall);

        // Sobe o módulo 500 mm do piso.
        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Inferior, 500f, out _));
        Assert.Equal(500f, module.Position.Y, precision: 1);

        // Editar a cota horizontal NÃO pode derrubar o módulo para o piso.
        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Anterior, 300f, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);
        Assert.NotNull(cotas);
        Assert.Equal(300f, cotas!.Value.Anterior, precision: 1);
        Assert.Equal(500f, cotas.Value.Inferior, precision: 1);
        Assert.Equal(500f, module.Position.Y, precision: 1);
    }

    [Fact]
    public void PlaceOnInsertionFace_SemMargem_PermiteEncostarNaExtremidade()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f
        };
        var walls = new[] { wall };
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        float width = 800f;
        float halfWidth = width * 0.5f;

        var placement = ModulePlacementService.PlaceOnInsertionFace(
            wall,
            walls,
            definition,
            width,
            definition.DefaultDepth,
            halfWidth,
            new Vector2(0, 1),
            moduleHeight: definition.DefaultHeight);

        var cotas = ModulePlacementService.ComputeWallCotasFromPlacement(
            wall,
            walls,
            definition,
            width,
            definition.DefaultHeight,
            placement);

        Assert.Equal(0f, cotas.Anterior, precision: 1);
    }

    [Fact]
    public void TryApplyWallCota_ValoresNegativos_ExtrapolamForaDosLimitesDaParede()
    {
        var wall = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0))
        {
            Thickness = 150f,
            Height = 2600f
        };
        var walls = new[] { wall };
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var initial = ModulePlacementService.PlaceAgainstWall(
            new Vector2(2500, 400), wall, walls, definition, 800f, definition.DefaultDepth, 2500f);
        var module = new ModuleInstance
        {
            DefinitionId = definition.Id,
            Width = 800f,
            Height = definition.DefaultHeight,
            Depth = definition.DefaultDepth
        };
        module.ApplyPlacement(initial.Position, initial.RotationYDegrees, definition,
            initial.WallId, initial.DistanceAlongWall);

        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Anterior, -120f, out _));
        Assert.True(ModulePlacementService.TryApplyWallCota(
            module, wall, walls, definition, ModuleCotaAxis.Inferior, -80f, out _));

        var cotas = ModulePlacementService.TryComputeWallCotas(module, wall, walls);
        Assert.NotNull(cotas);
        Assert.Equal(-120f, cotas.Value.Anterior, precision: 1);
        Assert.Equal(-80f, cotas.Value.Inferior, precision: 1);
        Assert.Equal(-80f, module.Position.Y, precision: 1);
    }
}
