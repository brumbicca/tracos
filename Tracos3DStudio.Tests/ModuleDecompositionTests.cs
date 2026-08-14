using Xunit;

namespace Tracos3DStudio.Tests;

public class ModuleDecompositionTests
{
    [Fact]
    public void Balcao2Portas_GeraLateraisBaseFrentes()
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f);

        Assert.Contains(pieces, p => p.Name == "Lateral" && p.Quantity == 2);
        Assert.Contains(pieces, p => p.Name == "Base inferior");
        Assert.Contains(pieces, p => p.Name == "Fundo");
        Assert.Equal(2, pieces.Count(p => p.Name.StartsWith("Frente porta", StringComparison.Ordinal)));
    }

    [Fact]
    public void Gaveteiro_GeraQuatroFrentes()
    {
        var module = ModuleCatalog.CreateInstance("gaveteiro", OpenTK.Mathematics.Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("gaveteiro");

        var pieces = ModuleDecompositionService.Decompose(module, definition, 18f, 6f);

        Assert.Equal(4, pieces.Count(p => p.Name.StartsWith("Frente gaveta", StringComparison.Ordinal)));
        Assert.DoesNotContain(pieces, p => p.Name == "Prateleira");
    }

    [Theory]
    [InlineData(15f)]
    [InlineData(25f)]
    public void EspessuraAlteraDimensoesInternas(float thickness)
    {
        var module = ModuleCatalog.CreateInstance("balcao-2-portas", OpenTK.Mathematics.Vector3.Zero);
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");

        var pieces = ModuleDecompositionService.Decompose(module, definition, thickness, 6f);
        var basePiece = pieces.Single(p => p.Name == "Base inferior");

        Assert.Equal(module.Width - 2 * thickness, basePiece.LengthMm, 1);
        Assert.Equal(thickness, basePiece.ThicknessMm);
    }
}
