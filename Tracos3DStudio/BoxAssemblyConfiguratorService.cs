namespace Tracos3DStudio;

/// <summary>Montagem da caixa — tipo de fundo e fixações (V3.7f Fase 3c).</summary>
public static class BoxAssemblyConfiguratorService
{
  public static BoxAssemblySectionSettings GetSection(
    DimensionConfiguratorSettings settings,
    ModuleDimensionSlot slot) =>
    slot switch
    {
      ModuleDimensionSlot.CozinhaInferior or ModuleDimensionSlot.CozinhaIlha => settings.CozinhaInferiorBox,
      ModuleDimensionSlot.CozinhaDespenseiro => settings.CozinhaDespenseiroBox,
      ModuleDimensionSlot.CozinhaSuperiorBaixo or ModuleDimensionSlot.CozinhaSuperiorMedio
        or ModuleDimensionSlot.CozinhaSuperiorAlto => settings.CozinhaSuperiorBox,
      ModuleDimensionSlot.DormitorioArmario => settings.DormitorioArmarioBox,
      ModuleDimensionSlot.DormitorioBancada or ModuleDimensionSlot.DormitorioCriado
        => settings.DormitorioBancadaCriadoBox,
      ModuleDimensionSlot.DormitorioSuperior => settings.DormitorioSuperiorBox,
      _ => settings.CozinhaInferiorBox
    };

  public static BoxAssemblySectionSettings GetSectionForDefinition(
    ModuleDefinition definition,
    DimensionConfiguratorSettings settings) =>
    GetSection(settings, DimensionConfiguratorService.ResolveSlot(definition));

  public static void EnsureBoxInitialized(DimensionConfiguratorSettings settings)
  {
    MigrateCozinhaBlindCornerDefaults(settings.CozinhaInferiorBox);

    MigrateShelfFromLegacyIfDefault(settings.CozinhaInferiorBox,
      settings.CozinhaInferiorShelfDepthInsetMm,
      settings.CozinhaInferiorShelfWidthInsetMm);
    MigrateShelfFromLegacyIfDefault(settings.CozinhaSuperiorBox,
      settings.CozinhaSuperiorShelfDepthInsetMm,
      settings.CozinhaSuperiorShelfWidthInsetMm);
    MigrateShelfFromLegacyIfDefault(settings.DormitorioArmarioBox,
      settings.DormitorioArmarioShelfDepthInsetMm,
      settings.DormitorioArmarioShelfWidthInsetMm);

    EnsureInferiorInitialized(settings.CozinhaInferiorBox);
    EnsureSuperiorInitialized(settings.CozinhaSuperiorBox);
    EnsureDespenseirosInitialized(settings.CozinhaDespenseiroBox, settings.CozinhaInferiorBox);
    EnsureArmarioInitialized(settings.DormitorioArmarioBox);
    EnsureInferiorInitialized(settings.DormitorioBancadaCriadoBox);
    EnsureSuperiorInitialized(settings.DormitorioSuperiorBox);
  }

  private static void MigrateCozinhaBlindCornerDefaults(BoxAssemblySectionSettings box)
  {
    const int currentVersion = 1;
    if (box.BlindCornerDefaultsVersion >= currentVersion)
      return;

    static bool IsMissingOrZero(IReadOnlyDictionary<string, float> values, string key) =>
      !values.TryGetValue(key, out float value) || MathF.Abs(value) < 0.001f;

    static bool IsMissingOr(
      IReadOnlyDictionary<string, string> values,
      string key,
      params string[] accepted) =>
      !values.TryGetValue(key, out string? value)
      || accepted.Any(option => value.Equals(option, StringComparison.OrdinalIgnoreCase));

    // Perfis gravados pela fase anterior tinham todos os parâmetros do canto reto zerados.
    // Só migra os valores nesse estado reconhecível; personalizações do usuário são preservadas.
    bool legacyDefaults =
      IsMissingOrZero(box.InferiorNumeric, "cr-affb")
      && IsMissingOrZero(box.InferiorNumeric, "cr-affs")
      && IsMissingOrZero(box.InferiorNumeric, "cr-affl")
      && IsMissingOrZero(box.InferiorNumeric, "cr-affd")
      && IsMissingOrZero(box.InferiorNumeric, "cr-ava-por")
      && IsMissingOrZero(box.InferiorNumeric, "crf-recuo-fro")
      && IsMissingOrZero(box.InferiorNumeric, "cr-afa-lat")
      && IsMissingOr(box.InferiorChoice, "cr-uso-dist", "Não", "Não usar")
      && IsMissingOr(box.InferiorChoice, "crs-tipo-fro", "Parcial");

    if (legacyDefaults)
    {
      box.InferiorNumeric["cr-affb"] = 18f;
      box.InferiorNumeric["cr-affs"] = 18f;
      box.InferiorNumeric["cr-affl"] = 18f;
      box.InferiorNumeric["cr-affd"] = -12f;
      box.InferiorNumeric["cr-ava-por"] = 27f;
      box.InferiorNumeric["crf-recuo-fro"] = 80f;
      box.InferiorNumeric["crf-dim-fro"] = 30f;
      box.InferiorNumeric["cr-afa-lat"] = 30f;
      box.InferiorChoice["cr-uso-dist"] = "Usar";
      box.InferiorChoice["crs-tipo-fro"] = "Total";
    }

    // Normaliza os rótulos antigos sem alterar o significado dos perfis existentes.
    NormalizeUseChoice(box.InferiorChoice, "cr-uso-dist");
    NormalizeUseChoice(box.InferiorChoice, "crf-sup");
    NormalizeUseChoice(box.InferiorChoice, "crf-inf");
    NormalizeUseChoice(box.InferiorChoice, "crf-tra");
    if (box.InferiorChoice.TryGetValue("crs-tipo-fro", out string? sarrafo)
        && sarrafo.Equals("Inteiro", StringComparison.OrdinalIgnoreCase))
      box.InferiorChoice["crs-tipo-fro"] = "Total";

    box.BlindCornerDefaultsVersion = currentVersion;
  }

