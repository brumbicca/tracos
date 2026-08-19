namespace Tracos3DStudio;

public readonly record struct RevealHiddenResult(int Modules, int Parts)
{
    public bool Changed => Modules > 0 || Parts > 0;
}

/// <summary>Ocultação temporária de módulos e peças, sem alterar engenharia ou orçamento.</summary>
public static class SceneOcclusionService
{
    public static bool HidePart(ModuleInstance module, string? partLabel)
    {
        if (string.IsNullOrWhiteSpace(partLabel))
            return false;

        if (DrawerPartNaming.IsAssemblySelection(partLabel))
        {
            bool changed = false;
            foreach (string label in module.Mesh.Faces
                         .Select(face => face.Label)
                         .Where(label => DrawerPartNaming.BelongsToAssembly(label, partLabel))
                         .Distinct(StringComparer.Ordinal))
                changed |= module.HiddenPartLabels.Add(label);
            return changed;
        }

        return module.HiddenPartLabels.Add(partLabel);
    }

    public static int HideModules(IEnumerable<ModuleInstance> modules)
    {
        int changed = 0;
        foreach (var module in modules.DistinctBy(module => module.Id))
        {
            if (!module.IsVisible)
                continue;
            module.IsVisible = false;
            changed++;
        }
        return changed;
    }

    public static RevealHiddenResult RevealAll(Project project)
    {
        int modules = 0;
        int parts = 0;
        foreach (var module in project.Modules)
        {
            if (!module.IsVisible)
            {
                module.IsVisible = true;
                modules++;
            }

            parts += module.HiddenPartLabels.Count;
            module.HiddenPartLabels.Clear();
        }
        return new RevealHiddenResult(modules, parts);
    }
}
