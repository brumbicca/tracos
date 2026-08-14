namespace Tracos3DStudio;

public static class ModuleInstanceNamingService
{
    public const int MaxLength = 80;

    public static string GetCatalogDisplayName(ModuleInstance module) =>
        ModuleCatalog.GetRequired(module.DefinitionId).DisplayName;

    public static string GetEffectiveDisplayName(ModuleInstance module) =>
        string.IsNullOrWhiteSpace(module.InstanceDisplayName)
            ? GetCatalogDisplayName(module)
            : module.InstanceDisplayName.Trim();

    public static bool TryApplyRename(ModuleInstance module, string? rawName, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(rawName))
        {
            module.InstanceDisplayName = null;
            return true;
        }

        string name = rawName.Trim();

        if (name.Length > MaxLength)
        {
            error = $"Nome no ambiente deve ter no máximo {MaxLength} caracteres.";
            return false;
        }

        module.InstanceDisplayName = name;
        return true;
    }
}