  private static void NormalizeUseChoice(Dictionary<string, string> values, string key)
  {
    if (!values.TryGetValue(key, out string? value))
      return;

    if (value.Equals("Sim", StringComparison.OrdinalIgnoreCase))
      values[key] = "Usar";
    else if (value.Equals("Não", StringComparison.OrdinalIgnoreCase))
      values[key] = "Não usar";
  }

  /// <summary>
  /// Preenche os campos da árvore "Montagem de Caixa - Armários" (Dormitórios) com defaults do schema
  /// e migra chaves legadas de inferiorNumeric quando existirem.
  /// </summary>
  public static void EnsureArmarioInitialized(BoxAssemblySectionSettings box)
  {
    MigrateArmarioFromInferiorIfNeeded(box);

    if (!box.ArmarioChoice.ContainsKey("fundo-tipo"))
      box.ArmarioChoice["fundo-tipo"] = PromobFundoFromBackPanel(box.BackPanelType);
    if (!box.ArmarioNumeric.ContainsKey("fundo-recuo"))
      box.ArmarioNumeric["fundo-recuo"] = box.BackRecessMm;

    foreach (var node in BoxAssemblyArmarioSchema.AllNodes())
    {
      foreach (var field in node.Fields)
      {
        if (field.Kind == BoxFieldKind.Numeric)
        {
          if (!box.ArmarioNumeric.ContainsKey(field.Key))
            box.ArmarioNumeric[field.Key] = field.DefaultValue;
        }
        else if (!box.ArmarioChoice.ContainsKey(field.Key))
        {
          box.ArmarioChoice[field.Key] = field.DefaultOption;
        }
      }
    }
  }

  /// <summary>Aplica os campos-ponte da árvore Armários de volta aos campos legados (mantém o 3D).</summary>
  public static void SyncArmarioToLegacy(BoxAssemblySectionSettings box)
  {
    if (box.ArmarioChoice.TryGetValue("fundo-tipo", out var tipo))
      box.BackPanelType = BackPanelFromPromobFundoInferior(tipo);
    if (box.ArmarioNumeric.TryGetValue("fundo-recuo", out var recuo))
      box.BackRecessMm = recuo;
  }

  private static void MigrateArmarioFromInferiorIfNeeded(BoxAssemblySectionSettings box)
  {
    if (box.ArmarioNumeric.Count > 0 || box.ArmarioChoice.Count > 0)
      return;

    foreach (var node in BoxAssemblyArmarioSchema.AllNodes())
    {
      foreach (var field in node.Fields)
      {
        if (field.Kind == BoxFieldKind.Numeric)
        {
          if (box.InferiorNumeric.TryGetValue(field.Key, out var v))
            box.ArmarioNumeric[field.Key] = v;
        }
        else if (box.InferiorChoice.TryGetValue(field.Key, out var s))
        {
          box.ArmarioChoice[field.Key] = s;
        }
      }
    }
  }

  /// <summary>
  /// Preenche os campos da árvore "Montagem da Caixa - Inferior" com defaults do schema
  /// (sem sobrescrever valores já persistidos) e semeia os campos legados (bridge 3D).
  /// </summary>
  public static void EnsureInferiorInitialized(BoxAssemblySectionSettings box)
  {
    // Semeia os campos-ponte a partir dos campos legados quando ainda não existem
    // (primeira abertura). Depois disso, o valor persistido do usuário prevalece.
    if (!box.InferiorChoice.ContainsKey("fundo-tipo"))
      box.InferiorChoice["fundo-tipo"] = PromobFundoFromBackPanel(box.BackPanelType);
    if (!box.InferiorNumeric.ContainsKey("fundo-recuo"))
      box.InferiorNumeric["fundo-recuo"] = box.BackRecessMm;
    if (!box.InferiorNumeric.ContainsKey("sar-prof-fro"))
      box.InferiorNumeric["sar-prof-fro"] = box.SarrafoHeightMm;
    if (!box.InferiorNumeric.ContainsKey("fix-lb-alb"))
      box.InferiorNumeric["fix-lb-alb"] = box.LateralBaseOverlapMm;
    if (!box.InferiorNumeric.ContainsKey("prat-recuo"))
      box.InferiorNumeric["prat-recuo"] = box.ShelfDepthInsetMm;
    if (!box.InferiorNumeric.ContainsKey("prat-folga"))
      box.InferiorNumeric["prat-folga"] = box.ShelfWidthInsetMm;

    // Demais campos: default do schema quando ausentes.
    foreach (var node in BoxAssemblyInferiorSchema.AllNodes())
    {
      foreach (var field in node.Fields)
      {
        if (field.Kind == BoxFieldKind.Numeric)
        {
          if (!box.InferiorNumeric.ContainsKey(field.Key))
            box.InferiorNumeric[field.Key] = field.DefaultValue;
        }
        else if (!box.InferiorChoice.ContainsKey(field.Key))
        {
          box.InferiorChoice[field.Key] = field.DefaultOption;
        }
      }
    }
  }

