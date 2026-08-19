using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Malhas especiais de Inferiores (cantos, diagonais, extratores…) — formas distintas visíveis no viewport.
/// </summary>
public static class ModuleMeshShapes
{
    public static bool TryBuild(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        if (BalconyModuleBuilder.TryBuild(instance, definition, dimensionSettings))
            return true;

        if (DrawerModuleBuilder.TryBuild(instance, definition, dimensionSettings))
            return true;

        return definition.ShapeKind switch
        {
            ModuleShapeKind.Standard => false,
            ModuleShapeKind.Filler => BuildFiller(instance),
            ModuleShapeKind.BlindCornerLeft => CallBlind(instance, definition, left: true, dimensionSettings),
            ModuleShapeKind.BlindCornerRight => CallBlind(instance, definition, left: false, dimensionSettings),
            ModuleShapeKind.CornerLLeft => CallL(instance, definition, left: true, dimensionSettings),
            ModuleShapeKind.CornerLRight => CallL(instance, definition, left: false, dimensionSettings),
            ModuleShapeKind.Oblique => CallOblique(instance, definition, dimensionSettings),
            ModuleShapeKind.CornerDrawer => CallCornerDrawer(instance, definition, dimensionSettings),
            ModuleShapeKind.CornerCurved => CallCornerCurved(instance, definition, dimensionSettings),
            ModuleShapeKind.CurvedFront => BuildCurvedFront(instance, definition, dimensionSettings),
            ModuleShapeKind.Bifold => BuildBifold(instance, definition, dimensionSettings),
            ModuleShapeKind.ColumnDoors => SpecialColumnModuleBuilder.Build(instance, definition, dimensionSettings),
            ModuleShapeKind.PullOutNarrow => BuildPullOutNarrow(instance, definition, dimensionSettings),
            ModuleShapeKind.WineRack => BuildWineRack(instance, definition, dimensionSettings),
            ModuleShapeKind.ApplianceBay => BuildApplianceBay(instance, definition, dimensionSettings),
            ModuleShapeKind.EndDiagonal => EndTerminalModuleBuilder.Build(instance, definition, dimensionSettings),
            ModuleShapeKind.EndCurved => BuildEndCut(instance, definition, dimensionSettings, EndCut.Curved),
            ModuleShapeKind.EndChamfer => EndTerminalModuleBuilder.Build(instance, definition, dimensionSettings),
            ModuleShapeKind.EndZ => BuildEndCut(instance, definition, dimensionSettings, EndCut.Z),
            ModuleShapeKind.OpenCornerShelves => BuildOpenShelves(instance, definition, dimensionSettings),
            _ => false
        };
    }

    private enum EndCut { Diagonal, Curved, Chamfer, Z }

    private static bool CallBlind(
        ModuleInstance instance,
        ModuleDefinition definition,
        bool left,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        if (instance.BlindCorner == null)
            instance.BlindCorner = BlindCornerParams.FromConfigurator(dimensionSettings);

        ModuleCornerMeshBuilder.BuildBlindCorner(instance, definition, left, dimensionSettings);
        return true;
    }

    private static bool CallL(ModuleInstance instance, ModuleDefinition definition, bool left)
    {
        return CallL(instance, definition, left, dimensionSettings: null);
    }

    private static bool CallL(
        ModuleInstance instance,
        ModuleDefinition definition,
        bool left,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        // Não chamar SyncFromEnvelope aqui: após Rebuild, instance.Depth passa a ser o
        // comprimento da asa (envelope), não a profundidade do configurador (B).
        // Pe/Pd/Altura vêm de ApplyDefinition / SetDimensions.
        if (instance.CornerL == null)
        {
            var (_, cfgH, cfgD) = DimensionConfiguratorService.ResolveInsertionDimensions(
                definition, dimensionSettings);
            float width = instance.Width > 0 ? instance.Width : definition.DefaultWidth;
            float height = instance.Height > 0 ? instance.Height : cfgH;
            // Profundidade do lado = DefaultDepth do SKU ou B do configurador — nunca o envelope.
            float depth = cfgD > 0 ? cfgD : definition.DefaultDepth;
            instance.CornerL = CornerLParams.FromModuleDefaults(width, depth, height, 18f, left);
        }

        instance.CornerL.IsLeftHand = left;
        CornerLModuleBuilder.Rebuild(instance, definition, instance.CornerL, dimensionSettings);
        return true;
    }

    private static bool CallOblique(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        ModuleCornerMeshBuilder.BuildOblique(instance, definition, dimensionSettings);
        return true;
    }

    private static bool CallCornerDrawer(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        ModuleCornerMeshBuilder.BuildCornerDrawer(instance, definition, dimensionSettings);
        return true;
    }

    private static bool CallCornerCurved(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? dimensionSettings)
    {
        ModuleCornerMeshBuilder.BuildCurvedCorner(instance, definition, dimensionSettings);
        return true;
    }

