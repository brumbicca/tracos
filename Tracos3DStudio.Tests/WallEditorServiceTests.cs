namespace Tracos3DStudio.Tests;

using Xunit;

public sealed class WallEditorServiceTests
{
    [Fact]
    public void CanSwitchToView_AllowsTopWhenEditorActive()
    {
        Assert.True(WallEditorService.CanSwitchToView(CameraViewMode.Top, editorActive: true));
    }

    [Fact]
    public void CanSwitchToView_BlocksNonTopWhenEditorActive()
    {
        Assert.False(WallEditorService.CanSwitchToView(CameraViewMode.Perspective, editorActive: true));
        Assert.False(WallEditorService.CanSwitchToView(CameraViewMode.Front, editorActive: true));
        Assert.False(WallEditorService.CanSwitchToView(CameraViewMode.Left, editorActive: true));
        Assert.False(WallEditorService.CanSwitchToView(CameraViewMode.Right, editorActive: true));
    }

    [Fact]
    public void CanSwitchToView_AllowsAnyViewWhenEditorInactive()
    {
        Assert.True(WallEditorService.CanSwitchToView(CameraViewMode.Perspective, editorActive: false));
        Assert.True(WallEditorService.CanSwitchToView(CameraViewMode.Top, editorActive: false));
    }

    [Fact]
    public void ShouldHideModulesAndCeiling_OnlyInEditor()
    {
        Assert.True(WallEditorService.ShouldHideModules(editorActive: true));
        Assert.True(WallEditorService.ShouldHideCeiling(editorActive: true));
        Assert.False(WallEditorService.ShouldHideModules(editorActive: false));
        Assert.False(WallEditorService.ShouldHideCeiling(editorActive: false));
    }

    [Fact]
    public void GetViewLabel_UsesEditorLabelWhenActive()
    {
        Assert.Equal("Editor de Paredes (Planta)", WallEditorService.GetViewLabel(true, CameraViewMode.Top, false));
        Assert.Equal("Planta", WallEditorService.GetViewLabel(false, CameraViewMode.Top, false));
    }
}
