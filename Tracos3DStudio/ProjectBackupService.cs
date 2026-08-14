using System.IO;
using System.IO.Compression;

namespace Tracos3DStudio;

public static class ProjectBackupService
{
    public static void ExportZip(
        string projectFilePath,
        string outputZipPath,
        string? libraryFilePath = null)
    {
        if (File.Exists(outputZipPath))
            File.Delete(outputZipPath);

        using var archive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(projectFilePath, Path.GetFileName(projectFilePath));

        string libPath = libraryFilePath ?? LibraryPersistence.DefaultLibraryPath;

        if (File.Exists(libPath))
            archive.CreateEntryFromFile(libPath, Path.GetFileName(libPath));

        var manifest = new BackupManifest
        {
            ProjectFileName = Path.GetFileName(projectFilePath),
            LibraryFileName = File.Exists(libPath) ? Path.GetFileName(libPath) : null,
            CreatedUtc = DateTime.UtcNow
        };

        var entry = archive.CreateEntry("backup-manifest.json");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private sealed class BackupManifest
    {
        public string? ProjectFileName { get; set; }

        public string? LibraryFileName { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
