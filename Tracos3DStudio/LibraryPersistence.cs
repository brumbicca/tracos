using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracos3DStudio;

public static class LibraryPersistence
{
    public const string FileExtension = ".tracos-lib";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string DefaultLibraryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tracos3DStudio",
            $"biblioteca{FileExtension}");

    public static LibraryDocument LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var document = JsonSerializer.Deserialize<LibraryDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Arquivo de biblioteca inválido.");

        if (document.SchemaVersion > LibraryDocument.CurrentSchemaVersion)
            throw new InvalidDataException("Versão da biblioteca não suportada.");

        LibraryDocumentMigration.Migrate(document);

        return document;
    }

    public static void SaveToFile(LibraryDocument document, string filePath)
    {
        document.SchemaVersion = LibraryDocument.CurrentSchemaVersion;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static LibraryDocument LoadDefaultOrEmpty()
    {
        if (!File.Exists(DefaultLibraryPath))
            return new LibraryDocument();

        try
        {
            return LoadFromFile(DefaultLibraryPath);
        }
        catch
        {
            return new LibraryDocument();
        }
    }

    public static void ApplyToCatalogs(LibraryDocument document)
    {
        var customModules = new List<ModuleDefinition>();
        var builtInOverrides = new List<ModuleDefinition>();

        foreach (var moduleData in document.Modules.Where(m => !string.IsNullOrWhiteSpace(m.Id)))
        {
            var definition = moduleData.ToDefinition();

            if (ModuleCatalog.IsBuiltIn(definition.Id))
                builtInOverrides.Add(definition);
            else
                customModules.Add(definition);
        }

        ModuleCatalog.SetBuiltInOverrides(builtInOverrides);
        ModuleCatalog.SetCustomModules(customModules);

        var materials = document.Materials
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => new MaterialDefinition
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                ColorHex = m.ColorHex,
                PricingMode = m.PricingMode,
                PriceValue = m.PriceValue
            })
            .ToList();

        MaterialCatalog.SetCustomMaterials(materials);
        LibraryState.Apply(document);
    }
}
