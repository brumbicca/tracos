using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SceneModuleSelectionServiceTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, true)]
    [InlineData(4, false, true)]
    public void Regras_RenomearSoComUm_ExcluirComUmOuMais(int count, bool canRename, bool canDelete)
    {
        Assert.Equal(canRename, SceneModuleSelectionService.CanRename(count));
        Assert.Equal(canDelete, SceneModuleSelectionService.CanDelete(count));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, null)]
    [InlineData(3, "3 módulos selecionados.")]
    public void FormatMultiSelectHint(int count, string? expected)
    {
        Assert.Equal(expected, SceneModuleSelectionService.FormatMultiSelectHint(count));
    }

    [Fact]
    public void ToggleId_AdicionaERemove_ParidadePromobCtrlClique()
    {
        var set = new HashSet<Guid>();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        Assert.True(SceneModuleSelectionService.ToggleId(set, a));
        Assert.True(SceneModuleSelectionService.ToggleId(set, b));
        Assert.Equal(2, set.Count);

        Assert.False(SceneModuleSelectionService.ToggleId(set, a));
        Assert.Single(set);
        Assert.Contains(b, set);
    }

    [Fact]
    public void NormalizeScreenRect_IndependenteDaOrdem()
    {
        var (minX, minY, maxX, maxY) = SceneModuleSelectionService.NormalizeScreenRect(100, 50, 20, 80);
        Assert.Equal(20, minX);
        Assert.Equal(50, minY);
        Assert.Equal(100, maxX);
        Assert.Equal(80, maxY);
    }

    [Fact]
    public void FindModulesIntersectingScreenRect_DetectaModuloNaCaixa()
    {
        var module = new ModuleInstance
        {
            DefinitionId = "balcao-2-portas",
            Width = 800f,
            Height = 700f,
            Depth = 500f,
            Position = new Vector3(0f, 0f, 0f),
            RotationYDegrees = 0f
        };

        // Câmera ortográfica olhando +Z → origem (0,350,-4000).
        var view = Matrix4.LookAt(
            new Vector3(0f, 350f, -4000f),
            new Vector3(0f, 350f, 0f),
            Vector3.UnitY);
        var projection = Matrix4.CreateOrthographic(4000f, 3000f, 10f, 20000f);
        const int w = 800;
        const int h = 600;

        Assert.True(SceneModuleSelectionService.TryGetModuleScreenBounds(
            module, view, projection, w, h,
            out double minX, out double minY, out double maxX, out double maxY));

        var hitsInside = SceneModuleSelectionService.FindModulesIntersectingScreenRect(
            [module], minX, minY, maxX, maxY, view, projection, w, h);
        Assert.Single(hitsInside);

        var hitsOutside = SceneModuleSelectionService.FindModulesIntersectingScreenRect(
            [module], 0, 0, 10, 10, view, projection, w, h);
        Assert.Empty(hitsOutside);
    }
}
