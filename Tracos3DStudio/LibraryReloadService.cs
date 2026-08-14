namespace Tracos3DStudio;

using System.IO;

public static class LibraryReloadService
{
    public static LibraryDocument ReloadFromDefaultPath()
    {
        var document = LibraryPersistence.LoadDefaultOrEmpty();
        LibraryPersistence.ApplyToCatalogs(document);
        return document;
    }

    public static LibraryDocument ReloadFromFile(string filePath)
    {
        var document = File.Exists(filePath)
            ? LibraryPersistence.LoadFromFile(filePath)
            : new LibraryDocument();

        LibraryPersistence.ApplyToCatalogs(document);
        return document;
    }
}