    private static bool BuildFiller(ModuleInstance instance)
    {
        ModuleMeshBuilder.BuildFlatPanel(instance);
        return true;
    }

    // BuildBlindCorner / BuildCornerL / BuildOblique movidos para ModuleCornerMeshBuilder.

    private static bool BuildCurvedFront(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(instance, definition, settings);

        // Aba curva aproximada com 3 fatias na frente
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float ft = definition.FrontThickness;
        var id = instance.Id;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        float bump = MathF.Min(40f, w * 0.08f);
        AddBox(instance, ToWorld, id,
            new Vector3(w * 0.15f, 30f, d + ft),
            new Vector3(w * 0.85f, h - 30f, d + ft + bump),
            FaceKind.ModuleFront);
        return true;
    }

    private static bool BuildBifold(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        if (definition.Id.StartsWith("canto-bifold-l-", StringComparison.OrdinalIgnoreCase))
        {
            bool left = definition.Id.Contains("esq", StringComparison.OrdinalIgnoreCase);
            return CallL(instance, definition, left, settings);
        }

        var bifold = new ModuleDefinition
        {
            Id = definition.Id,
            DisplayName = definition.DisplayName,
            Category = definition.Category,
            DoorCount = Math.Max(2, definition.DoorCount),
            DrawerCount = 0,
            FrontThickness = definition.FrontThickness,
            DefaultWidth = definition.DefaultWidth,
            DefaultHeight = definition.DefaultHeight,
            DefaultDepth = definition.DefaultDepth,
            MinWidth = definition.MinWidth,
            MaxWidth = definition.MaxWidth,
            MinHeight = definition.MinHeight,
            MaxHeight = definition.MaxHeight,
            MinDepth = definition.MinDepth,
            MaxDepth = definition.MaxDepth,
            ShapeKind = ModuleShapeKind.Standard
        };
        ModuleMeshBuilder.BuildBoxWithFront(instance, bifold, settings);
        return true;
    }

    private static bool BuildPullOutNarrow(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(instance, CloneFronts(definition, doors: 0, drawers: 0), settings);

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        // Divisórias verticais internas (visual de extrator)
        int slots = 4;
        for (int i = 1; i < slots; i++)
        {
            float z = d * i / slots;
            AddBox(instance, ToWorld, id,
                new Vector3(4f, 20f, z - 3f),
                new Vector3(w - 4f, h - 20f, z + 3f),
                FaceKind.ModuleFront);
        }

        return true;
    }

    private static bool BuildWineRack(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(instance, CloneFronts(definition, doors: 0, drawers: 0), settings);

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = 12f;
        var id = instance.Id;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        int rows = definition.DisplayName.Contains("Circ", StringComparison.OrdinalIgnoreCase) ? 6 : 8;
        for (int i = 1; i < rows; i++)
        {
            float y = h * i / rows;
            AddBox(instance, ToWorld, id,
                new Vector3(t, y - 4f, t),
                new Vector3(w - t, y + 4f, d - t),
                FaceKind.ModuleTop);
        }

        return true;
    }

    private static bool BuildApplianceBay(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        ModuleMeshBuilder.BuildBoxWithFront(
            instance,
            CloneFronts(definition, doors: 0, drawers: Math.Max(0, definition.DrawerCount)),
            settings);

        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        var id = instance.Id;
        var effective = DimensionConfiguratorService.CreateEffectiveRules(definition, settings);
        var structure = effective?.Structure;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        // Quadro do nicho (vazio visual na frente)
        float margin = 40f;
        float lateralOverFront = structure?.LateralAdvanceOverFrontPanelMm ?? 0f;
        float frontOverLateral = structure?.FrontPanelAdvanceOverLateralMm ?? 0f;
        float panelX0 = Math.Clamp(margin + lateralOverFront - frontOverLateral, 0f, w * 0.45f);
        float panelX1 = Math.Clamp(w - margin - lateralOverFront + frontOverLateral, w * 0.55f, w);
        float bayH = definition.DrawerCount > 0 ? h * 0.55f : h * 0.75f;
        float y0 = definition.DrawerCount > 0 ? h * 0.35f : h * 0.12f;
        AddBox(instance, ToWorld, id,
            new Vector3(panelX0, y0, d - 8f),
            new Vector3(panelX1, y0 + bayH, d + 4f),
            FaceKind.ModuleBack);
        return true;
    }

    private static bool BuildEndCut(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings,
        EndCut cut)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        bool left = definition.DisplayName.Contains("Esq", StringComparison.OrdinalIgnoreCase);
        float cutW = cut switch
        {
            EndCut.Chamfer => MathF.Min(80f, w * 0.35f),
            EndCut.Z => MathF.Min(120f, w * 0.45f),
            _ => MathF.Min(w * 0.55f, d * 0.7f)
        };

