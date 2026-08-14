namespace Tracos3DStudio;

/// <summary>
/// Regras de engenharia de modulação de um template (V3.7 — serializável em .tracos-lib).
/// </summary>
public sealed class ModulationRules
{
    public const int CurrentRulesVersion = 1;

    public int RulesVersion { get; set; } = CurrentRulesVersion;

    public string TemplateKind { get; set; } = ModulationTemplateKinds.Box;

    public ModulationStructure Structure { get; set; } = new();

    public List<ModulationPieceRule> Pieces { get; set; } = new();
}

public static class ModulationTemplateKinds
{
    public const string Box = "box";
}

public sealed class ModulationStructure
{
    public float PanelThicknessMm { get; set; } = 18f;

    public float BackThicknessMm { get; set; } = 6f;

    public float FrontThicknessMm { get; set; } = 18f;

    public float FrontGapMm { get; set; } = 4f;

    // — Montagem caixa (overlay configurador V3.7f Fase 3c) —

    public BoxBackPanelType BackPanelType { get; set; } = BoxBackPanelType.EncaixadoSarrafoHorizontal;

    public float BackRecessMm { get; set; } = 8f;

    /// <summary>A — Avanço Fundo sobre Base (mm). Chave Inferior: fbf-afb.</summary>
    public float BackAdvanceOverBaseMm { get; set; }

    /// <summary>B — Avanço Base sobre Fundo (mm). Chave Inferior: fbf-abf.</summary>
    public float BaseAdvanceOverBackMm { get; set; }

    /// <summary>C — Recuo Base (mm). Chave Inferior: fbf-rec-base.</summary>
    public float BaseRecessMm { get; set; }

    /// <summary>E — Avanço Fundo sobre Lateral (mm). Chave Inferior: ffl-afl.</summary>
    public float BackAdvanceOverLateralMm { get; set; }

    /// <summary>F — Avanço Lateral sobre Fundo (mm). Chave Inferior: ffl-alf.</summary>
    public float LateralAdvanceOverBackMm { get; set; }

    public float SarrafoHeightMm { get; set; } = 70f;

    /// <summary>Profundidade/altura do sarrafo traseiro (mm). Independente do dianteiro.</summary>
    public float SarrafoTraseiroHeightMm { get; set; } = 70f;

    /// <summary>Recuo do sarrafo dianteiro em relação à face frontal (mm). 0 = rente à frente.</summary>
    public float SarrafoDianteiroRecessMm { get; set; } = 0f;

    public float SarrafoThicknessMm { get; set; } = 18f;

    public float LateralBaseOverlapMm { get; set; }

    /// <summary>Largura das travessas de fundo (mm). 0 = automático (calculado pela espessura da lateral).</summary>
    public float CrossRailWidthMm { get; set; } = 0f;

    /// <summary>Exibir sarrafo de fundo. False oculta o sarrafo independente do BackPanelType.</summary>
    public bool SarrafoVisible { get; set; } = true;

    /// <summary>Sarrafo dianteiro (frontal) em orientação vertical. False = horizontal (padrão).</summary>
    public bool FrontSarrafoIsVertical { get; set; } = false;

    /// <summary>Sarrafo traseiro em orientação vertical. False = horizontal (padrão).</summary>
    public bool BackSarrafoIsVertical { get; set; } = false;

    /// <summary>Sarrafo dianteiro visível (controlado por A — Tipo Sarrafo: Frontal / Ambos / Inteiro).</summary>
    public bool FrontSarrafoVisible { get; set; } = true;

    /// <summary>Sarrafo traseiro visível (controlado por A — Tipo Sarrafo: Traseiro / Ambos / Inteiro).</summary>
    public bool BackSarrafoVisible { get; set; } = true;

    public List<ModulationFrontBay> FrontBays { get; set; } = new();

    public List<ModulationShelfRule> Shelves { get; set; } = new();
}

public sealed class ModulationFrontBay
{
    public string Id { get; set; } = "";

    public ModulationFrontType Type { get; set; } = ModulationFrontType.Door;

    /// <summary>Fração da largura útil da frente (0–1).</summary>
    public float WidthFraction { get; set; } = 1f;

    /// <summary>Fração da altura útil da frente (0–1).</summary>
    public float HeightFraction { get; set; } = 1f;

    public int StackCount { get; set; } = 1;
}

public sealed class ModulationShelfRule
{
    public string Id { get; set; } = "";

