using System.IO;

namespace Tracos3DStudio;

/// <summary>
/// Gerencia múltiplas abas de projeto na mesma janela (S3).
/// </summary>
public sealed class ProjectTabWorkspace
{
    private readonly List<ProjectTabSession> _tabs = new();

    public ProjectTabWorkspace()
    {
        var initial = new ProjectTabSession();
        initial.Project.Metadata.Name = $"Projeto sem título {ProjectTabSession.AllocateUntitledNumber()}";
        DimensionConfiguratorService.EnsureProjectSettings(initial.Project);
        _tabs.Add(initial);
        ActiveIndex = 0;
    }

    public IReadOnlyList<ProjectTabSession> Tabs => _tabs;

    public int ActiveIndex { get; private set; }

    public ProjectTabSession Active => _tabs[ActiveIndex];

    public void SyncActive(string? filePath, bool isDirty)
    {
        Active.FilePath = filePath;
        Active.IsDirty = isDirty;
    }

    public ProjectTabSession AddTab()
    {
        var session = new ProjectTabSession();
        session.Project.Metadata.Name = $"Projeto sem título {ProjectTabSession.AllocateUntitledNumber()}";
        DimensionConfiguratorService.EnsureProjectSettings(session.Project);
        _tabs.Add(session);
        return session;
    }

    public void SetActive(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        ActiveIndex = index;
    }

    public bool TryFindByFilePath(string filePath, out int index)
    {
        string normalized = Path.GetFullPath(filePath);

        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].FilePath == null)
                continue;

            if (string.Equals(Path.GetFullPath(_tabs[i].FilePath!), normalized, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            AddTab();
            ActiveIndex = 0;
            return;
        }

        if (ActiveIndex >= _tabs.Count)
            ActiveIndex = _tabs.Count - 1;
        else if (ActiveIndex > index)
            ActiveIndex--;
    }
}
