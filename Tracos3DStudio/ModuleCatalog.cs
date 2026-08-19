namespace Tracos3DStudio;

using OpenTK.Mathematics;

public static class ModuleCatalog
{
    private static readonly Dictionary<string, ModuleDefinition> Definitions = BuildDefinitions();

    private static Dictionary<string, ModuleDefinition> BuildDefinitions()
    {
        var definitions = new Dictionary<string, ModuleDefinition>(StringComparer.OrdinalIgnoreCase);

        void Add(ModuleDefinition definition) => definitions[definition.Id] = definition;

        ModuleCatalogInferiores.AddAll(Add);

        // —— Cozinhas → Sup Médios / Altos (stubs até próxima etapa) ——
        Add(new ModuleDefinition
        {
            Id = "aereo",
            DisplayName = "Aéreo 2P 800mm",
            Category = ModuleCategory.Cozinha,
            LibraryGroup = ModuleLibraryHierarchy.GroupSupMedios,
            LibrarySubGroup = ModuleLibraryHierarchy.SubAereos,
            CatalogOrder = 0,
            ShapeKind = ModuleShapeKind.Standard,
            DefaultWidth = 800f,
            DefaultHeight = 720f,
            DefaultDepth = 350f,
            MinWidth = 300f,
            MaxWidth = 1200f,
            MinHeight = 300f,
            MaxHeight = 900f,
            MinDepth = 250f,
            MaxDepth = 450f,
            DoorCount = 2,
            IsWallMounted = true
        });

        Add(new ModuleDefinition
        {
            Id = "despenseiro-2p-600",
            DisplayName = "2P 600mm",
            Category = ModuleCategory.Cozinha,
            LibraryGroup = ModuleLibraryHierarchy.GroupAltos,
            LibrarySubGroup = ModuleLibraryHierarchy.SubEspeciais,
            CatalogOrder = 0,
            ShapeKind = ModuleShapeKind.Standard,
            DefaultWidth = 600f,
            DefaultHeight = 2100f,
            DefaultDepth = 550f,
            MinWidth = 450f,
            MaxWidth = 900f,
            MinHeight = 1800f,
            MaxHeight = 2400f,
            MinDepth = 450f,
            MaxDepth = 650f,
            DoorCount = 2
        });

        // —— Dormitórios ——
        Add(Bedroom("guarda-roupa-2p", "Guarda-roupa 2 Portas", "Armários", 1200f, 2100f, 550f, 2, 0,
            800f, 1800f, 1800f, 2400f));
        Add(Bedroom("criado-mudo", "Criado-mudo 2 Gavetas", "Criados", 500f, 550f, 450f, 0, 2,
            350f, 700f, 400f, 700f, 350f, 550f));
        Add(Bedroom("comoda-4g", "Cômoda 4 Gavetas", "Cômodas", 800f, 850f, 450f, 0, 4,
            600f, 1200f, 700f, 1000f, 350f, 550f));

        // —— Painéis ——
        Add(Panel("painel-liso", "Painel Liso", 800f, 2100f));
        Add(Panel("painel-canaletado", "Painel Canaletado", 800f, 2100f));
        Add(Panel("painel-ripado", "Painel Ripado", 600f, 1800f));

        return definitions;
    }

    private static ModuleDefinition Bedroom(
        string id, string name, string group,
        float w, float h, float d, int doors, int drawers,
        float minW, float maxW, float minH, float maxH,
        float minD = 450f, float maxD = 650f) =>
        new()
        {
            Id = id,
            DisplayName = name,
            Category = ModuleCategory.Dormitorio,
            LibraryGroup = group,
            DefaultWidth = w,
            DefaultHeight = h,
            DefaultDepth = d,
            MinWidth = minW,
            MaxWidth = maxW,
            MinHeight = minH,
            MaxHeight = maxH,
            MinDepth = minD,
            MaxDepth = maxD,
            DoorCount = doors,
            DrawerCount = drawers
        };

    private static ModuleDefinition Panel(string id, string name, float w, float h) =>
        new()
        {
            Id = id,
            DisplayName = name,
            Category = ModuleCategory.Paineis,
            DefaultWidth = w,
            DefaultHeight = h,
            DefaultDepth = 18f,
            MinWidth = 200f,
            MaxWidth = 3000f,
            MinHeight = 200f,
            MaxHeight = 3000f,
            MinDepth = 18f,
            MaxDepth = 18f,
            IsWallMounted = true,
            ShapeKind = ModuleShapeKind.Filler
        };

    private static readonly Dictionary<string, ModuleDefinition> CustomDefinitions =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ModuleDefinition> BuiltInOverrides =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<ModuleDefinition> BuiltIn => Definitions.Values;

    public static IReadOnlyCollection<ModuleDefinition> Custom => CustomDefinitions.Values;

    public static IReadOnlyCollection<ModuleDefinition> All
    {
        get
        {
            var all = new List<ModuleDefinition>(Definitions.Count + CustomDefinitions.Count);
            all.AddRange(Definitions.Values);
            all.AddRange(CustomDefinitions.Values);
            return all;
        }
    }

    public static IEnumerable<ModuleDefinition> GetCozinhaCatalog() =>
        All.Where(definition =>
            definition.Category == ModuleCategory.Cozinha &&
            definition.IsCatalogVisible);

    public static bool IsBuiltIn(string id) => Definitions.ContainsKey(id);

    public static bool IsCustom(string id) => CustomDefinitions.ContainsKey(id);

    public static bool HasBuiltInOverride(string id) => BuiltInOverrides.ContainsKey(id);

    public static void SetBuiltInOverrides(IReadOnlyList<ModuleDefinition> overrides)
    {
        BuiltInOverrides.Clear();

        foreach (var patch in overrides)
        {
            if (!Definitions.TryGetValue(patch.Id, out ModuleDefinition? builtIn))
                continue;

            BuiltInOverrides[patch.Id] = ModuleCatalogOverrideMerger.Merge(builtIn, patch);
        }
    }

    public static void SetCustomModules(IReadOnlyList<ModuleDefinition> modules)
    {
        CustomDefinitions.Clear();

        foreach (var module in modules)
        {
            if (Definitions.ContainsKey(module.Id))
                continue;

            CustomDefinitions[module.Id] = module;
        }
    }

    public static void ResetUserLibrary()
    {
        BuiltInOverrides.Clear();
        CustomDefinitions.Clear();
    }

    public static ModuleDefinition GetRequired(string id) =>
        TryGet(id, out var definition) && definition != null
            ? definition
            : throw new KeyNotFoundException($"Módulo '{id}' não encontrado na biblioteca.");

    public static bool TryGet(string id, out ModuleDefinition? definition)
    {
        if (BuiltInOverrides.TryGetValue(id, out definition))
            return true;

        if (Definitions.TryGetValue(id, out definition))
            return true;

        return CustomDefinitions.TryGetValue(id, out definition);
    }

    public static ModuleInstance CreateInstance(string definitionId, Vector3 position)
    {
        var definition = GetRequired(definitionId);
        var instance = new ModuleInstance
        {
            DefinitionId = definitionId,
            Position = position,
            MaterialId = MaterialCatalog.DefaultMaterialId,
            LayerId = WallLayerCatalog.DefaultModuleLayerId
        };

        instance.ApplyDefinition(definition);
        instance.RebuildMesh(definition);
        return instance;
    }
}
