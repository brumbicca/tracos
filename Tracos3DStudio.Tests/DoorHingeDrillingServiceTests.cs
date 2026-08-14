using Xunit;

namespace Tracos3DStudio.Tests;

public class DoorHingeDrillingServiceTests
{
    [Fact]
    public void Calculate_PortaPadrao_DoisFurosNasExtremidades()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão 2 Portas",
            Name = "Frente porta 1",
            LengthMm = 396f,
            WidthMm = 842f,
            ThicknessMm = 18f,
            MaterialName = "MDF Branco"
        };

        var holes = DoorHingeDrillingService.Calculate(piece);

        Assert.Equal(2, holes.Count);
        Assert.All(holes, h => Assert.Equal(DrillHoleKind.HingeCup, h.Kind));
        Assert.All(holes, h => Assert.Equal(35f, h.DiameterMm));
        Assert.Equal(DrillHoleEdge.Right, holes[0].Edge);
        Assert.Equal(100f, holes[0].PosYmm);
        Assert.Equal(742f, holes[1].PosYmm);
        Assert.Equal(396f - DoorHingeDrillingService.EdgeOffsetMm, holes[0].PosXmm);
    }

    [Fact]
    public void Calculate_Porta2_FurosNoLadoEsquerdo()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão 2 Portas",
            Name = "Frente porta 2",
            LengthMm = 396f,
            WidthMm = 842f,
            ThicknessMm = 18f,
            MaterialName = "MDF Branco"
        };

        var holes = DoorHingeDrillingService.Calculate(piece);

        Assert.Equal(DrillHoleEdge.Left, holes[0].Edge);
        Assert.Equal(DoorHingeDrillingService.EdgeOffsetMm, holes[0].PosXmm);
    }

    [Fact]
    public void Calculate_PortaAlta_TresFuros()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Armário",
            Name = "Frente porta 1",
            LengthMm = 500f,
            WidthMm = 1800f,
            ThicknessMm = 18f,
            MaterialName = "MDF Branco"
        };

        var holes = DoorHingeDrillingService.Calculate(piece);

        Assert.Equal(3, holes.Count);
        Assert.Equal(100f, holes[0].PosYmm);
        Assert.Equal(900f, holes[1].PosYmm, 1);
        Assert.Equal(1700f, holes[2].PosYmm);
    }

    [Fact]
    public void Calculate_Lateral_SemFuros()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão 2 Portas",
            Name = "Lateral",
            LengthMm = 550f,
            WidthMm = 850f,
            ThicknessMm = 18f,
            MaterialName = "MDF Branco"
        };

        Assert.Empty(DoorHingeDrillingService.Calculate(piece));
    }

    [Fact]
    public void PartsListService_PortasIncluemFuros()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        var doorPieces = parts.Items.Where(p => DoorHingeDrillingService.IsDoorPiece(p.Name)).ToList();

        Assert.NotEmpty(doorPieces);
        Assert.All(doorPieces, p => Assert.NotEmpty(p.Holes));
    }
}
