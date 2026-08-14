using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class WallLayerDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; } = false;

    public bool IsCustom { get; init; } = false;

    public LayerFillMode FillMode { get; set; } = LayerFillMode.Default;
}

/// <summary>Camadas de parede e módulo (estilo Promob — Exibir → Camadas).</summary>
public static class WallLayerCatalog
{
    public const string DefaultLayerId = "parede";

    public const string DefaultModuleLayerId = "modulo";

    private static readonly WallLayerDefinition[] BuiltInLayers =
    [
        new() { Id = "parede", DisplayName = "Parede" },
        new() { Id = "divisoria", DisplayName = "Divisória" },
        new() { Id = "referencia", DisplayName = "Referência" },
        new() { Id = "modulo", DisplayName = "Módulo" }
    ];

    public static IReadOnlyList<WallLayerDefinition> GetDefinitions(ProjectMetadata metadata)
    {
        var result = new List<WallLayerDefinition>();

        foreach (var def in BuiltInLayers)
            result.Add(BuildDefinition(metadata, def.Id, def.DisplayName, isCustom: false));

        if (metadata.CustomLayerNames != null)
        {
            foreach (var pair in metadata.CustomLayerNames.OrderBy(p => p.Value, StringComparer.OrdinalIgnoreCase))
            {
                if (IsBuiltInLayer(pair.Key))
                    continue;

                result.Add(BuildDefinition(metadata, pair.Key, pair.Value, isCustom: true));
            }
        }

        return result;
    }

    public static bool IsLayerVisible(ProjectMetadata metadata, string? layerId)
    {
        string id = NormalizeLayerId(layerId);
        if (metadata.WallLayerVisibility != null &&
            metadata.WallLayerVisibility.TryGetValue(id, out bool visible))
            return visible;

        return true;
    }

    public static bool IsLayerLocked(ProjectMetadata metadata, string? layerId)
    {
        string id = NormalizeLayerId(layerId);
        return metadata.LayerLocked != null &&
               metadata.LayerLocked.TryGetValue(id, out bool locked) &&
               locked;
    }

    public static bool CanPickOnLayer(ProjectMetadata metadata, string? layerId) =>
        IsLayerVisible(metadata, layerId) && !IsLayerLocked(metadata, layerId);

    public static LayerFillMode GetLayerFillMode(ProjectMetadata metadata, string? layerId)
    {
        string id = NormalizeLayerId(layerId);

        if (metadata.LayerFillModes != null &&
            metadata.LayerFillModes.TryGetValue(id, out LayerFillMode mode))
            return mode;

        return LayerFillMode.Default;
    }

    public static void SetLayerFillMode(ProjectMetadata metadata, string layerId, LayerFillMode mode)
    {
        string id = NormalizeLayerId(layerId);
        metadata.LayerFillModes ??= new Dictionary<string, LayerFillMode>();

        if (mode == LayerFillMode.Default)
        {
            metadata.LayerFillModes.Remove(id);

            if (metadata.LayerFillModes.Count == 0)
                metadata.LayerFillModes = null;
        }
        else
            metadata.LayerFillModes[id] = mode;
    }

    public static string GetDisplayName(ProjectMetadata metadata, string? layerId)
    {
        string id = NormalizeLayerId(layerId);

        foreach (var def in BuiltInLayers)
        {
            if (def.Id == id)
                return def.DisplayName;
        }

        if (metadata.CustomLayerNames != null &&
            metadata.CustomLayerNames.TryGetValue(id, out string? customName) &&
            !string.IsNullOrWhiteSpace(customName))
            return customName;

        return id;
    }

    public static string GetDisplayName(string? layerId) =>
        GetDisplayName(new ProjectMetadata(), layerId);

    public static string NormalizeLayerId(string? layerId) =>
        string.IsNullOrWhiteSpace(layerId) ? DefaultLayerId : layerId.Trim().ToLowerInvariant();

    public static string NormalizeModuleLayerId(string? layerId) =>
        string.IsNullOrWhiteSpace(layerId) ? DefaultModuleLayerId : layerId.Trim().ToLowerInvariant();

    public static void SetLayerVisible(ProjectMetadata metadata, string layerId, bool visible)
    {
        metadata.WallLayerVisibility ??= new Dictionary<string, bool>();
        metadata.WallLayerVisibility[NormalizeLayerId(layerId)] = visible;
    }

    public static void SetLayerLocked(ProjectMetadata metadata, string layerId, bool locked)
    {
        metadata.LayerLocked ??= new Dictionary<string, bool>();
        metadata.LayerLocked[NormalizeLayerId(layerId)] = locked;
    }

    public static void SetAllLayersVisible(ProjectMetadata metadata, bool visible)
    {
        metadata.WallLayerVisibility ??= new Dictionary<string, bool>();

        foreach (var def in GetDefinitions(metadata))
            metadata.WallLayerVisibility[def.Id] = visible;
    }

