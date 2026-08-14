using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracos3DStudio;

/// <summary>
/// Persiste o padrão de engenharia definido no <b>Configurador de Dimensões</b>
/// (não é perfil de construção nem defaults de fábrica).
/// Usado por novos projetos e próximas sessões até o usuário alterar de novo no configurador.
/// </summary>
public static class DimensionConfiguratorProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static string ProfileFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tracos3DStudio",
            "configurador_dimensoes.json");

    private static string LegacyFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tracos3DStudio",
            "user_defaults.json");

    public static bool HasSavedProfile() =>
        File.Exists(ProfileFilePath) || File.Exists(LegacyFilePath);

    public static void Save(DimensionConfiguratorSettings settings)
    {
        var dir = Path.GetDirectoryName(ProfileFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ProfileFilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static DimensionConfiguratorSettings? Load()
    {
        if (TryLoadFrom(ProfileFilePath, out var profile))
            return profile;

        if (TryLoadFrom(LegacyFilePath, out profile))
        {
            Save(profile!);
            return profile;
        }

        return null;
    }

    private static bool TryLoadFrom(string path, out DimensionConfiguratorSettings? settings)
    {
        settings = null;
        try
        {
            if (!File.Exists(path))
                return false;

            var json = File.ReadAllText(path);
            settings = JsonSerializer.Deserialize<DimensionConfiguratorSettings>(json, JsonOptions);
            return settings != null;
        }
        catch
        {
            return false;
        }
    }
}
