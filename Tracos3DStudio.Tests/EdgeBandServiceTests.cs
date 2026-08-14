using Xunit;

namespace Tracos3DStudio.Tests;

public class EdgeBandServiceTests
{
    [Theory]
    [InlineData("Frente porta 1", "4 lados")]
    [InlineData("Frente gaveta 2", "4 lados")]
    [InlineData("Lateral", "Frente + topo")]
    [InlineData("Base inferior", "Frente")]
    [InlineData("Fundo", null)]
    public void ComputeEdgeBand_AplicaRegras(string pieceName, string? expected)
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Teste",
            Name = pieceName,
            LengthMm = 100,
            WidthMm = 100,
            ThicknessMm = 18,
            MaterialName = "MDF Branco"
        };

        Assert.Equal(expected, EdgeBandService.ComputeEdgeBand(piece));
    }
}
