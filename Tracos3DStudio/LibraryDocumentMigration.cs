using System.IO;

namespace Tracos3DStudio;

/// <summary>
/// Migrações de schema do arquivo .tracos-lib.
/// </summary>
public static class LibraryDocumentMigration
{
    public static void Migrate(LibraryDocument document)
    {
        while (document.SchemaVersion < LibraryDocument.CurrentSchemaVersion)
        {
            switch (document.SchemaVersion)
            {
                case 1:
                    MigrateV1ToV2(document);
                    document.SchemaVersion = 2;
                    break;
                default:
                    throw new InvalidDataException(
                        $"Migração ausente para schemaVersion {document.SchemaVersion}.");
            }
        }
    }

    private static void MigrateV1ToV2(LibraryDocument document)
    {
        // v2 adiciona modulationRules opcional por módulo — arquivos v1 permanecem válidos.
        _ = document;
    }
}
