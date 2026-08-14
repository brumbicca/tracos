namespace Tracos3DStudio;

public static class ModuleCatalogOverrideMerger
{
    public static ModuleDefinition Merge(ModuleDefinition builtIn, ModuleDefinition patch) =>
        new()
        {
            Id = builtIn.Id,
            Category = builtIn.Category,
            LibraryGroup = string.IsNullOrWhiteSpace(patch.LibraryGroup) ? builtIn.LibraryGroup : patch.LibraryGroup.Trim(),
            LibrarySubGroup = string.IsNullOrWhiteSpace(patch.LibrarySubGroup) ? builtIn.LibrarySubGroup : patch.LibrarySubGroup.Trim(),
            CatalogOrder = patch.CatalogOrder != 0 ? patch.CatalogOrder : builtIn.CatalogOrder,
            ShapeKind = patch.ShapeKind != ModuleShapeKind.Standard ? patch.ShapeKind : builtIn.ShapeKind,
            DisplayName = string.IsNullOrWhiteSpace(patch.DisplayName) ? builtIn.DisplayName : patch.DisplayName.Trim(),
            DefaultWidth = patch.DefaultWidth > 0 ? patch.DefaultWidth : builtIn.DefaultWidth,
            DefaultHeight = patch.DefaultHeight > 0 ? patch.DefaultHeight : builtIn.DefaultHeight,
            DefaultDepth = patch.DefaultDepth > 0 ? patch.DefaultDepth : builtIn.DefaultDepth,
            MinWidth = patch.MinWidth > 0 ? patch.MinWidth : builtIn.MinWidth,
            MaxWidth = patch.MaxWidth > 0 ? patch.MaxWidth : builtIn.MaxWidth,
            MinHeight = patch.MinHeight > 0 ? patch.MinHeight : builtIn.MinHeight,
            MaxHeight = patch.MaxHeight > 0 ? patch.MaxHeight : builtIn.MaxHeight,
            MinDepth = patch.MinDepth > 0 ? patch.MinDepth : builtIn.MinDepth,
            MaxDepth = patch.MaxDepth > 0 ? patch.MaxDepth : builtIn.MaxDepth,
            DoorCount = patch.DoorCount,
            DrawerCount = patch.DrawerCount,
            IsWallMounted = builtIn.IsWallMounted,
            ModulationRules = patch.ModulationRules ?? builtIn.ModulationRules
        };
}