        var id = instance.Id;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        if (left)
        {
            AddBox(instance, ToWorld, id, new Vector3(cutW, 0f, 0f), new Vector3(w, h, d), FaceKind.ModuleBack);
            float inset = cut == EndCut.Curved ? cutW * 0.55f : cutW;
            AddBox(instance, ToWorld, id, new Vector3(0f, 0f, 0f), new Vector3(inset, h, d * 0.55f), FaceKind.ModuleLeft);
            if (cut == EndCut.Z)
                AddBox(instance, ToWorld, id, new Vector3(0f, 0f, d * 0.4f), new Vector3(cutW * 0.6f, h, d), FaceKind.ModuleFront);
        }
        else
        {
            AddBox(instance, ToWorld, id, new Vector3(0f, 0f, 0f), new Vector3(w - cutW, h, d), FaceKind.ModuleBack);
            float inset = cut == EndCut.Curved ? cutW * 0.55f : cutW;
            AddBox(instance, ToWorld, id, new Vector3(w - inset, 0f, 0f), new Vector3(w, h, d * 0.55f), FaceKind.ModuleRight);
            if (cut == EndCut.Z)
                AddBox(instance, ToWorld, id, new Vector3(w - cutW * 0.6f, 0f, d * 0.4f), new Vector3(w, h, d), FaceKind.ModuleFront);
        }

        return true;
    }

    private static bool BuildOpenShelves(
        ModuleInstance instance,
        ModuleDefinition definition,
        DimensionConfiguratorSettings? settings)
    {
        float w = instance.Width;
        float h = instance.Height;
        float d = instance.Depth;
        float t = 18f;
        var id = instance.Id;
        Vector3 ToWorld(Vector3 local) =>
            ModulePlacementService.TransformLocalPoint(local, instance.Position, instance.RotationYDegrees);

        // Laterais + 3 prateleiras (cantoneira aberta)
        AddBox(instance, ToWorld, id, Vector3.Zero, new Vector3(t, h, d), FaceKind.ModuleLeft);
        AddBox(instance, ToWorld, id, new Vector3(w - t, 0f, 0f), new Vector3(w, h, d), FaceKind.ModuleRight);
        AddBox(instance, ToWorld, id, new Vector3(0f, 0f, 0f), new Vector3(w, t, d), FaceKind.ModuleBottom);

        for (int i = 1; i <= 3; i++)
        {
            float y = h * i / 4f;
            AddBox(instance, ToWorld, id,
                new Vector3(t, y, 0f),
                new Vector3(w - t, y + t, d),
                FaceKind.ModuleTop);
        }

        return true;
    }

    private static ModuleDefinition CloneFronts(ModuleDefinition definition, int doors, int drawers) =>
        WithFrontSpan(definition, doors, drawers);

    private static ModuleDefinition WithFrontSpan(ModuleDefinition definition, int doors, int drawers) =>
        new()
        {
            Id = definition.Id,
            DisplayName = definition.DisplayName,
            Category = definition.Category,
            LibraryGroup = definition.LibraryGroup,
            LibrarySubGroup = definition.LibrarySubGroup,
            CatalogOrder = definition.CatalogOrder,
            ShapeKind = ModuleShapeKind.Standard,
            DefaultWidth = definition.DefaultWidth,
            DefaultHeight = definition.DefaultHeight,
            DefaultDepth = definition.DefaultDepth,
            MinWidth = definition.MinWidth,
            MaxWidth = definition.MaxWidth,
            MinHeight = definition.MinHeight,
            MaxHeight = definition.MaxHeight,
            MinDepth = definition.MinDepth,
            MaxDepth = definition.MaxDepth,
            FrontThickness = definition.FrontThickness,
            DoorCount = doors,
            DrawerCount = drawers,
            IsWallMounted = definition.IsWallMounted,
            ModulationRules = definition.ModulationRules
        };

    private static void AddBox(
        ModuleInstance instance,
        Func<Vector3, Vector3> toWorld,
        Guid id,
        Vector3 min,
        Vector3 max,
        FaceKind kind)
    {
        // Reusa o builder de painel interno via reflection? Melhor chamar API interna.
        // Exposto: usamos quads via MeshData diretamente.
        var mesh = instance.Mesh;
        var a = toWorld(new Vector3(min.X, min.Y, min.Z));
        var b = toWorld(new Vector3(max.X, min.Y, min.Z));
        var c = toWorld(new Vector3(max.X, max.Y, min.Z));
        var d = toWorld(new Vector3(min.X, max.Y, min.Z));
        var e = toWorld(new Vector3(min.X, min.Y, max.Z));
        var f = toWorld(new Vector3(max.X, min.Y, max.Z));
        var g = toWorld(new Vector3(max.X, max.Y, max.Z));
        var h = toWorld(new Vector3(min.X, max.Y, max.Z));

        mesh.AddQuad(a, b, c, d, kind, id);
        mesh.AddQuad(e, f, g, h, kind, id);
        mesh.AddQuad(a, e, h, d, kind, id);
        mesh.AddQuad(b, f, g, c, kind, id);
        mesh.AddQuad(d, c, g, h, kind, id);
        mesh.AddQuad(a, b, f, e, kind, id);
    }
}
