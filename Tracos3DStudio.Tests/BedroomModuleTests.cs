using Xunit;

namespace Tracos3DStudio.Tests;

public class BedroomModuleTests
{
    [Fact]
    public void ModuleCatalog_ContemTresModulosDormitorio()
    {
        var bedroom = ModuleCatalog.BuiltIn.Where(m => m.Category == ModuleCategory.Dormitorio).ToList();

        Assert.Equal(3, bedroom.Count);
        Assert.True(ModuleCatalog.TryGet("guarda-roupa-2p", out _));
        Assert.True(ModuleCatalog.TryGet("criado-mudo", out _));
        Assert.True(ModuleCatalog.TryGet("comoda-4g", out _));
    }

    [Fact]
    public void GuardaRoupa_GeraMalhaComFrentes()
    {
        var instance = ModuleCatalog.CreateInstance("guarda-roupa-2p", OpenTK.Mathematics.Vector3.Zero);

        Assert.True(instance.Mesh.Vertices.Count > 0);
        Assert.Equal(2, ModuleCatalog.GetRequired("guarda-roupa-2p").DoorCount);
    }
}