    public static bool TryAddCustomLayer(ProjectMetadata metadata, string displayName, out string layerId, out string? error)
    {
        layerId = "";
        error = null;

        string trimmed = displayName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            error = "Informe o nome da camada.";
            return false;
        }

        string baseId = SlugFromDisplayName(trimmed);
        layerId = EnsureUniqueLayerId(metadata, baseId);

        metadata.CustomLayerNames ??= new Dictionary<string, string>();
        metadata.CustomLayerNames[layerId] = trimmed;
        metadata.WallLayerVisibility ??= new Dictionary<string, bool>();
        metadata.WallLayerVisibility[layerId] = true;

        return true;
    }

    public static int CountWallsOnLayer(IReadOnlyList<WallSegment> walls, string layerId)
    {
        string id = NormalizeLayerId(layerId);
        return walls.Count(w => NormalizeLayerId(w.LayerId) == id);
    }

    public static int CountModulesOnLayer(IReadOnlyList<ModuleInstance> modules, string layerId)
    {
        string id = NormalizeLayerId(layerId);
        return modules.Count(m => NormalizeModuleLayerId(m.LayerId) == id);
    }

    public static IReadOnlyList<WallLayerDefinition> GetEmptyCustomLayers(
        ProjectMetadata metadata,
        IReadOnlyList<WallSegment> walls,
        IReadOnlyList<ModuleInstance> modules) =>
        GetDefinitions(metadata)
            .Where(layer =>
                layer.IsCustom &&
                CountWallsOnLayer(walls, layer.Id) == 0 &&
                CountModulesOnLayer(modules, layer.Id) == 0)
            .ToList();

    public static int TryRemoveEmptyCustomLayers(
        ProjectMetadata metadata,
        IReadOnlyList<WallSegment> walls,
        IReadOnlyList<ModuleInstance> modules,
        out IReadOnlyList<string> removedDisplayNames)
    {
        var emptyLayers = GetEmptyCustomLayers(metadata, walls, modules);
        var names = new List<string>();

        foreach (var layer in emptyLayers)
        {
            if (RemoveCustomLayer(metadata, layer.Id))
                names.Add(layer.DisplayName);
        }

        removedDisplayNames = names;
        return names.Count;
    }

    private static bool RemoveCustomLayer(ProjectMetadata metadata, string layerId)
    {
        string id = NormalizeLayerId(layerId);

        if (IsBuiltInLayer(id))
            return false;

        if (metadata.CustomLayerNames == null || !metadata.CustomLayerNames.Remove(id))
            return false;

        metadata.WallLayerVisibility?.Remove(id);
        metadata.LayerLocked?.Remove(id);

        if (metadata.LayerFillModes != null)
        {
            metadata.LayerFillModes.Remove(id);

            if (metadata.LayerFillModes.Count == 0)
                metadata.LayerFillModes = null;
        }

        return true;
    }

    private static WallLayerDefinition BuildDefinition(
        ProjectMetadata metadata,
        string id,
        string displayName,
        bool isCustom)
    {
        return new WallLayerDefinition
        {
            Id = id,
            DisplayName = displayName,
            IsVisible = IsLayerVisible(metadata, id),
            IsLocked = IsLayerLocked(metadata, id),
            IsCustom = isCustom,
            FillMode = GetLayerFillMode(metadata, id)
        };
    }

    public static Vector4 GetLayerOutlineColor(string? layerId) =>
        NormalizeLayerId(layerId) switch
        {
            "divisoria" => new Vector4(0.2f, 0.45f, 0.95f, 1f),
            "referencia" => new Vector4(0.55f, 0.55f, 0.55f, 1f),
            "modulo" => new Vector4(0.55f, 0.35f, 0.15f, 1f),
            _ => new Vector4(0.05f, 0.05f, 0.05f, 1f)
        };

    private static bool IsBuiltInLayer(string layerId) =>
        BuiltInLayers.Any(def => def.Id == NormalizeLayerId(layerId));

    private static string EnsureUniqueLayerId(ProjectMetadata metadata, string baseId)
    {
        var known = new HashSet<string>(BuiltInLayers.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);

        if (metadata.CustomLayerNames != null)
        {
            foreach (var key in metadata.CustomLayerNames.Keys)
                known.Add(NormalizeLayerId(key));
        }

        string candidate = baseId;
        int suffix = 2;

        while (known.Contains(candidate))
        {
            candidate = $"{baseId}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string SlugFromDisplayName(string displayName)
    {
        string normalized = displayName.Trim().ToLowerInvariant();
        normalized = normalized.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        normalized = Regex.Replace(builder.ToString(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(normalized) ? "camada" : normalized;
    }
}
