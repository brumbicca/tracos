using Xunit;

namespace Tracos3DStudio.Tests;

public class Phase5AcceptanceTests
{
    [Fact]
    public void CozinhaEmL_PlanoCorte_ExecutavelComAproveitamento()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var plan = CutPlanService.Build(project);

        Assert.True(plan.TotalSheets >= 1);
        Assert.True(plan.OverallUtilizationPercent is > 0 and <= 100);
        Assert.All(plan.Sheets, sheet => Assert.True(sheet.UtilizationPercent > 0));
    }

    [Fact]
    public void CozinhaEmL_FrentesTemFitaDeBorda()
    {
        var project = Phase2AcceptanceTests.BuildKitchenLProject();
        var plan = CutPlanService.Build(project);

        var fronts = plan.Sheets
            .SelectMany(s => s.Placements)
            .Where(p => p.Piece.PieceName.StartsWith("Frente", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(fronts);
        Assert.All(fronts, p => Assert.Equal("4 lados", p.Piece.EdgeBand));
    }
}