    /// <summary>Posição vertical normalizada (0 = base interna, 1 = topo interno).</summary>
    public float HeightFraction { get; set; } = 0.5f;

    public float DepthInsetMm { get; set; } = 20f;

    public float WidthInsetMm { get; set; } = 4f;
}

public sealed class ModulationPieceRule
{
    public string Id { get; set; } = "";

    public string Role { get; set; } = "";

    public string Name { get; set; } = "";

    public ModulationDimensionBinding Length { get; set; } = new();

    public ModulationDimensionBinding Width { get; set; } = new();

    public ModulationDimensionBinding Thickness { get; set; } = new();

    public int Quantity { get; set; } = 1;

    /// <summary>Fita de borda por face (V3.7d). Null = heurística legada por nome.</summary>
    public ModulationEdgeBanding? EdgeBanding { get; set; }

    /// <summary>Padrão de furação (V3.7d). Auto = heurística legada por nome.</summary>
    public ModulationDrillingPattern DrillingPattern { get; set; } = ModulationDrillingPattern.Auto;
}

/// <summary>Fita de borda por face da peça (V3.7d).</summary>
public sealed class ModulationEdgeBanding
{
    public bool Front { get; set; }

    public bool Back { get; set; }

    public bool Top { get; set; }

    public bool Bottom { get; set; }

    public static ModulationEdgeBanding AllSides() => new() { Front = true, Back = true, Top = true, Bottom = true };

    public static ModulationEdgeBanding FrontOnly() => new() { Front = true };

    public static ModulationEdgeBanding FrontAndTop() => new() { Front = true, Top = true };
}

/// <summary>Padrão de usinagem por peça (V3.7d).</summary>
public enum ModulationDrillingPattern
{
    /// <summary>Heurística legada pelo nome da peça.</summary>
    Auto,

    /// <summary>Sem furos.</summary>
    None,

    /// <summary>Minifix excêntrico em lateral.</summary>
    Lateral,

    /// <summary>Minifix cabo em peça horizontal.</summary>
    Horizontal,

    /// <summary>Dobradiça (copo) em frente de porta.</summary>
    HingeDoor
}

public sealed class ModulationDimensionBinding
{
    public ModulationDimensionSource Source { get; set; } = ModulationDimensionSource.Constant;

    public float ConstantMm { get; set; }

    public float OffsetMm { get; set; }

    public float Scale { get; set; } = 1f;
}

public enum ModulationFrontType
{
    Door,
    Drawer,
    Open
}

public enum ModulationDimensionSource
{
    Constant,
    ModuleWidth,
    ModuleHeight,
    ModuleDepth,
    InnerWidth,
    InnerHeight,
    InnerDepth,
    PanelThickness,
    BackThickness,
    FrontThickness,
    FrontGap
}

/// <summary>
/// Presets de regras — usado em fixtures e migração inferida (V3.7a).
/// </summary>
public static class ModulationRulesPresets
{
    public static ModulationRules CreateStandardBox(int doorCount, int drawerCount, bool includeShelf = true)
    {
        var rules = new ModulationRules
        {
            TemplateKind = ModulationTemplateKinds.Box,
            Structure = BuildStructure(doorCount, drawerCount, includeShelf)
        };

        rules.Pieces.AddRange(BuildBoxPieces(doorCount, drawerCount, includeShelf));
        return rules;
    }

    private static ModulationStructure BuildStructure(int doorCount, int drawerCount, bool includeShelf)
    {
        var structure = new ModulationStructure();
        structure.FrontBays.Clear();

        if (drawerCount > 0)
        {
            float fraction = 1f / drawerCount;
            for (int i = 0; i < drawerCount; i++)
            {
                structure.FrontBays.Add(new ModulationFrontBay
                {
                    Id = $"gaveta-{i + 1}",
                    Type = ModulationFrontType.Drawer,
                    WidthFraction = 1f,
                    HeightFraction = fraction,
                    StackCount = 1
                });
            }

            return structure;
        }

        int fronts = Math.Max(1, doorCount);
        float doorFraction = 1f / fronts;
        for (int i = 0; i < fronts; i++)
        {
            structure.FrontBays.Add(new ModulationFrontBay
            {
                Id = $"porta-{i + 1}",
                Type = ModulationFrontType.Door,
                WidthFraction = doorFraction,
                HeightFraction = 1f,
                StackCount = 1
            });
        }

        if (includeShelf && doorCount > 0)
        {
            structure.Shelves.Add(new ModulationShelfRule
            {
                Id = "prateleira-1",
                HeightFraction = 0.5f
            });
        }

        return structure;
    }

