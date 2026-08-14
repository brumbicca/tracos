using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SceneModuleVisibilityServiceTests
{
    [Fact]
    public void GetVisibleState_RetornaNullQuandoVazio()
    {
        Assert.Null(SceneModuleVisibilityService.GetVisibleState([]));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(true, false, null)]
    public void GetVisibleState_TriState(bool first, bool second, bool? expected)
    {
        var modules = new[]
        {
            CreateModule(isVisible: first),
            CreateModule(isVisible: second)
        };

        Assert.Equal(expected, SceneModuleVisibilityService.GetVisibleState(modules));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    public void CanDeleteSelection_RespeitaBloqueio(bool firstLocked, bool secondLocked, bool expected)
    {
        var modules = new[]
        {
            CreateModule(isLocked: firstLocked),
            CreateModule(isLocked: secondLocked)
        };

        Assert.Equal(expected, SceneModuleVisibilityService.CanDeleteSelection(modules));
    }

    [Theory]
    [InlineData(true, false, "")]
    [InlineData(false, false, " (oculto)")]
    [InlineData(true, true, " (bloqueado)")]
    [InlineData(false, true, " (oculto, bloqueado)")]
    public void FormatListStatusSuffix(bool isVisible, bool isLocked, string expected)
    {
        var module = CreateModule(isVisible: isVisible, isLocked: isLocked);
        Assert.Equal(expected, SceneModuleVisibilityService.FormatListStatusSuffix(module));
    }

    private static ModuleInstance CreateModule(bool isVisible = true, bool isLocked = false) =>
        new()
        {
            DefinitionId = "balcao-2-portas",
            IsVisible = isVisible,
            IsLocked = isLocked
        };
}
