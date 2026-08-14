using Xunit;

namespace Tracos3DStudio.Tests;

public class WallCloseConfirmationTests
{
    [Theory]
    [InlineData(3, true, true)]
    [InlineData(4, true, true)]
    [InlineData(2, true, false)]
    [InlineData(3, false, false)]
    public void ShouldConfirm_QuandoFecharNoPrimeiroVertice(int pointCount, bool closing, bool expected) =>
        Assert.Equal(expected, WallCloseConfirmation.ShouldConfirm(pointCount, closing));
}