    private static IEnumerable<ModulationPieceRule> BuildBoxPieces(int doorCount, int drawerCount, bool includeShelf)
    {
        yield return Piece(
            "lateral",
            "lateral",
            "Lateral",
            Bind(ModulationDimensionSource.ModuleDepth),
            Bind(ModulationDimensionSource.ModuleHeight),
            Bind(ModulationDimensionSource.PanelThickness),
            2,
            ModulationEdgeBanding.FrontAndTop(),
            ModulationDrillingPattern.Lateral);

        yield return Piece(
            "base",
            "base-inferior",
            "Base inferior",
            Bind(ModulationDimensionSource.InnerWidth),
            Bind(ModulationDimensionSource.InnerDepth),
            Bind(ModulationDimensionSource.PanelThickness),
            edgeBanding: ModulationEdgeBanding.FrontOnly(),
            drillingPattern: ModulationDrillingPattern.Horizontal);

        yield return Piece(
            "tampo",
            "tampo-interno",
            "Tampo interno",
            Bind(ModulationDimensionSource.InnerWidth),
            Bind(ModulationDimensionSource.InnerDepth),
            Bind(ModulationDimensionSource.PanelThickness),
            edgeBanding: ModulationEdgeBanding.FrontOnly(),
            drillingPattern: ModulationDrillingPattern.Horizontal);

        yield return Piece(
            "fundo",
            "fundo",
            "Fundo",
            Bind(ModulationDimensionSource.InnerWidth),
            Bind(ModulationDimensionSource.InnerHeight),
            Bind(ModulationDimensionSource.BackThickness),
            drillingPattern: ModulationDrillingPattern.None);

        if (includeShelf && doorCount > 0 && drawerCount == 0)
        {
            yield return Piece(
                "prateleira",
                "prateleira",
                "Prateleira",
                Bind(ModulationDimensionSource.InnerWidth, offsetMm: -4f),
                Bind(ModulationDimensionSource.InnerDepth, offsetMm: -20f),
                Bind(ModulationDimensionSource.PanelThickness),
                edgeBanding: ModulationEdgeBanding.FrontOnly(),
                drillingPattern: ModulationDrillingPattern.Horizontal);
        }

        if (drawerCount > 0)
        {
            for (int i = 0; i < drawerCount; i++)
            {
                yield return Piece(
                    $"frente-gaveta-{i + 1}",
                    "frente-gaveta",
                    $"Frente gaveta {i + 1}",
                    Bind(ModulationDimensionSource.ModuleWidth, offsetMm: -8f),
                    Bind(ModulationDimensionSource.ModuleHeight, scale: 1f / drawerCount, offsetMm: -4f),
                    Bind(ModulationDimensionSource.FrontThickness),
                    edgeBanding: ModulationEdgeBanding.AllSides(),
                    drillingPattern: ModulationDrillingPattern.None);
            }

            yield break;
        }

        int fronts = Math.Max(1, doorCount);
        for (int i = 0; i < fronts; i++)
        {
            yield return Piece(
                $"frente-porta-{i + 1}",
                "frente-porta",
                $"Frente porta {i + 1}",
                Bind(ModulationDimensionSource.ModuleWidth, scale: 1f / fronts, offsetMm: -4f),
                Bind(ModulationDimensionSource.ModuleHeight, offsetMm: -8f),
                Bind(ModulationDimensionSource.FrontThickness),
                edgeBanding: ModulationEdgeBanding.AllSides(),
                drillingPattern: ModulationDrillingPattern.HingeDoor);
        }
    }

    private static ModulationPieceRule Piece(
        string id,
        string role,
        string name,
        ModulationDimensionBinding length,
        ModulationDimensionBinding width,
        ModulationDimensionBinding thickness,
        int quantity = 1,
        ModulationEdgeBanding? edgeBanding = null,
        ModulationDrillingPattern drillingPattern = ModulationDrillingPattern.Auto) =>
        new()
        {
            Id = id,
            Role = role,
            Name = name,
            Length = length,
            Width = width,
            Thickness = thickness,
            Quantity = quantity,
            EdgeBanding = edgeBanding,
            DrillingPattern = drillingPattern
        };

    private static ModulationDimensionBinding Bind(
        ModulationDimensionSource source,
        float offsetMm = 0f,
        float scale = 1f) =>
        new()
        {
            Source = source,
            OffsetMm = offsetMm,
            Scale = scale
        };
}
