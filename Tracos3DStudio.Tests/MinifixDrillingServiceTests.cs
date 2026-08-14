using Xunit;

namespace Tracos3DStudio.Tests;

public class MinifixDrillingServiceTests
{
    [Fact]
    public void Calculate_Lateral_GeraExcetricos()
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

        var holes = MinifixDrillingService.Calculate(piece);

        Assert.Equal(2, holes.Count);
        Assert.All(holes, h => Assert.Equal(DrillHoleKind.MinifixCam, h.Kind));
        Assert.All(holes, h => Assert.Equal(15f, h.DiameterMm));
    }

    [Fact]
    public void Calculate_BaseInferior_GeraCabos()
    {
        var piece = new PartPiece
        {
            ModuleId = Guid.NewGuid(),
            ModuleName = "Balcão 2 Portas",
            Name = "Base inferior",
            LengthMm = 764f,
            WidthMm = 544f,
            ThicknessMm = 18f,
            MaterialName = "MDF Branco"
        };

        var holes = MinifixDrillingService.Calculate(piece);

        Assert.Equal(2, holes.Count);
        Assert.All(holes, h => Assert.Equal(DrillHoleKind.MinifixDowel, h.Kind));
    }

    [Fact]
    public void PartsListService_LateraisIncluemMinifix()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var parts = PartsListService.Build(project);
        var laterals = parts.Items.Where(p => p.Name == "Lateral").ToList();

        Assert.NotEmpty(laterals);
        Assert.All(laterals, p => Assert.Contains(p.Holes, h => h.Kind == DrillHoleKind.MinifixCam));
    }
}
