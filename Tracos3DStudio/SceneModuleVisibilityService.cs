namespace Tracos3DStudio;

public static class SceneModuleVisibilityService
{
    public static bool? GetVisibleState(IEnumerable<ModuleInstance> modules)
    {
        var list = modules.ToList();
        if (list.Count == 0)
            return null;

        bool allVisible = list.All(module => module.IsVisible);
        if (allVisible)
            return true;

        bool allHidden = list.All(module => !module.IsVisible);
        if (allHidden)
            return false;

        return null;
    }

    public static bool? GetLockedState(IEnumerable<ModuleInstance> modules)
    {
        var list = modules.ToList();
        if (list.Count == 0)
            return null;

        bool allLocked = list.All(module => module.IsLocked);
        if (allLocked)
            return true;

        bool allUnlocked = list.All(module => !module.IsLocked);
        if (allUnlocked)
            return false;

        return null;
    }

    public static bool CanToggle(IReadOnlyList<ModuleInstance> modules) => modules.Count > 0;

    public static bool CanDeleteSelection(IReadOnlyList<ModuleInstance> modules) =>
        modules.Count > 0 && modules.All(module => !module.IsLocked);

    public static bool IsEditable(ModuleInstance module) => !module.IsLocked;

    public static string FormatListStatusSuffix(ModuleInstance module)
    {
        if (!module.IsVisible && module.IsLocked)
            return " (oculto, bloqueado)";

        if (!module.IsVisible)
            return " (oculto)";

        if (module.IsLocked)
            return " (bloqueado)";

        return string.Empty;
    }
}
