namespace Tracos3DStudio;

/// <summary>
/// Padrões de dimensão do projeto (paridade Promob — Configurador de Dimensões).
/// Persistido em <see cref="ProjectMetadata.DimensionSettings"/>.
/// </summary>
public sealed class DimensionConfiguratorSettings
{
    public float MaxWidthMm { get; set; } = 2750f;

    public float MaxHeightMm { get; set; } = 2750f;

    public float MaxDepthMm { get; set; } = 2100f;

    // — Cozinhas — Dimensões Externas (Promob A–O) —

    public float CozinhaInferiorHeightMm { get; set; } = 850f;

    public float CozinhaInferiorDepthMm { get; set; } = 550f;

    /// <summary>C — Superiores Baixos — Altura.</summary>
    public float CozinhaSuperiorBaixoHeightMm { get; set; } = 350f;

    /// <summary>D — Superiores Médios — Altura (legado JSON: cozinhaSuperiorHeightMm).</summary>
    public float CozinhaSuperiorHeightMm { get; set; } = 720f;

    /// <summary>E — Superiores Altos — Altura.</summary>
    public float CozinhaSuperiorAltoHeightMm { get; set; } = 1050f;

    /// <summary>F — Superiores — Profundidade.</summary>
    public float CozinhaSuperiorDepthMm { get; set; } = 350f;

    /// <summary>G — Ilhas — Profundidade.</summary>
    public float CozinhaIlhaDepthMm { get; set; } = 350f;

    /// <summary>H — Despenseiros — Altura.</summary>
    public float CozinhaDespenseiroHeightMm { get; set; } = 1350f;

    /// <summary>I — Despenseiros — Profundidade.</summary>
    public float CozinhaDespenseiroDepthMm { get; set; } = 550f;

    /// <summary>J — Vista p/ Tampo — Altura.</summary>
    public float CozinhaVistaTampoHeightMm { get; set; } = 120f;

    /// <summary>K — Tampo — Avanço.</summary>
    public float CozinhaTampoAvancoMm { get; set; } = 30f;

    /// <summary>L — Rodapés — Recuo.</summary>
    public float CozinhaRodapeRecuoMm { get; set; } = 50f;

    /// <summary>M — Moldura Engrossuramento — Profundidade.</summary>
    public float CozinhaMolduraEngrossProfMm { get; set; } = 18f;

    /// <summary>N — Vista Inferior — Altura.</summary>
    public float CozinhaVistaInferiorHeightMm { get; set; } = 100f;

    /// <summary>O — Vista Inferior — Recuo.</summary>
    public float CozinhaVistaInferiorRecuoMm { get; set; } = 20f;

    // — Dormitórios — Dimensões Externas (Promob A–J) —

    public float DormitorioArmarioHeightMm { get; set; } = 2100f;

    public float DormitorioArmarioDepthMm { get; set; } = 550f;

    /// <summary>C — Bancadas — Altura (cômoda no catálogo Traços).</summary>
    public float DormitorioBancadaHeightMm { get; set; } = 850f;

    /// <summary>D — Bancada — Profundidade.</summary>
    public float DormitorioBancadaDepthMm { get; set; } = 450f;

    public float DormitorioCriadoHeightMm { get; set; } = 550f;

    public float DormitorioCriadoDepthMm { get; set; } = 450f;

    /// <summary>G — Superiores — Altura.</summary>
    public float DormitorioSuperiorHeightMm { get; set; } = 350f;

    /// <summary>H — Superiores — Profundidade.</summary>
    public float DormitorioSuperiorDepthMm { get; set; } = 350f;

    /// <summary>I — Tampo — Avanço.</summary>
    public float DormitorioTampoAvancoMm { get; set; } = 30f;

    /// <summary>J — Moldura Engrossuramento — Profundidade.</summary>
    public float DormitorioMolduraEngrossProfMm { get; set; } = 18f;

    // — Painéis decorativos (Traços L7) —

    public float PainelHeightMm { get; set; } = 2100f;

    public float PainelWidthMm { get; set; } = 800f;

    public float PainelThicknessMm { get; set; } = 18f;

    // — Cozinhas — Chapas (V3.7f Fase 2) —

    public float CozinhaPanelThicknessMm { get; set; } = 18f;

    public float CozinhaBackThicknessMm { get; set; } = 6f;

    public float CozinhaFrontThicknessMm { get; set; } = 18f;

    // — Cozinhas — Montagem caixa inferior —

    public float CozinhaInferiorShelfDepthInsetMm { get; set; } = 20f;

    public float CozinhaInferiorShelfWidthInsetMm { get; set; } = 4f;

    // — Cozinhas — Montagem caixa superior —

    public float CozinhaSuperiorShelfDepthInsetMm { get; set; } = 20f;

    public float CozinhaSuperiorShelfWidthInsetMm { get; set; } = 4f;

