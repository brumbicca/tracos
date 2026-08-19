namespace Tracos3DStudio;

/// <summary>
/// Hierarquia Cozinhas — ordem das abas como no Promob Plus 5.60
/// (Módulos → Cozinhas → Inferiores → Cantos | Balcões…).
/// </summary>
public static class ModuleLibraryHierarchy
{
    public const string GroupInferiores = "Inferiores";
    public const string GroupAltos = "Altos";
    public const string GroupIlhas = "Ilhas";
    public const string GroupSupBaixos = "Sup Baixos";
    public const string GroupSupMedios = "Sup Médios";
    public const string GroupSupAltos = "Sup Altos";

    // Legado / stub até expandir as demais abas
    public const string GroupSuperiores = GroupSupMedios;
    public const string GroupDespenseiros = GroupAltos;

    public const string SubCantos = "Cantos";
    public const string SubBalcoes = "Balcões";
    public const string SubEspeciais = "Especiais";
    public const string SubGaveteiros = "Gaveteiros";
    public const string SubDiagonais = "Diagonais";
    public const string SubCantoneiras = "Cantoneiras";
    public const string SubFechamentos = "Fechamentos";
    public const string SubAereos = "Aéreos";

    /// <summary>Nível 2 sob Cozinhas — ordem Promob.</summary>
    public static readonly string[] CozinhaGroupOrder =
    [
        GroupInferiores,
        GroupAltos,
        GroupIlhas,
        GroupSupBaixos,
        GroupSupMedios,
        GroupSupAltos
    ];

    /// <summary>Nível 3 sob Inferiores — ordem Promob (imagens MCP).</summary>
    public static readonly string[] InferioresSubGroupOrder =
    [
        SubCantos,
        SubBalcoes,
        SubEspeciais,
        SubGaveteiros,
        SubDiagonais,
        SubCantoneiras,
        SubFechamentos
    ];

    public static readonly IReadOnlyDictionary<string, string[]> CozinhaSubGroupOrder =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [GroupInferiores] = InferioresSubGroupOrder,
            [GroupAltos] = [SubEspeciais],
            [GroupIlhas] = [SubBalcoes],
            [GroupSupBaixos] = [SubAereos],
            [GroupSupMedios] = [SubAereos],
            [GroupSupAltos] = [SubAereos]
        };

    public static string GetBuiltinInsertAutomationId(string definitionId) =>
        definitionId.ToLowerInvariant() switch
        {
            "balcao-2-portas" => "ModuleBalcony2Button",
            "balcao-3-portas" => "ModuleBalcony3Button",
            "gaveteiro" => "ModuleDrawerButton",
            "aereo" => "ModuleWallCabinetButton",
            "guarda-roupa-2p" => "ModuleWardrobeButton",
            "criado-mudo" => "ModuleNightstandButton",
            "comoda-4g" => "ModuleChestButton",
            _ => $"ModuleInsert_{definitionId.Replace('-', '_')}"
        };
}