  /// <summary>Aplica os campos-ponte da árvore Inferior de volta aos campos legados (mantém o 3D).</summary>
  public static void SyncInferiorToLegacy(BoxAssemblySectionSettings box)
  {
    if (box.InferiorChoice.TryGetValue("fundo-tipo", out var tipo))
    {
      var backPanelType = BackPanelFromPromobFundoInferior(tipo);
      // sar-sent-tra define a orientação do sarrafo TRASEIRO (back assembly).
      if (backPanelType is BoxBackPanelType.EncaixadoSarrafoHorizontal
                        or BoxBackPanelType.EncaixadoSarrafoVertical
          && box.InferiorChoice.TryGetValue("sar-sent-tra", out var sentidoTra))
      {
        backPanelType = sentidoTra == "Vertical"
          ? BoxBackPanelType.EncaixadoSarrafoVertical
          : BoxBackPanelType.EncaixadoSarrafoHorizontal;
      }
      box.BackPanelType = backPanelType;
    }

    if (box.InferiorNumeric.TryGetValue("fundo-recuo", out var recuo))
      box.BackRecessMm = recuo;
    if (box.InferiorNumeric.TryGetValue("sar-prof-fro", out var sarH))
      box.SarrafoHeightMm = sarH;
    if (box.InferiorNumeric.TryGetValue("fix-lb-alb", out var alb))
      box.LateralBaseOverlapMm = alb;
    if (box.InferiorNumeric.TryGetValue("prat-recuo", out var prat))
      box.ShelfDepthInsetMm = prat;
    if (box.InferiorNumeric.TryGetValue("prat-folga", out var folga))
      box.ShelfWidthInsetMm = folga;
  }

  /// <summary>
  /// Preenche os campos da árvore "Montagem da Caixa - Superior" com defaults do schema
  /// (sem sobrescrever valores já persistidos) e semeia os campos legados (bridge 3D).
  /// </summary>
  public static void EnsureSuperiorInitialized(BoxAssemblySectionSettings box)
  {
    if (!box.SuperiorChoice.ContainsKey("fundo-tipo"))
      box.SuperiorChoice["fundo-tipo"] = PromobFundoSuperiorFromBackPanel(box.BackPanelType);
    if (!box.SuperiorNumeric.ContainsKey("fundo-recuo"))
      box.SuperiorNumeric["fundo-recuo"] = box.BackRecessMm;
    if (!box.SuperiorNumeric.ContainsKey("prat-recuo"))
      box.SuperiorNumeric["prat-recuo"] = box.ShelfDepthInsetMm;
    if (!box.SuperiorNumeric.ContainsKey("prat-folga"))
      box.SuperiorNumeric["prat-folga"] = box.ShelfWidthInsetMm;

    foreach (var node in BoxAssemblySuperiorSchema.AllNodes())
    {
      foreach (var field in node.Fields)
      {
        if (field.Kind == BoxFieldKind.Numeric)
        {
          if (!box.SuperiorNumeric.ContainsKey(field.Key))
            box.SuperiorNumeric[field.Key] = field.DefaultValue;
        }
        else if (!box.SuperiorChoice.ContainsKey(field.Key))
        {
          box.SuperiorChoice[field.Key] = field.DefaultOption;
        }
      }
    }
  }

  /// <summary>Aplica os campos-ponte da árvore Superior de volta aos campos legados (mantém o 3D).</summary>
  public static void SyncSuperiorToLegacy(BoxAssemblySectionSettings box)
  {
    if (box.SuperiorChoice.TryGetValue("fundo-tipo", out var tipo))
      box.BackPanelType = BackPanelFromPromobFundoSuperior(tipo);
    if (box.SuperiorNumeric.TryGetValue("fundo-recuo", out var recuo))
      box.BackRecessMm = recuo;
    if (box.SuperiorNumeric.TryGetValue("prat-recuo", out var prat))
      box.ShelfDepthInsetMm = prat;
    if (box.SuperiorNumeric.TryGetValue("prat-folga", out var folga))
      box.ShelfWidthInsetMm = folga;
  }

