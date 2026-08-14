using System.Globalization;
using System.Windows.Media;

namespace Tracos3DStudio;

public abstract class SceneModuleListEntry
{
    public virtual bool IsGroupHeader => false;
}

public enum SceneModuleListGroupKind
{
    Compartment,
    Wall,
    Other
}

public sealed class SceneModuleListGroupEntry : SceneModuleListEntry
{
    public override bool IsGroupHeader => true;

    public required string GroupTitle { get; init; }

    public SceneModuleListGroupKind Kind { get; init; } = SceneModuleListGroupKind.Wall;

    public Guid? WallId { get; init; }

    public Guid? CompartmentId { get; init; }
}

public sealed class SceneModuleListItem : SceneModuleListEntry
{
    public required ModuleInstance Module { get; init; }

    public required string DisplayLabel { get; init; }

    public required string IconHint { get; init; }

    public required Brush AccentBrush { get; init; }

    public double ListOpacity => Module.IsVisible ? 1.0 : 0.45;
}

public static class SceneModuleListService
{
    public static string FormatListLabel(ModuleInstance module) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{ModuleInstanceNamingService.GetEffectiveDisplayName(module)} — {module.Width:0}×{module.Height:0}×{module.Depth:0} mm{SceneModuleVisibilityService.FormatListStatusSuffix(module)}");

    public static SceneModuleListItem CreateItem(ModuleInstance module)
    {
        var definition = ModuleCatalog.GetRequired(module.DefinitionId);
        return new SceneModuleListItem
        {
            Module = module,
            DisplayLabel = FormatListLabel(module),
            IconHint = ModuleCatalogThumbnail.GetIconHint(definition),
            AccentBrush = ModuleCatalogThumbnail.GetAccentBrush(definition)
        };
    }

    public static IReadOnlyList<SceneModuleListItem> BuildItems(IEnumerable<ModuleInstance> modules) =>
        modules.Select(CreateItem).ToList();

    public static IReadOnlyList<SceneModuleListEntry> BuildGroupedEntries(
        IEnumerable<ModuleInstance> modules,
        IReadOnlyList<WallSegment> walls,
        IReadOnlyList<RoomCompartment> compartments)
    {
        var moduleList = modules.ToList();
        var compartmentList = compartments.ToList();

        if (moduleList.Count == 0)
            return Array.Empty<SceneModuleListEntry>();

        var entries = new List<SceneModuleListEntry>();
        var modulesByWall = new Dictionary<Guid, List<ModuleInstance>>();

        foreach (var module in moduleList)
        {
            if (!module.AttachedWallId.HasValue)
                continue;

            if (!modulesByWall.TryGetValue(module.AttachedWallId.Value, out List<ModuleInstance>? group))
            {
                group = new List<ModuleInstance>();
                modulesByWall[module.AttachedWallId.Value] = group;
            }

            group.Add(module);
        }

        if (compartmentList.Count == 0)
        {
            AppendWallGroups(entries, walls, walls, modulesByWall);
            AppendUnknownWallModules(entries, moduleList, walls);
            AppendUnattachedModules(entries, moduleList);
            return entries;
        }

        foreach (var compartment in compartmentList)
        {
            var wallsInCompartment = walls
                .Where(wall => RoomCompartmentService.ResolveWallCompartmentId(wall, compartmentList) == compartment.Id)
                .ToList();

            bool compartmentHasModules = wallsInCompartment.Any(wall =>
                modulesByWall.TryGetValue(wall.Id, out List<ModuleInstance>? group) && group.Count > 0);

            if (!compartmentHasModules)
                continue;

            entries.Add(new SceneModuleListGroupEntry
            {
                GroupTitle = RoomCompartmentService.FormatCompartmentGroupTitle(compartment, compartmentList),
                Kind = SceneModuleListGroupKind.Compartment,
                CompartmentId = compartment.Id
            });

            AppendWallGroups(entries, wallsInCompartment, walls, modulesByWall);
        }

        var orphanWalls = walls
            .Where(wall =>
                wall.CompartmentId.HasValue &&
                RoomCompartmentService.FindCompartment(compartmentList, wall.CompartmentId.Value) == null &&
                modulesByWall.TryGetValue(wall.Id, out List<ModuleInstance>? group) &&
                group.Count > 0)
            .ToList();

        if (orphanWalls.Count > 0)
        {
            entries.Add(new SceneModuleListGroupEntry
            {
                GroupTitle = "Cômodo removido",
                Kind = SceneModuleListGroupKind.Other,
                CompartmentId = null
            });

            AppendWallGroups(entries, orphanWalls, walls, modulesByWall);
        }

        AppendUnknownWallModules(entries, moduleList, walls);
        AppendUnattachedModules(entries, moduleList);
        return entries;
    }

    private static void AppendWallGroups(
        List<SceneModuleListEntry> entries,
        IEnumerable<WallSegment> wallsInScope,
        IReadOnlyList<WallSegment> allWalls,
        Dictionary<Guid, List<ModuleInstance>> modulesByWall)
    {
        foreach (var wall in wallsInScope)
        {
            if (!modulesByWall.TryGetValue(wall.Id, out List<ModuleInstance>? group) || group.Count == 0)
                continue;

            entries.Add(new SceneModuleListGroupEntry
            {
                GroupTitle = WallLabelService.FormatWallGroupTitle(wall, allWalls),
                Kind = SceneModuleListGroupKind.Wall,
                WallId = wall.Id
            });

            foreach (var module in group.OrderBy(m => m.DistanceAlongWall))
                entries.Add(CreateItem(module));
        }
    }

    private static void AppendUnknownWallModules(
        List<SceneModuleListEntry> entries,
        List<ModuleInstance> moduleList,
        IReadOnlyList<WallSegment> walls)
    {
        var attachedToUnknownWall = moduleList
            .Where(module =>
                module.AttachedWallId.HasValue &&
                WallLabelService.FindWall(walls, module.AttachedWallId.Value) == null)
            .OrderBy(module => module.DistanceAlongWall)
            .ToList();

        if (attachedToUnknownWall.Count == 0)
            return;

        entries.Add(new SceneModuleListGroupEntry
        {
            GroupTitle = "Parede removida",
            Kind = SceneModuleListGroupKind.Other,
            WallId = null
        });

        foreach (var module in attachedToUnknownWall)
            entries.Add(CreateItem(module));
    }

    private static void AppendUnattachedModules(List<SceneModuleListEntry> entries, List<ModuleInstance> moduleList)
    {
        var unattached = moduleList
            .Where(module => !module.AttachedWallId.HasValue)
            .ToList();

        if (unattached.Count == 0)
            return;

        entries.Add(new SceneModuleListGroupEntry
        {
            GroupTitle = WallLabelService.UnattachedGroupTitle,
            Kind = SceneModuleListGroupKind.Other,
            WallId = null
        });

        foreach (var module in unattached)
            entries.Add(CreateItem(module));
    }
}
