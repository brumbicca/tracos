namespace Tracos3DStudio;

public static class ModuleCatalogFilterService
{
    public static bool Matches(ModuleDefinition definition, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        string term = query.Trim();

        return definition.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               definition.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               definition.LibraryGroup.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               definition.LibrarySubGroup.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<ModuleDefinition> Filter(
        IEnumerable<ModuleDefinition> definitions,
        string? query) =>
        definitions.Where(definition => Matches(definition, query)).ToList();
}
