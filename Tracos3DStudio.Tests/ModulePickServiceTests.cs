using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ModulePickServiceTests
{
    [Fact]
    public void TryRayBounds_RaioCentral_AcertaModulo()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(1000, 0, 500));

        var origin = new Vector3(1400, -100, 775);
        var direction = Vector3.UnitY;

        Assert.True(ModulePickService.TryRayBounds(origin, direction, module, out float t));
        Assert.True(t > 0f);
    }

    [Fact]
    public void TryPickRay_DoisModulos_RetornaMaisProximo()
    {
        var near = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(0, 0, 0));
        var far = ModuleCatalog.CreateInstance("gaveteiro", new Vector3(3000, 0, 0));
        var modules = new List<ModuleInstance> { near, far };

        var origin = new Vector3(400, 400, 1200);
        var direction = Vector3.Normalize(new Vector3(400, 400, 275) - origin);

        Assert.True(ModulePickService.TryPickRay(origin, direction, modules, out var picked, out _));
        Assert.Equal(near.Id, picked!.Id);
    }
}