  /// <summary>
  /// Preenche os campos da árvore "Montagem da Caixa - Despenseiros | Torres" com defaults do schema
  /// (sem sobrescrever valores já persistidos) e semeia os campos legados (bridge 3D).
  /// </summary>
  public static void EnsureDespenseirosInitialized(
    BoxAssemblySectionSettings box,
    BoxAssemblySectionSettings legacyInferiorSeed)
  {
    if (!box.DespenseirosChoice.ContainsKey("fundo-tipo"))
      box.DespenseirosChoice["fundo-tipo"] =
        PromobFundoSuperiorFromBackPanel(legacyInferiorSeed.BackPanelType);
    if (!box.DespenseirosNumeric.ContainsKey("fundo-recuo"))
      box.DespenseirosNumeric["fundo-recuo"] = legacyInferiorSeed.BackRecessMm;
    if (!box.DespenseirosNumeric.ContainsKey("prat-recuo-fro"))
      box.DespenseirosNumeric["prat-recuo-fro"] = legacyInferiorSeed.ShelfDepthInsetMm;
    if (!box.DespenseirosNumeric.ContainsKey("prat-folga"))
      box.DespenseirosNumeric["prat-folga"] = legacyInferiorSeed.ShelfWidthInsetMm;

    foreach (var node in BoxAssemblyDespenseirosSchema.AllNodes())
    {
      foreach (var field in node.Fields)
      {
        if (field.Kind == BoxFieldKind.Numeric)
        {
          if (!box.DespenseirosNumeric.ContainsKey(field.Key))
            box.DespenseirosNumeric[field.Key] = field.DefaultValue;
        }
        else if (!box.DespenseirosChoice.ContainsKey(field.Key))
        {
          box.DespenseirosChoice[field.Key] = field.DefaultOption;
        }
      }
    }
  }

  /// <summary>Aplica os campos-ponte da árvore Despenseiros de volta aos campos legados (mantém o 3D).</summary>
  public static void SyncDespenseirosToLegacy(BoxAssemblySectionSettings box)
  {
    if (box.DespenseirosChoice.TryGetValue("fundo-tipo", out var tipo))
      box.BackPanelType = BackPanelFromPromobFundoSuperior(tipo);
    if (box.DespenseirosNumeric.TryGetValue("fundo-recuo", out var recuo))
      box.BackRecessMm = recuo;
    if (box.DespenseirosNumeric.TryGetValue("prat-recuo-fro", out var prat))
      box.ShelfDepthInsetMm = prat;
    if (box.DespenseirosNumeric.TryGetValue("prat-folga", out var folga))
      box.ShelfWidthInsetMm = folga;
  }

  private static string PromobFundoFromBackPanel(BoxBackPanelType type) => type switch
  {
    BoxBackPanelType.EncaixadoSarrafoVertical => "Trav Vertical",
    BoxBackPanelType.RebaixadoSarrafoVertical => "Rebaixado",
    BoxBackPanelType.Travessas => "Trav Vertical",
    BoxBackPanelType.Pregado => "Sem fundo",
    _ => "Inteiro"
  };

  private static BoxBackPanelType BackPanelFromPromobFundoInferior(string tipo) => tipo switch
  {
    "Rebaixado" => BoxBackPanelType.RebaixadoSarrafoVertical,
    "Trav Vertical" => BoxBackPanelType.EncaixadoSarrafoVertical,
    "Trav Horizontal" => BoxBackPanelType.EncaixadoSarrafoHorizontal,
    "Sem fundo" => BoxBackPanelType.Pregado,
    _ => BoxBackPanelType.EncaixadoSarrafoHorizontal
  };

  private static string PromobFundoSuperiorFromBackPanel(BoxBackPanelType type) =>
    type == BoxBackPanelType.Pregado ? "Sem fundo" : "Inteiro";

  private static BoxBackPanelType BackPanelFromPromobFundoSuperior(string tipo) =>
    tipo == "Sem fundo" ? BoxBackPanelType.Pregado : BoxBackPanelType.EncaixadoSarrafoHorizontal;

  private static void MigrateShelfFromLegacyIfDefault(
    BoxAssemblySectionSettings box,
    float legacyDepthInsetMm,
    float legacyWidthInsetMm)
  {
    if (box.ShelfDepthInsetMm == 20f && Math.Abs(legacyDepthInsetMm - 20f) > 0.01f)
      box.ShelfDepthInsetMm = legacyDepthInsetMm;

    if (box.ShelfWidthInsetMm == 4f && Math.Abs(legacyWidthInsetMm - 4f) > 0.01f)
      box.ShelfWidthInsetMm = legacyWidthInsetMm;
  }

  public static void SyncLegacyShelfFields(DimensionConfiguratorSettings settings)
  {
    settings.CozinhaInferiorShelfDepthInsetMm = settings.CozinhaInferiorBox.ShelfDepthInsetMm;
    settings.CozinhaInferiorShelfWidthInsetMm = settings.CozinhaInferiorBox.ShelfWidthInsetMm;
    settings.CozinhaSuperiorShelfDepthInsetMm = settings.CozinhaSuperiorBox.ShelfDepthInsetMm;
    settings.CozinhaSuperiorShelfWidthInsetMm = settings.CozinhaSuperiorBox.ShelfWidthInsetMm;
    settings.DormitorioArmarioShelfDepthInsetMm = settings.DormitorioArmarioBox.ShelfDepthInsetMm;
    settings.DormitorioArmarioShelfWidthInsetMm = settings.DormitorioArmarioBox.ShelfWidthInsetMm;
  }