    // — Cozinhas — Frentes | Portas —

    public float CozinhaDoorFrontGapMm { get; set; } = 4f;

    /// <summary>Folga entre portas/frentes — módulos superiores (default Promob: 5 mm).</summary>
    public float CozinhaSuperiorDoorFrontGapMm { get; set; } = 5f;

    /// <summary>Folga entre portas/frentes — despenseiros e torres (default Promob: 4 mm).</summary>
    public float CozinhaDespenseiroDoorFrontGapMm { get; set; } = 4f;

    // — Cozinhas — Gavetas —

    public float CozinhaDrawerFrontGapMm { get; set; } = 4f;

    // — Dormitórios — Chapas —

    public float DormitorioPanelThicknessMm { get; set; } = 18f;

    public float DormitorioBackThicknessMm { get; set; } = 6f;

    public float DormitorioFrontThicknessMm { get; set; } = 18f;

    // — Dormitórios — Montagem caixa armários —

    public float DormitorioArmarioShelfDepthInsetMm { get; set; } = 20f;

    public float DormitorioArmarioShelfWidthInsetMm { get; set; } = 4f;

    public float DormitorioArmarioDoorFrontGapMm { get; set; } = 4f;

    public float DormitorioDrawerFrontGapMm { get; set; } = 4f;

    // — Montagem da caixa (V3.7f Fase 3c) —

    public BoxAssemblySectionSettings CozinhaInferiorBox { get; set; } = new();

    public BoxAssemblySectionSettings CozinhaSuperiorBox { get; set; } = new();

    public BoxAssemblySectionSettings CozinhaDespenseiroBox { get; set; } = new();

    public BoxAssemblySectionSettings DormitorioArmarioBox { get; set; } = new();

    public BoxAssemblySectionSettings DormitorioBancadaCriadoBox { get; set; } = new();

    public BoxAssemblySectionSettings DormitorioSuperiorBox { get; set; } = new();

    // — Dormitórios — Frentes | Portas —

    public CozinhaFrentesPortasSettings DormitorioFrentesPortas { get; set; } = new();

    // — Dormitórios — Gavetas —

    public CozinhaGavetasSettings DormitorioGavetas { get; set; } = new();

    // — Chapas por tipo de peça (V3.7f Fase 3b) —

    public CategoryChapaSettings CozinhaChapas { get; set; } =
        CategoryChapaSettings.CreateCozinhaDefaults();

    public CategoryChapaSettings DormitorioChapas { get; set; } =
        CategoryChapaSettings.CreateDormitorioDefaults();

    // — Eletros (V3.7f Fase 3f) —

    public CozinhaEletrosSettings CozinhaEletros { get; set; } = new();

    // — Frentes | Portas (V3.7f Fase 3g) —

    public CozinhaFrentesPortasSettings CozinhaFrentesPortas { get; set; } = new();

    // — Gavetas (V3.7f Fase 3h) —

    public CozinhaGavetasSettings CozinhaGavetas { get; set; } = new();

    // — Gavetas Internas | Auxiliares (V3.7f Fase 3i) —

    public CozinhaGavetasInternasSettings CozinhaGavetasInternas { get; set; } = new();

    // — Cozinhas Cava (V3.7f Fase 3j) —

    public CozinhaCavaSettings CozinhaCava { get; set; } = new();

    public static DimensionConfiguratorSettings CreateDefault() => new();

