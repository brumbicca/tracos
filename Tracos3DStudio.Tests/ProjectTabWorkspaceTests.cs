using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ProjectTabWorkspaceTests
{
    [Fact]
    public void Workspace_StartsWithOneTab()
    {
        var workspace = new ProjectTabWorkspace();

        Assert.Single(workspace.Tabs);
        Assert.Equal(0, workspace.ActiveIndex);
        Assert.NotNull(workspace.Active.Project);
    }

    [Fact]
    public void AddTab_IncreasesCountAndReturnsNewSession()
    {
        var workspace = new ProjectTabWorkspace();
        int initialCount = workspace.Tabs.Count;

        var session = workspace.AddTab();

        Assert.Equal(initialCount + 1, workspace.Tabs.Count);
        Assert.Same(session, workspace.Tabs[^1]);
    }

    [Fact]
    public void SyncActive_PersistsPathAndDirtyOnActiveSession()
    {
        var workspace = new ProjectTabWorkspace();
        const string path = @"C:\Projetos\cozinha.tracos";

        workspace.SyncActive(path, true);

        Assert.Equal(path, workspace.Active.FilePath);
        Assert.True(workspace.Active.IsDirty);
    }

    [Fact]
    public void TryFindByFilePath_MatchesCaseInsensitive()
    {
        var workspace = new ProjectTabWorkspace();
        workspace.Active.FilePath = @"C:\Projetos\Ambiente.tracos";

        Assert.True(workspace.TryFindByFilePath(@"c:\projetos\ambiente.tracos", out int index));
        Assert.Equal(0, index);
    }

    [Fact]
    public void RemoveAt_LastTab_RecreatesEmptyWorkspace()
    {
        var workspace = new ProjectTabWorkspace();
        workspace.SyncActive(null, false);

        workspace.RemoveAt(0);

        Assert.Single(workspace.Tabs);
        Assert.Equal(0, workspace.ActiveIndex);
    }

    [Fact]
    public void Session_GetDisplayName_UsesFileNameWhenSaved()
    {
        ProjectTabSession.ResetUntitledCounter();
        var session = new ProjectTabSession
        {
            FilePath = @"D:\Obras\Cliente A\suite.tracos"
        };

        Assert.Equal("suite", session.GetDisplayName());
    }

    [Fact]
    public void Session_ResetToEmpty_ClearsDirtyAndPath()
    {
        ProjectTabSession.ResetUntitledCounter();
        var session = new ProjectTabSession();
        session.FilePath = @"C:\x.tracos";
        session.IsDirty = true;
        session.Project.Metadata.Name = "Antigo";

        session.ResetToEmpty();

        Assert.Null(session.FilePath);
        Assert.False(session.IsDirty);
        Assert.Empty(session.Project.Room.Walls);
    }
}