  public static void ApplyToStructure(
    ModulationStructure structure,
    ModuleDefinition definition,
    DimensionConfiguratorSettings settings)
  {
    var slot = DimensionConfiguratorService.ResolveSlot(definition);
    var section = GetSectionForDefinition(definition, settings);

    // Garante que BackPanelType está sempre derivado dos choices mais recentes,
    // independente de quem chamou e se SyncToLegacy já foi chamado.
    switch (slot)
    {
      case ModuleDimensionSlot.CozinhaInferior or ModuleDimensionSlot.CozinhaIlha:
        SyncInferiorToLegacy(section);
        break;
      case ModuleDimensionSlot.CozinhaSuperiorBaixo or ModuleDimensionSlot.CozinhaSuperiorMedio
        or ModuleDimensionSlot.CozinhaSuperiorAlto:
        SyncSuperiorToLegacy(section);
        break;
      case ModuleDimensionSlot.CozinhaDespenseiro:
        SyncDespenseirosToLegacy(section);
        break;
    }

    structure.BackPanelType = section.BackPanelType;
    structure.BackRecessMm = section.BackRecessMm;
    structure.SarrafoHeightMm = section.SarrafoHeightMm;
    structure.SarrafoThicknessMm =
      ChapaConfiguratorService.GetThickness(ChapaPieceKinds.CompSarrafo, definition, settings);
    structure.LateralBaseOverlapMm = section.LateralBaseOverlapMm;

    var (activeNumeric, activeChoice) = GetActiveDicts(section, slot);

    if (activeChoice.TryGetValue("fundo-tipo", out var fundoTipo))
    {
      structure.BackPanelLayout = fundoTipo switch
      {
        "Rebaixado" => BoxBackPanelLayout.Rebaixado,
        "Trav Vertical" => BoxBackPanelLayout.TravessaVertical,
        "Trav Horizontal" => BoxBackPanelLayout.TravessaHorizontal,
        "Sem fundo" => BoxBackPanelLayout.SemFundo,
        _ => BoxBackPanelLayout.Inteiro
      };
    }

    structure.BackHeightRecessMm = ReadSigned(activeNumeric, "fundo-rebaixo");
    structure.BackUpperRailOffsetMm = ReadSigned(activeNumeric, "fundo-afa-sup");
    structure.BackLowerRailOffsetMm = ReadSigned(activeNumeric, "fundo-afa-inf");
    structure.BackSupportRailWidthMm = ReadNonNeg(activeNumeric, "fundo-dim-trav-sust");
    structure.BackSupportRailCount = activeChoice.TryGetValue("fundo-trav-sust", out var sust) switch
    {
      true when sust == "2" => 2,
      true when sust is "1" or "Sim" => 1,
      _ => 0
    };

    // Fixação Base - Fundo / Fundo - Lateral (mesmo local do configurador Inferior).
    structure.BackAdvanceOverBaseMm = ReadSigned(activeNumeric, "fbf-afb");
    structure.BaseAdvanceOverBackMm = ReadSigned(activeNumeric, "fbf-abf");
    structure.BaseRecessMm = ReadSigned(activeNumeric, "fbf-rec-base");
    structure.BackAdvanceOverLateralMm = ReadSigned(activeNumeric, "ffl-afl");
    structure.LateralAdvanceOverBackMm = ReadSigned(activeNumeric, "ffl-alf");
    structure.BackAdvanceOverDivisionMm = ReadSigned(activeNumeric, "ffd-afd");
    structure.BaseAdvanceOverLateralMm = ReadSigned(activeNumeric, "fix-lb-abl");
    structure.LateralBottomRecessMm = ReadSigned(activeNumeric, "lat-rebaixo");
    structure.LateralDepthGapMm = ReadSigned(activeNumeric, "lat-folga");
    structure.LateralDepthAlignment = activeChoice.TryGetValue("lat-alinhamento", out var alignment)
      ? alignment switch
      {
        "Frente" => LateralDepthAlignment.Front,
        "Centro" => LateralDepthAlignment.Center,
        _ => LateralDepthAlignment.Back
      }
      : LateralDepthAlignment.Back;

    // Superior/Despenseiros: chaves com sufixo -inf quando existirem.
    if (!activeNumeric.ContainsKey("fbf-afb") && activeNumeric.TryGetValue("fbf-afb-inf", out var afbInf))
      structure.BackAdvanceOverBaseMm = afbInf;

    // fundo-dim-trav → largura das travessas (0 = automático no BuildBackAssembly)
    if (activeNumeric.TryGetValue("fundo-dim-trav", out var dimTrav) && dimTrav > 0f)
      structure.CrossRailWidthMm = dimTrav;
    else
      structure.CrossRailWidthMm = 0f;

    // sar-tipo → visibilidade individual por sarrafo
    activeChoice.TryGetValue("sar-tipo", out var sarTipo);
    structure.SarrafoVisible     = sarTipo != "Sem sarrafo";
    structure.SarrafoWhole = sarTipo == "Inteiro";
    structure.FrontSarrafoVisible = !structure.SarrafoWhole && sarTipo is (null or "Frontal" or "Ambos");
    structure.BackSarrafoVisible  = !structure.SarrafoWhole && sarTipo is ("Traseiro" or "Ambos");

    activeChoice.TryGetValue("sar-seg", out var sarSeg);
    structure.FrontSarrafoSegmented = sarSeg is "Frontal" or "Ambos" or "Inteiro";
    structure.BackSarrafoSegmented = sarSeg is "Traseiro" or "Ambos" or "Inteiro";
    structure.SarrafoChamfered = activeChoice.TryGetValue("sar-formato", out var formato)
                                 && formato.Equals("Chanfrado", StringComparison.OrdinalIgnoreCase);
    structure.SarrafoAdvanceOverLateralMm = ReadSigned(activeNumeric, "fsl-asl");
    structure.SarrafoAdvanceOverBackMm = ReadSigned(activeNumeric, "fsfi-asf");
    structure.BackAdvanceOverSarrafoMm = ReadSigned(activeNumeric, "fsfi-afs");
    structure.BackSarrafoRecessMm = ReadSigned(activeNumeric, "fsfr-recuo");
    structure.BackSarrafoLowerRecessMm = ReadSigned(activeNumeric, "fsfr-rebaixo");
    structure.LateralAdvanceOverFrontPanelMm = ReadSigned(activeNumeric, "fpfl-alf");
    structure.FrontPanelAdvanceOverLateralMm = ReadSigned(activeNumeric, "fpfl-afl");

    structure.DivisionFrontInsetMm = ReadSigned(activeNumeric, "div-recuo-fro");
    structure.DivisionMovableBackInsetMm = ReadSigned(activeNumeric, "div-recuo-tra-mov");
    structure.DivisionFixedBackInsetMm = ReadSigned(activeNumeric, "div-recuo-tra-fix");
    structure.DivisionBottomRecessMm = ReadSigned(activeNumeric, "div-rebaixo");
    structure.DivisionSpacerWidthMm = ReadNonNeg(activeNumeric, "div-dim-dist");

    // sar-sent-fro → orientação do sarrafo DIANTEIRO (frontal)
    structure.FrontSarrafoIsVertical = activeChoice.TryGetValue("sar-sent-fro", out var sentFro)
                                       && (sentFro == "Vertical" || sentFro == "Trav Vertical");

    // sar-sent-tra → orientação do sarrafo TRASEIRO (independente do dianteiro)
    structure.BackSarrafoIsVertical = activeChoice.TryGetValue("sar-sent-tra", out var sentTra)
                                      && (sentTra == "Vertical" || sentTra == "Trav Vertical");

    // sar-prof-fro / sar-prof-tra → profundidade/altura independentes por sarrafo
    if (activeNumeric.TryGetValue("sar-prof-fro", out var profFro) && profFro > 0f)
      structure.SarrafoHeightMm = profFro;
    if (activeNumeric.TryGetValue("sar-prof-tra", out var profTra) && profTra > 0f)
      structure.SarrafoTraseiroHeightMm = profTra;

    // sar-recuo-fro → recuo opcional do sarrafo dianteiro (0 = rente à frente)
    structure.SarrafoDianteiroRecessMm = ReadSigned(activeNumeric, "sar-recuo-fro");

    foreach (var shelf in structure.Shelves)
    {
      shelf.DepthInsetMm = section.ShelfDepthInsetMm;
      shelf.WidthInsetMm = section.ShelfWidthInsetMm;
      shelf.BackInsetMm = shelf.IsFixed
        ? ReadSigned(activeNumeric, "prat-recuo-tra-fix")
        : ReadSigned(activeNumeric, "prat-recuo-tra-mov");
    }


    if (slot is ModuleDimensionSlot.CozinhaInferior or ModuleDimensionSlot.CozinhaIlha)
    {
      FrentesPortasConfiguratorService.EnsureInitialized(settings);
      FrentesPortasConfiguratorService.ApplyInferioresToStructure(settings.CozinhaFrentesPortas, structure);
    }
  }

