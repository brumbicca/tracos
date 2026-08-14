namespace Tracos3DStudio;

/// <summary>Tipo de montagem do fundo (paridade Promob — Montagem da Caixa).</summary>
public enum BoxBackPanelType
{
  EncaixadoSarrafoHorizontal,
  EncaixadoSarrafoVertical,
  Pregado,
  RebaixadoSarrafoVertical,
  Travessas
}

public static class BoxBackPanelTypeExtensions
{
  public static string DisplayName(this BoxBackPanelType type) => type switch
  {
    BoxBackPanelType.EncaixadoSarrafoHorizontal => "Fundo encaixado — Sarrafo trás horizontal",
    BoxBackPanelType.EncaixadoSarrafoVertical => "Fundo encaixado — Sarrafo trás vertical",
    BoxBackPanelType.Pregado => "Fundo pregado",
    BoxBackPanelType.RebaixadoSarrafoVertical => "Fundo rebaixado — Sarrafo trás vertical",
    BoxBackPanelType.Travessas => "Fundo travessas",
    _ => type.ToString()
  };

  public static bool UsesSarrafo(this BoxBackPanelType type) =>
    type is BoxBackPanelType.EncaixadoSarrafoHorizontal
      or BoxBackPanelType.EncaixadoSarrafoVertical
      or BoxBackPanelType.RebaixadoSarrafoVertical;
}

/// <summary>Parâmetros de montagem de caixa por seção (V3.7f Fase 3c).</summary>
public sealed class BoxAssemblySectionSettings
{
  public BoxBackPanelType BackPanelType { get; set; } = BoxBackPanelType.EncaixadoSarrafoHorizontal;

  /// <summary>Recuo/ranhura do fundo encaixado (mm).</summary>
  public float BackRecessMm { get; set; } = 8f;

  public float SarrafoHeightMm { get; set; } = 70f;

  public float SarrafoThicknessMm { get; set; } = 18f;

  /// <summary>Fixação lateral sobre base — recuo/superposição (mm).</summary>
  public float LateralBaseOverlapMm { get; set; } = 0f;

  public float ShelfDepthInsetMm { get; set; } = 20f;

  public float ShelfWidthInsetMm { get; set; } = 4f;

  /// <summary>Campos numéricos da árvore "Montagem da Caixa - Inferior" (paridade Promob).</summary>
  public Dictionary<string, float> InferiorNumeric { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos de combo/tipo da árvore "Montagem da Caixa - Inferior" (paridade Promob).</summary>
  public Dictionary<string, string> InferiorChoice { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos numéricos da árvore "Montagem da Caixa - Superior" (paridade Promob).</summary>
  public Dictionary<string, float> SuperiorNumeric { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos de combo/tipo da árvore "Montagem da Caixa - Superior" (paridade Promob).</summary>
  public Dictionary<string, string> SuperiorChoice { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos numéricos da árvore "Montagem da Caixa - Despenseiros | Torres" (paridade Promob).</summary>
  public Dictionary<string, float> DespenseirosNumeric { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos de combo/tipo da árvore "Montagem da Caixa - Despenseiros | Torres" (paridade Promob).</summary>
  public Dictionary<string, string> DespenseirosChoice { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos numéricos da árvore "Montagem de Caixa - Armários" (Dormitórios — paridade Promob).</summary>
  public Dictionary<string, float> ArmarioNumeric { get; set; } = new(StringComparer.Ordinal);

  /// <summary>Campos de combo/tipo da árvore "Montagem de Caixa - Armários" (Dormitórios — paridade Promob).</summary>
  public Dictionary<string, string> ArmarioChoice { get; set; } = new(StringComparer.Ordinal);

  public BoxAssemblySectionSettings Clone() => new()
  {
    BackPanelType = BackPanelType,
    BackRecessMm = BackRecessMm,
    SarrafoHeightMm = SarrafoHeightMm,
    SarrafoThicknessMm = SarrafoThicknessMm,
    LateralBaseOverlapMm = LateralBaseOverlapMm,
    ShelfDepthInsetMm = ShelfDepthInsetMm,
    ShelfWidthInsetMm = ShelfWidthInsetMm,
    InferiorNumeric = new Dictionary<string, float>(InferiorNumeric, StringComparer.Ordinal),
    InferiorChoice = new Dictionary<string, string>(InferiorChoice, StringComparer.Ordinal),
    SuperiorNumeric = new Dictionary<string, float>(SuperiorNumeric, StringComparer.Ordinal),
    SuperiorChoice = new Dictionary<string, string>(SuperiorChoice, StringComparer.Ordinal),
    DespenseirosNumeric = new Dictionary<string, float>(DespenseirosNumeric, StringComparer.Ordinal),
    DespenseirosChoice = new Dictionary<string, string>(DespenseirosChoice, StringComparer.Ordinal),
    ArmarioNumeric = new Dictionary<string, float>(ArmarioNumeric, StringComparer.Ordinal),
    ArmarioChoice = new Dictionary<string, string>(ArmarioChoice, StringComparer.Ordinal)
  };
}

public static class BoxAssemblyNodeKinds
{
  public const string Fundo = "fundo";
  public const string FixacaoLateralBase = "fix-lat-base";
  public const string Sarrafo = "sarrafo";
  public const string Prateleira = "prateleira";

  public static readonly string[] CozinhaInferior =
    [Fundo, FixacaoLateralBase, Sarrafo, Prateleira];

  public static readonly string[] CozinhaSuperior =
    [Fundo, Prateleira];

  public static readonly string[] DormitorioArmario =
    [Fundo, FixacaoLateralBase, Prateleira];

  public static string DisplayName(string kind) => kind switch
  {
    Fundo => "Fundo",
    FixacaoLateralBase => "Fixação Lateral — Base",
    Sarrafo => "Sarrafo",
    Prateleira => "Prateleira",
    _ => kind
  };
}
