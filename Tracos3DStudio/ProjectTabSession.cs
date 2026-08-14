using System.IO;

namespace Tracos3DStudio;

/// <summary>
/// Uma aba de projeto aberta na janela principal (paridade Promob S3).
/// </summary>
public sealed class ProjectTabSession
{
    private static int _untitledCounter;

    public Guid Id { get; } = Guid.NewGuid();

    public Project Project { get; } = new();

    public string? FilePath { get; set; }

    public bool IsDirty { get; set; }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(FilePath))
            return Path.GetFileNameWithoutExtension(FilePath);

        if (!string.IsNullOrWhiteSpace(Project.Metadata.Name))
            return Project.Metadata.Name;

        return $"Projeto sem título {_untitledCounter}";
    }

    public static void ResetUntitledCounter() => _untitledCounter = 0;

    internal static int AllocateUntitledNumber() => ++_untitledCounter;

    public void ResetToEmpty()
    {
        Project.Clear();
        FilePath = null;
        IsDirty = false;
        Project.Metadata.Name = $"Projeto sem título {AllocateUntitledNumber()}";
    }
}