  /// <summary>Retorna os dicionários numérico/choice da sub-árvore ativa para o slot.</summary>
  private static (Dictionary<string, float> Numeric, Dictionary<string, string> Choice)
    GetActiveDicts(BoxAssemblySectionSettings section, ModuleDimensionSlot slot) => slot switch
  {
    ModuleDimensionSlot.CozinhaSuperiorBaixo
    or ModuleDimensionSlot.CozinhaSuperiorMedio
    or ModuleDimensionSlot.CozinhaSuperiorAlto
      => (section.SuperiorNumeric, section.SuperiorChoice),
    ModuleDimensionSlot.CozinhaDespenseiro
      => (section.DespenseirosNumeric, section.DespenseirosChoice),
    _ => (section.InferiorNumeric, section.InferiorChoice)
  };

  private static float ReadNonNeg(Dictionary<string, float> numeric, string key) =>
    numeric.TryGetValue(key, out var value) ? MathF.Max(0f, value) : 0f;

  private static float ReadSigned(Dictionary<string, float> numeric, string key) =>
    numeric.TryGetValue(key, out var value) && float.IsFinite(value) ? value : 0f;

  public static void ApplyToPieces(
    ModulationRules rules,
    ModuleDefinition definition,
    DimensionConfiguratorSettings settings)
  {
    var s = rules.Structure;
    var section = GetSectionForDefinition(definition, settings);

    foreach (var lateral in rules.Pieces.Where(p => p.Role == "lateral"))
    {
      lateral.Length.OffsetMm = -s.LateralDepthGapMm;
      lateral.Width.OffsetMm = -MathF.Max(s.LateralBaseOverlapMm, s.LateralBottomRecessMm);
    }

    foreach (var basePiece in rules.Pieces.Where(p => p.Role == "base-inferior"))
    {
      basePiece.Length.OffsetMm = 2f * s.BaseAdvanceOverLateralMm;
      basePiece.Width = new ModulationDimensionBinding
      {
        Source = ModulationDimensionSource.ModuleDepth,
        OffsetMm = s.BaseFullDepth
          ? 0f
          : -s.LateralDepthGapMm - s.BaseRecessMm
      };
    }

    // Fundo inteiro/rebaixado, travessas ou ausência de fundo devem produzir a
    // mesma lista de corte exibida pela geometria.
    var originalBack = rules.Pieces.FirstOrDefault(p => p.Role == "fundo");
    rules.Pieces.RemoveAll(p => p.Role == "travessa-fundo");
    switch (s.BackPanelLayout)
    {
      case BoxBackPanelLayout.SemFundo:
        rules.Pieces.RemoveAll(p => p.Role == "fundo");
        break;
      case BoxBackPanelLayout.TravessaVertical:
        rules.Pieces.RemoveAll(p => p.Role == "fundo");
        rules.Pieces.Add(Piece("travessa-fundo-v", "travessa-fundo", "Travessa de fundo vertical",
          ModulationDimensionSource.ModuleHeight, 0f,
          ModulationDimensionSource.Constant, s.CrossRailWidthMm > 0f ? s.CrossRailWidthMm : 54f,
          s.PanelThicknessMm, quantity: 2));
        break;
      case BoxBackPanelLayout.TravessaHorizontal:
        rules.Pieces.RemoveAll(p => p.Role == "fundo");
        rules.Pieces.Add(Piece("travessa-fundo-h", "travessa-fundo", "Travessa de fundo horizontal",
          ModulationDimensionSource.InnerWidth, 0f,
          ModulationDimensionSource.Constant, s.CrossRailWidthMm > 0f ? s.CrossRailWidthMm : 54f,
          s.PanelThicknessMm, quantity: 2));
        break;
      default:
        originalBack ??= Piece("fundo", "fundo", "Fundo",
          ModulationDimensionSource.InnerWidth, 0f,
          ModulationDimensionSource.InnerHeight, 0f, s.BackThicknessMm);
        originalBack.Length = new ModulationDimensionBinding
        {
          Source = ModulationDimensionSource.InnerWidth,
          OffsetMm = s.BackAdvanceOverLateralMm * 2f - s.LateralAdvanceOverBackMm * 2f
        };
        originalBack.Width = new ModulationDimensionBinding
        {
          Source = ModulationDimensionSource.InnerHeight,
          OffsetMm = s.BackPanelLayout == BoxBackPanelLayout.Rebaixado ? -s.BackHeightRecessMm : 0f
        };
        if (!rules.Pieces.Contains(originalBack))
          rules.Pieces.Add(originalBack);
        break;
    }

    var shelfPieces = rules.Pieces.Where(p => p.Role == "prateleira").ToList();
    var shelfRule = s.Shelves.FirstOrDefault();
    foreach (var shelfPiece in shelfPieces)
    {
      float rear = shelfRule?.BackInsetMm ?? 0f;
      shelfPiece.Length.OffsetMm = -2f * (shelfRule?.WidthInsetMm ?? 0f);
      shelfPiece.Width.OffsetMm = -(shelfRule?.DepthInsetMm ?? 0f) - rear;
    }

    if (shelfPieces.Count > 0 && s.Divisions.Count > 0)
    {
      float lateralGap = shelfRule?.WidthInsetMm ?? 0f;
      var boundaries = new List<float> { 0f };
      boundaries.AddRange(s.Divisions
        .Select(division => Math.Clamp(division.WidthFraction, 0.05f, 0.95f))
        .Distinct()
        .OrderBy(value => value));
      boundaries.Add(1f);

      rules.Pieces.RemoveAll(p => p.Role == "prateleira");
      foreach (var template in shelfPieces)
      {
        for (int bay = 0; bay < boundaries.Count - 1; bay++)
        {
          bool hasDividerOnLeft = bay > 0;
          bool hasDividerOnRight = bay < boundaries.Count - 2;
          float dividerAllowance =
            (hasDividerOnLeft ? s.PanelThicknessMm * 0.5f : 0f) +
            (hasDividerOnRight ? s.PanelThicknessMm * 0.5f : 0f);

          var piece = ClonePiece(template);
          piece.Id = $"{template.Id}-vao-{bay + 1}";
          piece.Name = "Prateleira";
          piece.Quantity = 1;
          piece.Length = new ModulationDimensionBinding
          {
            Source = ModulationDimensionSource.InnerWidth,
            Scale = boundaries[bay + 1] - boundaries[bay],
            OffsetMm = -2f * lateralGap - dividerAllowance
          };
          rules.Pieces.Add(piece);
        }
      }
    }

    rules.Pieces.RemoveAll(p => p.Role is "sarrafo" or "divisoria" or "distanciador-divisoria");
    if (s.SarrafoVisible && section.BackPanelType.UsesSarrafo())
    {
      string suffix = s.SarrafoChamfered ? " chanfrado" : "";
      if (s.SarrafoWhole)
      {
        rules.Pieces.Add(Piece("sarrafo-inteiro", "sarrafo", $"Sarrafo inteiro{suffix}",
          ModulationDimensionSource.InnerWidth, 2f * s.SarrafoAdvanceOverLateralMm,
          ModulationDimensionSource.ModuleDepth, -s.SarrafoDianteiroRecessMm,
          s.SarrafoThicknessMm));
      }
      else
      {
        AddSarrafoPiece(rules, s, front: true, s.FrontSarrafoVisible,
          s.FrontSarrafoSegmented, suffix);
        AddSarrafoPiece(rules, s, front: false, s.BackSarrafoVisible,
          s.BackSarrafoSegmented, suffix);
      }
    }

    int divisionIndex = 0;
    foreach (var division in s.Divisions)
    {
      divisionIndex++;
      float rear = division.IsFixed ? s.DivisionFixedBackInsetMm : s.DivisionMovableBackInsetMm;
      rules.Pieces.Add(Piece($"divisoria-{divisionIndex}", "divisoria", $"Divisória {divisionIndex}",
        ModulationDimensionSource.ModuleDepth,
        -(s.DivisionFrontInsetMm + rear) + s.BackAdvanceOverDivisionMm,
        ModulationDimensionSource.InnerHeight, -s.DivisionBottomRecessMm,
        s.PanelThicknessMm));

      if (s.DivisionSpacerWidthMm > 0f)
      {
        rules.Pieces.Add(Piece($"dist-div-{divisionIndex}", "distanciador-divisoria",
          $"Distanciador divisória {divisionIndex}",
          ModulationDimensionSource.Constant, s.DivisionSpacerWidthMm,
          ModulationDimensionSource.InnerHeight, -s.DivisionBottomRecessMm,
          s.PanelThicknessMm));
      }
    }
  }

