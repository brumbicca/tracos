using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModulePartEditServiceTests
{
    [Fact]
    public void TrySetFaceOffset_MantemValorAbsoluto_SemSomarDeNovo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        module.RebuildMesh(definition);

        const string partLabel = "Porta porta-1";
        Assert.True(module.Mesh.Faces.Any(f => f.Label == partLabel) ||
                    module.Mesh.Faces.Any(f => f.Label.Contains("Porta", StringComparison.OrdinalIgnoreCase)));

        string label = module.Mesh.Faces
            .Select(f => f.Label)
            .First(l => l.Contains("Porta", StringComparison.OrdinalIgnoreCase));

        var handle = new PartHandle(PartHandleAxis.Width, Positive: true);

        Assert.True(ModulePartEditService.TrySetFaceOffset(module, label, handle, -150f, out _));
        Assert.Equal(-150f, ModulePartEditService.GetFaceOffset(module, label, handle), 1);

        // Reaplicar o mesmo valor no painel não deve acumular.
        Assert.True(ModulePartEditService.TrySetFaceOffset(module, label, handle, -150f, out _));
        Assert.Equal(-150f, ModulePartEditService.GetFaceOffset(module, label, handle), 1);

        Assert.True(ModulePartEditService.TrySetFaceOffset(module, label, handle, -200f, out _));
        Assert.Equal(-200f, ModulePartEditService.GetFaceOffset(module, label, handle), 1);

        Assert.Equal(-200f, ModulePartEditService.GetDisplayOffsetForAxis(
            module, label, PartHandleAxis.Width, preferredPositive: true), 1);
    }
}
