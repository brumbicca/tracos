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

    // Fixação Base - Fundo / Fundo - Lateral (mesmo local do configurador Inferior).
    structure.BackAdvanceOverBaseMm = ReadNonNeg(activeNumeric, "fbf-afb");
    structure.BaseAdvanceOverBackMm = ReadNonNeg(activeNumeric, "fbf-abf");
    structure.BaseRecessMm = ReadNonNeg(activeNumeric, "fbf-rec-base");
    structure.BackAdvanceOverLateralMm = ReadNonNeg(activeNumeric, "ffl-afl");
    structure.LateralAdvanceOverBackMm = ReadNonNeg(activeNumeric, "ffl-alf");

    // Superior/Despenseiros: chaves com sufixo -inf quando existirem.
    if (structure.BackAdvanceOverBaseMm <= 0f && activeNumeric.TryGetValue("fbf-afb-inf", out var afbInf))
      structure.BackAdvanceOverBaseMm = MathF.Max(0f, afbInf);

    // fundo-dim-trav → largura das travessas (0 = automático no BuildBackAssembly)
    if (activeNumeric.TryGetValue("fundo-dim-trav", out var dimTrav) && dimTrav > 0f)
      structure.CrossRailWidthMm = dimTrav;
    else
      structure.CrossRailWidthMm = 0f;

    // sar-tipo → visibilidade individual por sarrafo
    activeChoice.TryGetValue("sar-tipo", out var sarTipo);
    structure.SarrafoVisible     = sarTipo != "Sem sarrafo";
    structure.FrontSarrafoVisible = sarTipo is null or "Frontal" or "Ambos" or "Inteiro";
    structure.BackSarrafoVisible  = sarTipo is "Traseiro" or "Ambos" or "Inteiro";

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
    structure.SarrafoDianteiroRecessMm = activeNumeric.TryGetValue("sar-recuo-fro", out var recuoFro)
      ? MathF.Max(0f, recuoFro) : 0f;

    foreach (var shelf in structure.Shelves)
    {
      shelf.DepthInsetMm = section.ShelfDepthInsetMm;
      shelf.WidthInsetMm = section.ShelfWidthInsetMm;
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

  public static void ApplyToPieces(
    ModulationRules rules,
    ModuleDefinition definition,
    DimensionConfiguratorSettings settings)
  {
    var section = GetSectionForDefinition(definition, settings);
    if (!section.BackPanelType.UsesSarrafo() || !rules.Structure.SarrafoVisible)
      return;

    if (rules.Pieces.Any(p => p.Role == "sarrafo"))
      return;

    rules.Pieces.Add(new ModulationPieceRule
    {
      Id = "sarrafo",
      Role = "sarrafo",
      Name = "Sarrafo",
      Length = new ModulationDimensionBinding
      {
        Source = ModulationDimensionSource.InnerWidth
      },
      Width = new ModulationDimensionBinding
      {
        Source = ModulationDimensionSource.Constant,
        ConstantMm = section.SarrafoHeightMm
      },
      Thickness = new ModulationDimensionBinding
      {
        Source = ModulationDimensionSource.Constant,
        ConstantMm = section.SarrafoThicknessMm
      },
      Quantity = 1,
      DrillingPattern = ModulationDrillingPattern.Horizontal
    });
  }
}