  private static void AddSarrafoPiece(
    ModulationRules rules,
    ModulationStructure structure,
    bool front,
    bool visible,
    bool segmented,
    string suffix)
  {
    if (!visible)
      return;

    string side = front ? "dianteiro" : "traseiro";
    float depth = front ? structure.SarrafoHeightMm : structure.SarrafoTraseiroHeightMm;
    var piece = Piece($"sarrafo-{side}", "sarrafo", $"Sarrafo {side}{suffix}",
      ModulationDimensionSource.InnerWidth, 2f * structure.SarrafoAdvanceOverLateralMm,
      ModulationDimensionSource.Constant, depth,
      structure.SarrafoThicknessMm,
      quantity: segmented ? 2 : 1);
    if (segmented)
    {
      piece.Length.Scale = 0.5f;
      piece.Length.OffsetMm = structure.SarrafoAdvanceOverLateralMm - 4f;
    }
    rules.Pieces.Add(piece);
  }

  private static ModulationPieceRule ClonePiece(ModulationPieceRule source) =>
    new()
    {
      Id = source.Id,
      Role = source.Role,
      Name = source.Name,
      Length = CloneBinding(source.Length),
      Width = CloneBinding(source.Width),
      Thickness = CloneBinding(source.Thickness),
      Quantity = source.Quantity,
      EdgeBanding = source.EdgeBanding == null
        ? null
        : new ModulationEdgeBanding
        {
          Front = source.EdgeBanding.Front,
          Back = source.EdgeBanding.Back,
          Top = source.EdgeBanding.Top,
          Bottom = source.EdgeBanding.Bottom
        },
      DrillingPattern = source.DrillingPattern
    };

  private static ModulationDimensionBinding CloneBinding(ModulationDimensionBinding source) =>
    new()
    {
      Source = source.Source,
      ConstantMm = source.ConstantMm,
      OffsetMm = source.OffsetMm,
      Scale = source.Scale
    };

  private static ModulationPieceRule Piece(
    string id,
    string role,
    string name,
    ModulationDimensionSource lengthSource,
    float lengthOffset,
    ModulationDimensionSource widthSource,
    float widthValue,
    float thickness,
    int quantity = 1) =>
    new()
    {
      Id = id,
      Role = role,
      Name = name,
      Length = new ModulationDimensionBinding
      {
        Source = lengthSource,
        OffsetMm = lengthOffset
      },
      Width = widthSource == ModulationDimensionSource.Constant
        ? new ModulationDimensionBinding
        {
          Source = ModulationDimensionSource.Constant,
          ConstantMm = widthValue
        }
        : new ModulationDimensionBinding
        {
          Source = widthSource,
          OffsetMm = widthValue
        },
      Thickness = new ModulationDimensionBinding
      {
        Source = ModulationDimensionSource.Constant,
        ConstantMm = thickness
      },
      Quantity = quantity,
      DrillingPattern = ModulationDrillingPattern.Horizontal
    };
}
