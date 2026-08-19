namespace Tracos3DStudio;

/// <summary>
/// Identidade hierárquica das gavetas no viewport. Todas as peças de uma mesma
/// gaveta compartilham o prefixo "Gaveta N —", permitindo selecionar/ocultar o
/// conjunto antes de entrar nas peças individuais.
/// </summary>
public static class DrawerPartNaming
{
    private const string Separator = " — ";

    public static string Assembly(int oneBasedIndex) => $"Gaveta {oneBasedIndex}";

    public static string Part(int oneBasedIndex, string partName) =>
        $"{Assembly(oneBasedIndex)}{Separator}{partName}";

    public static bool TryGetAssembly(string? label, out string assembly)
    {
        assembly = string.Empty;
        if (string.IsNullOrWhiteSpace(label) ||
            !label.StartsWith("Gaveta ", StringComparison.OrdinalIgnoreCase))
            return false;

        int separator = label.IndexOf(Separator, StringComparison.Ordinal);
        assembly = separator >= 0 ? label[..separator] : label;
        return assembly.Length > "Gaveta ".Length;
    }

    public static bool IsAssemblySelection(string? label) =>
        TryGetAssembly(label, out string assembly) &&
        string.Equals(label, assembly, StringComparison.Ordinal);

    public static bool BelongsToAssembly(string? partLabel, string? assemblyLabel) =>
        TryGetAssembly(partLabel, out string assembly) &&
        string.Equals(assembly, assemblyLabel, StringComparison.Ordinal);

    public static bool MatchesSelection(string? partLabel, string? selectionLabel) =>
        IsAssemblySelection(selectionLabel)
            ? BelongsToAssembly(partLabel, selectionLabel)
            : string.Equals(partLabel, selectionLabel, StringComparison.Ordinal);
}