    public DimensionConfiguratorSettings Clone() => new()
    {
        MaxWidthMm = MaxWidthMm,
        MaxHeightMm = MaxHeightMm,
        MaxDepthMm = MaxDepthMm,
        CozinhaInferiorHeightMm = CozinhaInferiorHeightMm,
        CozinhaInferiorDepthMm = CozinhaInferiorDepthMm,
        CozinhaSuperiorBaixoHeightMm = CozinhaSuperiorBaixoHeightMm,
        CozinhaSuperiorHeightMm = CozinhaSuperiorHeightMm,
        CozinhaSuperiorAltoHeightMm = CozinhaSuperiorAltoHeightMm,
        CozinhaSuperiorDepthMm = CozinhaSuperiorDepthMm,
        CozinhaIlhaDepthMm = CozinhaIlhaDepthMm,
        CozinhaDespenseiroHeightMm = CozinhaDespenseiroHeightMm,
        CozinhaDespenseiroDepthMm = CozinhaDespenseiroDepthMm,
        CozinhaVistaTampoHeightMm = CozinhaVistaTampoHeightMm,
        CozinhaTampoAvancoMm = CozinhaTampoAvancoMm,
        CozinhaRodapeRecuoMm = CozinhaRodapeRecuoMm,
        CozinhaMolduraEngrossProfMm = CozinhaMolduraEngrossProfMm,
        CozinhaVistaInferiorHeightMm = CozinhaVistaInferiorHeightMm,
        CozinhaVistaInferiorRecuoMm = CozinhaVistaInferiorRecuoMm,
        DormitorioArmarioHeightMm = DormitorioArmarioHeightMm,
        DormitorioArmarioDepthMm = DormitorioArmarioDepthMm,
        DormitorioBancadaHeightMm = DormitorioBancadaHeightMm,
        DormitorioBancadaDepthMm = DormitorioBancadaDepthMm,
        DormitorioCriadoHeightMm = DormitorioCriadoHeightMm,
        DormitorioCriadoDepthMm = DormitorioCriadoDepthMm,
        DormitorioSuperiorHeightMm = DormitorioSuperiorHeightMm,
        DormitorioSuperiorDepthMm = DormitorioSuperiorDepthMm,
        DormitorioTampoAvancoMm = DormitorioTampoAvancoMm,
        DormitorioMolduraEngrossProfMm = DormitorioMolduraEngrossProfMm,
        PainelHeightMm = PainelHeightMm,
        PainelWidthMm = PainelWidthMm,
        PainelThicknessMm = PainelThicknessMm,
        CozinhaPanelThicknessMm = CozinhaPanelThicknessMm,
        CozinhaBackThicknessMm = CozinhaBackThicknessMm,
        CozinhaFrontThicknessMm = CozinhaFrontThicknessMm,
        CozinhaInferiorShelfDepthInsetMm = CozinhaInferiorShelfDepthInsetMm,
        CozinhaInferiorShelfWidthInsetMm = CozinhaInferiorShelfWidthInsetMm,
        CozinhaSuperiorShelfDepthInsetMm = CozinhaSuperiorShelfDepthInsetMm,
        CozinhaSuperiorShelfWidthInsetMm = CozinhaSuperiorShelfWidthInsetMm,
        CozinhaDoorFrontGapMm = CozinhaDoorFrontGapMm,
        CozinhaSuperiorDoorFrontGapMm = CozinhaSuperiorDoorFrontGapMm,
        CozinhaDespenseiroDoorFrontGapMm = CozinhaDespenseiroDoorFrontGapMm,
        CozinhaDrawerFrontGapMm = CozinhaDrawerFrontGapMm,
        DormitorioPanelThicknessMm = DormitorioPanelThicknessMm,
        DormitorioBackThicknessMm = DormitorioBackThicknessMm,
        DormitorioFrontThicknessMm = DormitorioFrontThicknessMm,
        DormitorioArmarioShelfDepthInsetMm = DormitorioArmarioShelfDepthInsetMm,
        DormitorioArmarioShelfWidthInsetMm = DormitorioArmarioShelfWidthInsetMm,
        DormitorioArmarioDoorFrontGapMm = DormitorioArmarioDoorFrontGapMm,
        DormitorioDrawerFrontGapMm = DormitorioDrawerFrontGapMm,
        CozinhaInferiorBox = CozinhaInferiorBox.Clone(),
        CozinhaSuperiorBox = CozinhaSuperiorBox.Clone(),
        CozinhaDespenseiroBox = CozinhaDespenseiroBox.Clone(),
        DormitorioArmarioBox = DormitorioArmarioBox.Clone(),
        DormitorioBancadaCriadoBox = DormitorioBancadaCriadoBox.Clone(),
        DormitorioSuperiorBox = DormitorioSuperiorBox.Clone(),
        DormitorioFrentesPortas = DormitorioFrentesPortas.Clone(),
        DormitorioGavetas = DormitorioGavetas.Clone(),
        CozinhaChapas = CozinhaChapas.Clone(),
        DormitorioChapas = DormitorioChapas.Clone(),
        CozinhaEletros = CozinhaEletros.Clone(),
        CozinhaFrentesPortas = CozinhaFrentesPortas.Clone(),
        CozinhaGavetas = CozinhaGavetas.Clone(),
        CozinhaGavetasInternas = CozinhaGavetasInternas.Clone(),
        CozinhaCava = CozinhaCava.Clone()
    };
}

public enum ModuleDimensionSlot
{
    CozinhaInferior,
    CozinhaSuperiorBaixo,
    CozinhaSuperiorMedio,
    CozinhaSuperiorAlto,
    CozinhaDespenseiro,
    CozinhaIlha,
    DormitorioArmario,
    DormitorioBancada,
    DormitorioCriado,
    DormitorioSuperior,
    Painel
}

public enum DimensionConfiguratorApplyScope
{
    /// <summary>Apenas próximas inserções (botão Aplicar).</summary>
    NextInsertionsOnly,

    /// <summary>Itens selecionados + próximas inserções.</summary>
    SelectedAndNext,

    /// <summary>Todos os módulos do ambiente + próximas inserções.</summary>
    AllExistingAndNext
}
