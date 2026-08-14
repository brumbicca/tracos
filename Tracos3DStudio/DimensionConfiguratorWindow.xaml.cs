using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tracos3DStudio;

public partial class DimensionConfiguratorWindow : Window
{
    private DimensionConfiguratorSettings _settings;
    private readonly Dictionary<string, TextBox> _fields = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ComboBox> _combos = new(StringComparer.Ordinal);
    private string _activeSection = "max";
    private bool _initialized;
    /// <summary>
    /// Padrão de engenharia do projeto ao abrir a janela — o painel volta aqui após Aplicar em módulos.
    /// </summary>
    private DimensionConfiguratorSettings _projectDefaultSnapshot;
    private bool _mutualExclusiveCheckboxUpdate;

    public DimensionConfiguratorSettings ResultSettings { get; private set; }

    public DimensionConfiguratorApplyScope ApplyScope { get; private set; } =
        DimensionConfiguratorApplyScope.NextInsertionsOnly;

    public bool HasSelectedModule { get; private set; }

    /// <summary>
    /// Atualiza o checkbox "Aplicar nos itens selecionados" quando a seleção na cena muda
    /// com o configurador já aberto (comportamento Promob).
    /// </summary>
    public void UpdateHasSelectedModule(bool hasSelectedModule)
    {
        HasSelectedModule = hasSelectedModule;
        ApplySelectedCheck.IsEnabled = hasSelectedModule;

        if (hasSelectedModule)
            ApplySelectedCheck.IsChecked = true;
        else
            ApplySelectedCheck.IsChecked = false;
    }

    /// <summary>
    /// Callback invocado pelo botão Aplicar (sem fechar) e pelo OK (antes de fechar).
    /// </summary>
    public Action<DimensionConfiguratorSettings, DimensionConfiguratorApplyScope>? OnApply { get; set; }

    /// <summary>
    /// Persiste automaticamente no projeto/perfil ao editar campos (sem reconstruir módulos existentes).
    /// </summary>
    public Action<DimensionConfiguratorSettings>? OnAutoSave { get; set; }

    public DimensionConfiguratorSettings GetCommittedSettings()
    {
        FlushActiveSection();
        return BuildResultSettings();
    }

    public DimensionConfiguratorWindow(
        DimensionConfiguratorSettings settings,
        bool hasSelectedModule)
    {
        // Atribuir antes de InitializeComponent: o TreeViewItem inicial tem IsSelected="True",
        // então SelectedItemChanged dispara durante a construção e usa _settings.
        _settings = settings.Clone();
        InitializeNestedStructures(_settings);
        ResultSettings = BuildResultSettings();
        _projectDefaultSnapshot = ResultSettings.Clone();
        HasSelectedModule = hasSelectedModule;

        InitializeComponent();

        PopulateChapaTreeItems();
        PopulateBoxAssemblyTreeItems();

        UpdateHasSelectedModule(hasSelectedModule);

        ApplySelectedCheck.Checked += ApplySelectedCheck_Checked;
        ApplySelectedCheck.Unchecked += ApplySelectedCheck_Unchecked;
        ApplyAllCheck.Checked += ApplyAllCheck_Checked;
        ApplyAllCheck.Unchecked += ApplyAllCheck_Unchecked;

        _initialized = true;
        ShowSection("max");
    }

    private static void InitializeNestedStructures(DimensionConfiguratorSettings settings)
    {
        ChapaConfiguratorService.EnsureChapasInitialized(settings);
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        EletrosConfiguratorService.EnsureInitialized(settings);
        FrentesPortasConfiguratorService.EnsureInitialized(settings);
        FrentesPortasConfiguratorService.EnsureDormitorioInitialized(settings);
        GavetasConfiguratorService.EnsureInitialized(settings);
        GavetasConfiguratorService.EnsureDormitorioInitialized(settings);
        GavetasInternasConfiguratorService.EnsureInitialized(settings);
        CavaConfiguratorService.EnsureInitialized(settings);
    }

    /// <summary>
    /// Após aplicar em módulos da cena, o painel exibe de novo o padrão salvo do projeto (Promob).
    /// </summary>
    private void RestorePanelFromProjectDefault()
    {
        _settings = _projectDefaultSnapshot.Clone();
        InitializeNestedStructures(_settings);
        ResultSettings = BuildResultSettings();
        ShowSection(_activeSection);
    }

    private void PopulateBoxAssemblyTreeItems()
    {
        PopulateInferiorBranch(CozinhaBoxInferiorTreeItem, "coz-box-inf");
        PopulateSuperiorBranch(CozinhaBoxSuperiorTreeItem, "coz-box-sup");
        PopulateDespenseirosBranch(CozinhaBoxDespenseirosTreeItem, "coz-box-desp");
        PopulateEletrosBranch(CozinhaEletrosTreeItem, "coz-eletros");
        PopulateFrentesPortasBranch(CozinhaFrentesPortasTreeItem, "coz-portas");
        PopulateGavetasBranch(CozinhaGavetasTreeItem, "coz-gavetas");
        PopulateGavetasInternasBranch(CozinhaGavetasInternasTreeItem, "coz-gavint");
        PopulateCavaBranch(CozinhaCavaTreeItem, "coz-cava");
        PopulateArmarioBranch(DormitorioBoxArmarioTreeItem, "dor-box-arm");
        PopulateInferiorBranch(DormitorioBoxBancadaCriadoTreeItem, "dor-box-banc");
        PopulateSuperiorBranch(DormitorioBoxSuperiorTreeItem, "dor-box-sup-dor");
        PopulateFrentesPortasBranch(DormitorioFrentesPortasTreeItem, "dor-portas");
        PopulateGavetasBranch(DormitorioGavetasTreeItem, "dor-gavetas");
    }

    private static void PopulateSuperiorBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in BoxAssemblySuperiorSchema.DirectNodes)
            parent.Items.Add(new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" });

        foreach (var group in BoxAssemblySuperiorSchema.Groups)
        {
            var groupItem = new TreeViewItem { Header = group.Header, Tag = $"{tagPrefix}-group" };
            foreach (var node in group.Nodes)
                groupItem.Items.Add(new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" });

            parent.Items.Add(groupItem);
        }
    }

    private static void PopulateDespenseirosBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in BoxAssemblyDespenseirosSchema.DirectNodes)
            parent.Items.Add(new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" });
    }

    private static void PopulateEletrosBranch(TreeViewItem parent, string tagPrefix)
    {
        var leaf = new TreeViewItem
        {
            Header = CozinhaEletrosSchema.Leaf.Header,
            Tag = $"{tagPrefix}-{CozinhaEletrosSchema.LeafId}"
        };
        AutomationProperties.SetAutomationId(leaf, "CozinhaEletrosLeafTreeItem");
        parent.Items.Add(leaf);
    }

    private static void PopulateFrentesPortasBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in CozinhaFrentesPortasSchema.DirectNodes)
        {
            var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
            AutomationProperties.SetAutomationId(leaf, $"CozinhaPortas{ToAutomationSuffix(node.Id)}TreeItem");
            parent.Items.Add(leaf);
        }

        foreach (var group in CozinhaFrentesPortasSchema.Groups)
        {
            var groupItem = new TreeViewItem { Header = group.Header, Tag = $"{tagPrefix}-group" };
            foreach (var node in group.Nodes)
            {
                var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
                AutomationProperties.SetAutomationId(leaf, $"CozinhaPortas{ToAutomationSuffix(node.Id)}TreeItem");
                groupItem.Items.Add(leaf);
            }

            parent.Items.Add(groupItem);
        }
    }

    private static void PopulateGavetasBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in CozinhaGavetasSchema.DirectNodes)
        {
            var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
            AutomationProperties.SetAutomationId(leaf, $"CozinhaGavetas{ToAutomationSuffix(node.Id)}TreeItem");
            parent.Items.Add(leaf);
        }
    }

    private static void PopulateGavetasInternasBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in CozinhaGavetasInternasSchema.DirectNodes)
        {
            var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
            AutomationProperties.SetAutomationId(leaf, $"CozinhaGavetasInternas{ToAutomationSuffix(node.Id)}TreeItem");
            parent.Items.Add(leaf);
        }
    }

    private static void PopulateCavaBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in CozinhaCavaSchema.DirectNodes)
        {
            var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
            AutomationProperties.SetAutomationId(leaf, $"CozinhaCava{ToAutomationSuffix(node.Id)}TreeItem");
            parent.Items.Add(leaf);
        }

        foreach (var group in CozinhaCavaSchema.Groups)
        {
            var groupItem = new TreeViewItem { Header = group.Header, Tag = $"{tagPrefix}-group" };
            foreach (var node in group.Nodes)
            {
                var leaf = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
                AutomationProperties.SetAutomationId(leaf, $"CozinhaCava{ToAutomationSuffix(node.Id)}TreeItem");
                groupItem.Items.Add(leaf);
            }

            parent.Items.Add(groupItem);
        }
    }

    private static string ToAutomationSuffix(string nodeId) =>
        string.Concat(nodeId.Split('-').Select(static part =>
            part.Length > 0
                ? char.ToUpperInvariant(part[0]) + part[1..]
                : part));

    private static void PopulateArmarioBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in BoxAssemblyArmarioSchema.DirectNodes)
            parent.Items.Add(CreateArmarioLeaf(tagPrefix, node));

        foreach (var group in BoxAssemblyArmarioSchema.Groups)
        {
            var groupItem = new TreeViewItem { Header = group.Header, Tag = $"{tagPrefix}-group" };
            foreach (var node in group.Nodes)
                groupItem.Items.Add(CreateArmarioLeaf(tagPrefix, node));

            parent.Items.Add(groupItem);
        }
    }

    private static TreeViewItem CreateArmarioLeaf(string tagPrefix, BoxNodeDef node)
    {
        var item = new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" };
        AutomationProperties.SetAutomationId(item, ToArmarioLeafAutomationId(node.Id));
        AutomationProperties.SetName(item, node.Header);
        return item;
    }

    private static string ToArmarioLeafAutomationId(string nodeId)
    {
        var parts = nodeId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var suffix = string.Concat(parts.Select(p => char.ToUpper(p[0]) + p[1..]));
        return $"DormitorioArmario{suffix}TreeItem";
    }

    private static void PopulateInferiorBranch(TreeViewItem parent, string tagPrefix)
    {
        foreach (var node in BoxAssemblyInferiorSchema.DirectNodes)
            parent.Items.Add(new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" });

        foreach (var group in BoxAssemblyInferiorSchema.Groups)
        {
            var groupItem = new TreeViewItem { Header = group.Header, Tag = $"{tagPrefix}-group" };
            foreach (var node in group.Nodes)
                groupItem.Items.Add(new TreeViewItem { Header = node.Header, Tag = $"{tagPrefix}-{node.Id}" });

            parent.Items.Add(groupItem);
        }
    }

    private static void PopulateBoxBranch(TreeViewItem parent, string tagPrefix, string[] kinds)
    {
        foreach (var kind in kinds)
        {
            parent.Items.Add(new TreeViewItem
            {
                Header = BoxAssemblyNodeKinds.DisplayName(kind),
                Tag = $"{tagPrefix}-{kind}"
            });
        }
    }

    private void PopulateChapaTreeItems()
    {
        PopulateChapaBranch(CozinhaChapasTreeItem, "coz-chapa", ChapaPieceKinds.CozinhaPieces);
        PopulateChapaBranch(DormitorioChapasTreeItem, "dor-chapa", ChapaPieceKinds.DormitorioPieces,
            dormitorioLabels: true, includeSubgroups: false);
        PopulateChapaBranch(DormitorioChapasComponentesTreeItem, "dor-componentes",
            ChapaPieceKinds.DormitorioComponentes, dormitorioLabels: true, includeSubgroups: false);
        PopulateChapaBranch(DormitorioChapasGavetasSapateirasTreeItem, "dor-gav-sap",
            ChapaPieceKinds.DormitorioGavetasSapateiras, dormitorioLabels: true, includeSubgroups: false);
    }

    private static void PopulateChapaBranch(
        TreeViewItem parent,
        string tagPrefix,
        string[] kinds,
        bool dormitorioLabels = false,
        bool includeSubgroups = true)
    {
        foreach (var kind in kinds)
            parent.Items.Add(CreateChapaLeaf(tagPrefix, kind, dormitorioLabels));

        if (!includeSubgroups)
            return;

        parent.Items.Add(CreateChapaSubGroup(tagPrefix, "Componentes", ChapaPieceKinds.Componentes));
        parent.Items.Add(CreateChapaSubGroup(tagPrefix, "Gavetas", ChapaPieceKinds.Gavetas));
    }

    private static TreeViewItem CreateChapaSubGroup(string tagPrefix, string header, string[] kinds)
    {
        var group = new TreeViewItem { Header = header };
        foreach (var kind in kinds)
            group.Items.Add(CreateChapaLeaf(tagPrefix, kind));

        return group;
    }

    private static TreeViewItem CreateChapaLeaf(string tagPrefix, string kind, bool dormitorioLabels = false) => new()
    {
        Header = ChapaPieceKinds.DisplayName(kind, dormitorioLabels),
        Tag = $"{tagPrefix}-{kind}"
    };

    private void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Ignora disparos durante InitializeComponent (FieldsPanel ainda não existe).
        if (!_initialized || CategoryTree.SelectedItem is not TreeViewItem item)
            return;

        if (item.Tag is string tag)
        {
            FlushActiveSection();
            ShowSection(tag);
            return;
        }

        if (item.Parent is TreeViewItem parent && parent.Tag is string parentTag)
        {
            FlushActiveSection();
            ShowSection(parentTag);
        }
    }

    private void ShowSection(string sectionId)
    {
        _activeSection = sectionId;
        FieldsPanel.Children.Clear();
        _fields.Clear();
        _combos.Clear();

        if (sectionId.StartsWith("coz-chapa-", StringComparison.Ordinal))
        {
            var kind = sectionId["coz-chapa-".Length..];
            if (kind == "group")
            {
                AddSectionTitle("Cozinhas — Chapas", "Selecione um tipo de peça para editar sua espessura.");
                return;
            }
            ShowChapaSection("Cozinhas", _settings.CozinhaChapas, kind);
            return;
        }

        if (sectionId.StartsWith("dor-chapa-", StringComparison.Ordinal))
        {
            var kind = sectionId["dor-chapa-".Length..];
            if (kind == "group")
            {
                AddSectionTitle("Dormitórios — Chapas", "Selecione um tipo de peça para editar sua espessura.");
                return;
            }
            ShowChapaSection("Dormitórios", _settings.DormitorioChapas, kind, "Chapas", dormitorioLabels: true);
            return;
        }

        if (sectionId.StartsWith("dor-componentes-", StringComparison.Ordinal))
        {
            var kind = sectionId["dor-componentes-".Length..];
            if (kind == "group")
            {
                AddSectionTitle("Dormitórios — Componentes",
                    "Selecione um componente para editar limites de chapa e espessura.");
                return;
            }
            ShowChapaSection("Dormitórios", _settings.DormitorioChapas, kind, "Componentes", dormitorioLabels: true);
            return;
        }

        if (sectionId.StartsWith("dor-gav-sap-", StringComparison.Ordinal))
        {
            var kind = sectionId["dor-gav-sap-".Length..];
            if (kind == "group")
            {
                AddSectionTitle("Dormitórios — Gavetas | Sapateiras",
                    "Selecione um tipo de peça para editar limites de chapa e espessura.");
                return;
            }
            ShowChapaSection("Dormitórios", _settings.DormitorioChapas, kind, "Gavetas | Sapateiras", dormitorioLabels: true);
            return;
        }

        if (sectionId.StartsWith("coz-box-inf-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-box-inf-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Montagem da Caixa - Inferior",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }

            ShowInferiorSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-box-sup-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-box-sup-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Montagem da Caixa - Superior",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }

            ShowSuperiorSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-box-desp-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-box-desp-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Montagem da Caixa - Despenseiros | Torres",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowDespenseirosSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-eletros-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-eletros-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Eletros",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowEletrosSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-portas-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-portas-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Frentes | Portas — Folgas Painel",
                    "Selecione Portas Alumínio ou Portas Vidro para editar as folgas.");
                return;
            }

            ShowFrentesPortasSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-gavetas-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-gavetas-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Gavetas",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowGavetasSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-gavint-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-gavint-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Gavetas Internas | Auxiliares",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowGavetasInternasSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("coz-cava-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["coz-cava-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Cozinhas — Cozinhas Cava — Frentes | Portas",
                    "Selecione Inferiores, Superiores, Despenseiros ou Torres para editar as folgas.");
                return;
            }

            ShowCavaSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("dor-box-arm-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["dor-box-arm-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Dormitórios — Montagem da Caixa - Armários",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowDorArmarioSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("dor-box-banc-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["dor-box-banc-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Dormitórios — Montagem de Caixa - Bancadas | Criados",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowDorBancadaCriadoSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("dor-box-sup-dor-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["dor-box-sup-dor-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Dormitórios — Montagem de Caixa - Superior",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowDorSuperiorSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("dor-portas-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["dor-portas-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Dormitórios — Frentes | Portas",
                    "Selecione uma subseção para editar as folgas.");
                return;
            }
            ShowDorFrentesPortasSection(nodeId);
            return;
        }

        if (sectionId.StartsWith("dor-gavetas-", StringComparison.Ordinal))
        {
            string nodeId = sectionId["dor-gavetas-".Length..];
            if (nodeId == "group")
            {
                AddSectionTitle("Dormitórios — Gavetas",
                    "Selecione uma folha do subgrupo para editar seus campos.");
                return;
            }
            ShowDorGavetasSection(nodeId);
            return;
        }

        switch (sectionId)
        {
            case "max":
                AddSectionTitle("Medidas Máximas", "Limites globais de largura, altura e profundidade dos módulos.");
                AddField("max-width", "A — Largura máxima (mm)", _settings.MaxWidthMm);
                AddField("max-height", "B — Altura máxima (mm)", _settings.MaxHeightMm);
                AddField("max-depth", "C — Profundidade máxima (mm)", _settings.MaxDepthMm);
                break;

            case "coz-ext":
                AddSectionTitle("Cozinhas — Dimensões Externas",
                    "Medidas padrão na inserção (paridade Promob A–O).");
                AddField("coz-inf-h", "A — Inferiores — Altura (mm)", _settings.CozinhaInferiorHeightMm);
                AddField("coz-inf-d", "B — Inferiores — Profundidade (mm)", _settings.CozinhaInferiorDepthMm);
                AddField("coz-sup-baixo-h", "C — Superiores Baixos — Altura (mm)", _settings.CozinhaSuperiorBaixoHeightMm);
                AddField("coz-sup-h", "D — Superiores Médios — Altura (mm)", _settings.CozinhaSuperiorHeightMm);
                AddField("coz-sup-alto-h", "E — Superiores Altos — Altura (mm)", _settings.CozinhaSuperiorAltoHeightMm);
                AddField("coz-sup-d", "F — Superiores — Profundidade (mm)", _settings.CozinhaSuperiorDepthMm);
                AddField("coz-ilha-d", "G — Ilhas — Profundidade (mm)", _settings.CozinhaIlhaDepthMm);
                AddField("coz-desp-h", "H — Despenseiros — Altura (mm)", _settings.CozinhaDespenseiroHeightMm);
                AddField("coz-desp-d", "I — Despenseiros — Profundidade (mm)", _settings.CozinhaDespenseiroDepthMm);
                AddField("coz-vis-tamp-h", "J — Vista p/ Tampo — Altura (mm)", _settings.CozinhaVistaTampoHeightMm);
                AddField("coz-tamp-av", "K — Tampo — Avanço (mm)", _settings.CozinhaTampoAvancoMm);
                AddField("coz-rod-rec", "L — Rodapés — Recuo (mm)", _settings.CozinhaRodapeRecuoMm);
                AddField("coz-mol-eng", "M — Moldura Engrossuramento — Profundidade (mm)", _settings.CozinhaMolduraEngrossProfMm);
                AddField("coz-vis-inf-h", "N — Vista Inferior — Altura (mm)", _settings.CozinhaVistaInferiorHeightMm);
                AddField("coz-vis-inf-rec", "O — Vista Inferior — Recuo (mm)", _settings.CozinhaVistaInferiorRecuoMm);
                break;

            case "dor-ext":
                AddSectionTitle("Dormitórios — Dimensões Externas",
                    "Medidas padrão na inserção (paridade Promob A–J). Cômoda usa Bancadas (C/D).");
                AddField("dor-arm-h", "A — Armários — Altura (mm)", _settings.DormitorioArmarioHeightMm);
                AddField("dor-arm-d", "B — Armários — Profundidade (mm)", _settings.DormitorioArmarioDepthMm);
                AddField("dor-banc-h", "C — Bancadas — Altura (mm)", _settings.DormitorioBancadaHeightMm);
                AddField("dor-banc-d", "D — Bancada — Profundidade (mm)", _settings.DormitorioBancadaDepthMm);
                AddField("dor-cri-h", "E — Criados — Altura (mm)", _settings.DormitorioCriadoHeightMm);
                AddField("dor-cri-d", "F — Criados — Profundidade (mm)", _settings.DormitorioCriadoDepthMm);
                AddField("dor-sup-h", "G — Superiores — Altura (mm)", _settings.DormitorioSuperiorHeightMm);
                AddField("dor-sup-d", "H — Superiores — Profundidade (mm)", _settings.DormitorioSuperiorDepthMm);
                AddField("dor-tamp-av", "I — Tampo — Avanço (mm)", _settings.DormitorioTampoAvancoMm);
                AddField("dor-mol-eng", "J — Moldura Engrossuramento — Profundidade (mm)", _settings.DormitorioMolduraEngrossProfMm);
                break;

            case "pan-ext":
                AddSectionTitle("Painéis — Dimensões Externas",
                    "Medidas padrão na inserção de painéis decorativos (Traços).");
                AddField("pan-w", "Largura padrão (mm)", _settings.PainelWidthMm);
                AddField("pan-h", "Altura padrão (mm)", _settings.PainelHeightMm);
                AddField("pan-t", "Espessura (mm)", _settings.PainelThicknessMm);
                break;

            case "pan-chapas":
                AddSectionTitle("Painéis — Chapas",
                    "Espessura padrão de painéis decorativos.");
                AddField("pan-chap-t", "D — Espessura da chapa (mm)", _settings.PainelThicknessMm);
                break;
        }
    }

    private void ShowBoxSection(string categoryLabel, BoxAssemblySectionSettings section, string kind)
    {
        string display = BoxAssemblyNodeKinds.DisplayName(kind);
        AddSectionTitle($"{categoryLabel} — {display}",
            "Paridade Promob — montagem da caixa (tipo de fundo e fixações).");
        string prefix = $"box-{kind}";

        switch (kind)
        {
            case BoxAssemblyNodeKinds.Fundo:
                AddBackTypeCombo($"{prefix}-type", section.BackPanelType);
                AddField($"{prefix}-recess", "B — Recuo/ranhura do fundo encaixado (mm)", section.BackRecessMm);
                AddField($"{prefix}-sarrafo-h", "C — Altura do sarrafo (mm)", section.SarrafoHeightMm);
                AddField($"{prefix}-sarrafo-t", "D — Espessura do sarrafo (mm)", section.SarrafoThicknessMm);
                break;

            case BoxAssemblyNodeKinds.FixacaoLateralBase:
                AddField($"{prefix}-overlap", "A — Superposição lateral sobre base (mm)", section.LateralBaseOverlapMm);
                break;

            case BoxAssemblyNodeKinds.Sarrafo:
                AddField($"{prefix}-sarrafo-h", "A — Altura do sarrafo (mm)", section.SarrafoHeightMm);
                AddField($"{prefix}-sarrafo-t", "B — Espessura do sarrafo (mm)", section.SarrafoThicknessMm);
                break;

            case BoxAssemblyNodeKinds.Prateleira:
                AddField($"{prefix}-shelf-d", "A — Recuo frontal prateleiras (mm)", section.ShelfDepthInsetMm);
                AddField($"{prefix}-shelf-w", "D — Folga lateral prateleiras (mm)", section.ShelfWidthInsetMm);
                break;
        }
    }

    private void ShowInferiorSection(string nodeId)
    {
        var node = BoxAssemblyInferiorSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Montagem da Caixa - Inferior — {node.Header}",
            "Paridade Promob — montagem da caixa (tipo de fundo, fixações e cantos).");

        var box = _settings.CozinhaInferiorBox;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.InferiorNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"boxinf-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.InferiorChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"boxinf-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowSuperiorSection(string nodeId)
    {
        var node = BoxAssemblySuperiorSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Montagem da Caixa - Superior — {node.Header}",
            "Paridade Promob — montagem da caixa superior (fixações e cantos).");

        var box = _settings.CozinhaSuperiorBox;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.SuperiorNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"boxsup-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.SuperiorChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"boxsup-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDespenseirosSection(string nodeId)
    {
        var node = BoxAssemblyDespenseirosSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Montagem da Caixa - Despenseiros | Torres — {node.Header}",
            "Paridade Promob — montagem da caixa despenseiros e torres.");

        var box = _settings.CozinhaDespenseiroBox;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.DespenseirosNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"boxdesp-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.DespenseirosChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"boxdesp-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDorArmarioSection(string nodeId)
    {
        var node = BoxAssemblyArmarioSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Dormitórios — Montagem da Caixa - Armários — {node.Header}",
            "Paridade Promob — montagem da caixa de guarda-roupa.");

        var box = _settings.DormitorioArmarioBox;
        string? currentGroup = null;
        foreach (var field in node.Fields)
        {
            if (!string.IsNullOrEmpty(field.Group) && field.Group != currentGroup)
            {
                currentGroup = field.Group;
                AddFieldGroupHeader(currentGroup);
            }

            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.ArmarioNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"dorarm-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.ArmarioChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"dorarm-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDorBancadaCriadoSection(string nodeId)
    {
        var node = BoxAssemblyInferiorSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Dormitórios — Montagem de Caixa - Bancadas | Criados — {node.Header}",
            "Paridade Promob — montagem da caixa (tipo de fundo, fixações e cantos).");

        var box = _settings.DormitorioBancadaCriadoBox;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.InferiorNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"dorbanc-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.InferiorChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"dorbanc-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDorSuperiorSection(string nodeId)
    {
        var node = BoxAssemblySuperiorSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Dormitórios — Montagem de Caixa - Superior — {node.Header}",
            "Paridade Promob — montagem da caixa superior (fixações e cantos).");

        var box = _settings.DormitorioSuperiorBox;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = box.SuperiorNumeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"dorsup-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = box.SuperiorChoice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"dorsup-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDorFrentesPortasSection(string nodeId)
    {
        var node = CozinhaFrentesPortasSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Dormitórios — Frentes | Portas — {node.Header}",
            "Paridade Promob — folgas de frentes e portas.");

        var portas = _settings.DormitorioFrentesPortas;
        foreach (var field in node.Fields)
        {
            string storageKey = FrentesPortasConfiguratorService.MakeKey(nodeId, field.Key);
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = portas.Numeric.TryGetValue(storageKey, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"dorport-num-{nodeId}::{field.Key}", field.Label, value);
            }
            else
            {
                string selected = portas.Choice.TryGetValue(storageKey, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"dorport-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowDorGavetasSection(string nodeId)
    {
        var node = CozinhaGavetasSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Dormitórios — Gavetas — {node.Header}",
            "Paridade Promob — folgas, fixações laterais e fundos da caixa de gaveta.");

        var gavetas = _settings.DormitorioGavetas;
        foreach (var field in node.Fields)
        {
            string storageKey = GavetasConfiguratorService.MakeKey(nodeId, field.Key);
            string selected = gavetas.Choice.TryGetValue(storageKey, out var s)
                ? s
                : field.DefaultOption;
            AddChoiceCombo($"dorgav-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
        }
    }

    private void ShowGavetasSection(string nodeId)
    {
        var node = CozinhaGavetasSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Gavetas — {node.Header}",
            "Paridade Promob — folgas, fixações laterais e fundos da caixa de gaveta.");

        var gavetas = _settings.CozinhaGavetas;
        foreach (var field in node.Fields)
        {
            string storageKey = GavetasConfiguratorService.MakeKey(nodeId, field.Key);
            string selected = gavetas.Choice.TryGetValue(storageKey, out var s)
                ? s
                : field.DefaultOption;
            AddChoiceCombo($"gav-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
        }
    }

    private void ShowGavetasInternasSection(string nodeId)
    {
        var node = CozinhaGavetasInternasSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Gavetas Internas | Auxiliares — {node.Header}",
            "Paridade Promob — folgas, fixações laterais e fundos de gavetas internas e auxiliares.");

        var gavetasInternas = _settings.CozinhaGavetasInternas;
        foreach (var field in node.Fields)
        {
            string storageKey = GavetasInternasConfiguratorService.MakeKey(nodeId, field.Key);
            string selected = gavetasInternas.Choice.TryGetValue(storageKey, out var s)
                ? s
                : field.DefaultOption;
            AddChoiceCombo($"gavint-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
        }
    }

    private void ShowCavaSection(string nodeId)
    {
        var node = CozinhaCavaSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Cozinhas Cava — {node.Header}",
            "Paridade Promob — puxadores cava, laterais, cantos e folgas de frentes/portas.");

        var cava = _settings.CozinhaCava;
        foreach (var field in node.Fields)
        {
            string storageKey = CavaConfiguratorService.MakeKey(nodeId, field.Key);
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = cava.Numeric.TryGetValue(storageKey, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"cava-num-{nodeId}::{field.Key}", field.Label, value);
            }
            else
            {
                string selected = cava.Choice.TryGetValue(storageKey, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"cava-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowFrentesPortasSection(string nodeId)
    {
        var node = CozinhaFrentesPortasSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Frentes | Portas — {node.Header}",
            "Paridade Promob — folgas entre portas/frentes e bordas do módulo.");

        var portas = _settings.CozinhaFrentesPortas;
        foreach (var field in node.Fields)
        {
            string storageKey = FrentesPortasConfiguratorService.MakeKey(nodeId, field.Key);
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = portas.Numeric.TryGetValue(storageKey, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"portas-num-{nodeId}::{field.Key}", field.Label, value);
            }
            else
            {
                string selected = portas.Choice.TryGetValue(storageKey, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"portas-cho-{nodeId}::{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void ShowEletrosSection(string nodeId)
    {
        var node = CozinhaEletrosSchema.FindNode(nodeId);
        if (node is null)
            return;

        AddSectionTitle($"Cozinhas — Eletros — {node.Header}",
            "Paridade Promob — vãos e apoios para fogão, forno, microondas e lava louças.");

        var eletros = _settings.CozinhaEletros;
        foreach (var field in node.Fields)
        {
            if (field.Kind == BoxFieldKind.Numeric)
            {
                float value = eletros.Numeric.TryGetValue(field.Key, out var v)
                    ? v
                    : field.DefaultValue;
                AddField($"ele-num-{field.Key}", field.Label, value);
            }
            else
            {
                string selected = eletros.Choice.TryGetValue(field.Key, out var s)
                    ? s
                    : field.DefaultOption;
                AddChoiceCombo($"ele-cho-{field.Key}", field.Label, field.Options, selected);
            }
        }
    }

    private void AddChoiceCombo(string key, string label, string[] options, string selected)
    {
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
        int selectedIndex = 0;
        for (int i = 0; i < options.Length; i++)
        {
            combo.Items.Add(new ComboBoxItem { Content = options[i], Tag = options[i] });
            if (string.Equals(options[i], selected, StringComparison.Ordinal))
                selectedIndex = i;
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = selectedIndex;

        combo.SelectionChanged += (_, _) => FlushActiveSection();
        AutomationProperties.SetName(combo, label);
        _combos[key] = combo;
        FieldsPanel.Children.Add(combo);
    }

    private void ShowChapaSection(
        string categoryLabel,
        CategoryChapaSettings chapas,
        string kind,
        string sectionLabel = "Chapas",
        bool dormitorioLabels = false)
    {
        var piece = chapas.GetOrCreate(kind);
        string display = ChapaPieceKinds.DisplayName(kind, dormitorioLabels);
        AddSectionTitle($"{categoryLabel} — {sectionLabel} — {display}",
            "Paridade Promob — limites de chapa e espessura por tipo de peça.");
        string prefix = $"chapa-{kind}";
        AddField($"{prefix}-max-w", "B — Largura máxima da chapa (mm)", piece.MaxWidthMm);
        AddField($"{prefix}-max-l", "C — Comprimento máximo da chapa (mm)", piece.MaxLengthMm);
        AddField($"{prefix}-thick", "D — Espessura da chapa (mm)", piece.ThicknessMm);
    }

    private void AddSectionTitle(string title, string description)
    {
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        });
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });
    }

    private void AddFieldGroupHeader(string title)
    {
        FieldsPanel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x7E, 0xB8)),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 10, 0, 6),
            Child = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            }
        });
    }

    private void AddField(string key, string label, float value)
    {
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var box = new TextBox
        {
            Text = value.ToString("0", CultureInfo.InvariantCulture),
            Margin = new Thickness(0, 0, 0, 10)
        };
        box.LostFocus += (_, _) => FlushActiveSection();
        AutomationProperties.SetName(box, label);
        _fields[key] = box;
        FieldsPanel.Children.Add(box);
    }

    private void AddBackTypeCombo(string key, BoxBackPanelType selected)
    {
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = "A — Tipo de fundo",
            Margin = new Thickness(0, 0, 0, 2)
        });

        var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
        foreach (BoxBackPanelType type in Enum.GetValues<BoxBackPanelType>())
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content = type.DisplayName(),
                Tag = type
            });
        }

        combo.SelectedIndex = (int)selected;
        combo.SelectionChanged += (_, _) => FlushActiveSection();
        AutomationProperties.SetName(combo, "Tipo de fundo");
        _combos[key] = combo;
        FieldsPanel.Children.Add(combo);
    }

    private void FlushActiveSection()
    {
        bool touchedInferior = false;
        bool touchedSuperior = false;
        bool touchedDespenseiros = false;
        bool touchedPortas = false;
        bool touchedDorArm = false;
        bool touchedDorBanc = false;
        bool touchedDorSup = false;

        foreach (var (key, combo) in _combos)
        {
            if (key.StartsWith("boxinf-cho-", StringComparison.Ordinal))
            {
                FlushInferiorChoice(key["boxinf-cho-".Length..], combo);
                touchedInferior = true;
                continue;
            }

            if (key.StartsWith("boxsup-cho-", StringComparison.Ordinal))
            {
                FlushSuperiorChoice(key["boxsup-cho-".Length..], combo);
                touchedSuperior = true;
                continue;
            }

            if (key.StartsWith("boxdesp-cho-", StringComparison.Ordinal))
            {
                FlushDespenseirosChoice(key["boxdesp-cho-".Length..], combo);
                touchedDespenseiros = true;
                continue;
            }

            if (key.StartsWith("dorarm-cho-", StringComparison.Ordinal))
            {
                if (combo.SelectedItem is ComboBoxItem { Tag: string opt })
                    _settings.DormitorioArmarioBox.ArmarioChoice[key["dorarm-cho-".Length..]] = opt;
                touchedDorArm = true;
                continue;
            }

            if (key.StartsWith("dorbanc-cho-", StringComparison.Ordinal))
            {
                if (combo.SelectedItem is ComboBoxItem { Tag: string opt })
                    _settings.DormitorioBancadaCriadoBox.InferiorChoice[key["dorbanc-cho-".Length..]] = opt;
                touchedDorBanc = true;
                continue;
            }

            if (key.StartsWith("dorsup-cho-", StringComparison.Ordinal))
            {
                if (combo.SelectedItem is ComboBoxItem { Tag: string opt })
                    _settings.DormitorioSuperiorBox.SuperiorChoice[key["dorsup-cho-".Length..]] = opt;
                touchedDorSup = true;
                continue;
            }

            if (key.StartsWith("dorport-cho-", StringComparison.Ordinal))
            {
                if (TryParsePortasFieldKey(key["dorport-cho-".Length..], out var nId, out var fKey)
                    && combo.SelectedItem is ComboBoxItem { Tag: string opt })
                    _settings.DormitorioFrentesPortas.Choice[
                        FrentesPortasConfiguratorService.MakeKey(nId, fKey)] = opt;
                continue;
            }

            if (key.StartsWith("dorgav-cho-", StringComparison.Ordinal))
            {
                if (TryParsePortasFieldKey(key["dorgav-cho-".Length..], out var nId, out var fKey)
                    && combo.SelectedItem is ComboBoxItem { Tag: string opt })
                    _settings.DormitorioGavetas.Choice[
                        GavetasConfiguratorService.MakeKey(nId, fKey)] = opt;
                continue;
            }

            if (key.StartsWith("ele-cho-", StringComparison.Ordinal))
            {
                FlushEletrosChoice(key["ele-cho-".Length..], combo);
                continue;
            }

            if (key.StartsWith("portas-cho-", StringComparison.Ordinal))
            {
                FlushPortasChoice(key["portas-cho-".Length..], combo);
                touchedPortas = true;
                continue;
            }

            if (key.StartsWith("gav-cho-", StringComparison.Ordinal))
            {
                FlushGavetasChoice(key["gav-cho-".Length..], combo);
                continue;
            }

            if (key.StartsWith("gavint-cho-", StringComparison.Ordinal))
            {
                FlushGavetasInternasChoice(key["gavint-cho-".Length..], combo);
                continue;
            }

            if (key.StartsWith("cava-cho-", StringComparison.Ordinal))
            {
                FlushCavaChoice(key["cava-cho-".Length..], combo);
                continue;
            }

            FlushBoxCombo(key, combo);
        }

        foreach (var (key, box) in _fields)
        {
            if (key.StartsWith("boxinf-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.CozinhaInferiorBox.InferiorNumeric[key["boxinf-num-".Length..]] = v;
                    touchedInferior = true;
                }
                continue;
            }

            if (key.StartsWith("boxsup-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.CozinhaSuperiorBox.SuperiorNumeric[key["boxsup-num-".Length..]] = v;
                    touchedSuperior = true;
                }
                continue;
            }

            if (key.StartsWith("boxdesp-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.CozinhaDespenseiroBox.DespenseirosNumeric[key["boxdesp-num-".Length..]] = v;
                    touchedDespenseiros = true;
                }
                continue;
            }

            if (key.StartsWith("dorarm-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.DormitorioArmarioBox.ArmarioNumeric[key["dorarm-num-".Length..]] = v;
                    touchedDorArm = true;
                }
                continue;
            }

            if (key.StartsWith("dorbanc-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.DormitorioBancadaCriadoBox.InferiorNumeric[key["dorbanc-num-".Length..]] = v;
                    touchedDorBanc = true;
                }
                continue;
            }

            if (key.StartsWith("dorsup-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.DormitorioSuperiorBox.SuperiorNumeric[key["dorsup-num-".Length..]] = v;
                    touchedDorSup = true;
                }
                continue;
            }

            if (key.StartsWith("dorport-num-", StringComparison.Ordinal))
            {
                if (TryParsePortasFieldKey(key["dorport-num-".Length..], out var nodeId, out var fieldKey)
                    && TryParseMm(box.Text, out float v) && v >= 0f)
                    _settings.DormitorioFrentesPortas.Numeric[
                        FrentesPortasConfiguratorService.MakeKey(nodeId, fieldKey)] = v;
                continue;
            }

            if (key.StartsWith("ele-num-", StringComparison.Ordinal))
            {
                if (TryParseMm(box.Text, out float v) && v >= 0f)
                    _settings.CozinhaEletros.Numeric[key["ele-num-".Length..]] = v;
                continue;
            }

            if (key.StartsWith("portas-num-", StringComparison.Ordinal))
            {
                if (TryParsePortasFieldKey(key["portas-num-".Length..], out var nodeId, out var fieldKey)
                    && TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.CozinhaFrentesPortas.Numeric[
                        FrentesPortasConfiguratorService.MakeKey(nodeId, fieldKey)] = v;
                    touchedPortas = true;
                }
                continue;
            }

            if (key.StartsWith("cava-num-", StringComparison.Ordinal))
            {
                if (TryParsePortasFieldKey(key["cava-num-".Length..], out var nodeId, out var fieldKey)
                    && TryParseMm(box.Text, out float v) && v >= 0f)
                {
                    _settings.CozinhaCava.Numeric[
                        CavaConfiguratorService.MakeKey(nodeId, fieldKey)] = v;
                }
                continue;
            }

            if (!TryParseMm(box.Text, out float value) || value <= 0f)
                continue;

            if (key.StartsWith("chapa-", StringComparison.Ordinal))
            {
                FlushChapaField(key, value);
                continue;
            }

            if (key.StartsWith("box-", StringComparison.Ordinal))
            {
                FlushBoxField(key, value);
                continue;
            }

            switch (key)
            {
                case "max-width": _settings.MaxWidthMm = value; break;
                case "max-height": _settings.MaxHeightMm = value; break;
                case "max-depth": _settings.MaxDepthMm = value; break;
                case "coz-inf-h": _settings.CozinhaInferiorHeightMm = value; break;
                case "coz-inf-d": _settings.CozinhaInferiorDepthMm = value; break;
                case "coz-sup-baixo-h": _settings.CozinhaSuperiorBaixoHeightMm = value; break;
                case "coz-sup-h": _settings.CozinhaSuperiorHeightMm = value; break;
                case "coz-sup-alto-h": _settings.CozinhaSuperiorAltoHeightMm = value; break;
                case "coz-sup-d": _settings.CozinhaSuperiorDepthMm = value; break;
                case "coz-ilha-d": _settings.CozinhaIlhaDepthMm = value; break;
                case "coz-desp-h": _settings.CozinhaDespenseiroHeightMm = value; break;
                case "coz-desp-d": _settings.CozinhaDespenseiroDepthMm = value; break;
                case "coz-vis-tamp-h": _settings.CozinhaVistaTampoHeightMm = value; break;
                case "coz-tamp-av": _settings.CozinhaTampoAvancoMm = value; break;
                case "coz-rod-rec": _settings.CozinhaRodapeRecuoMm = value; break;
                case "coz-mol-eng": _settings.CozinhaMolduraEngrossProfMm = value; break;
                case "coz-vis-inf-h": _settings.CozinhaVistaInferiorHeightMm = value; break;
                case "coz-vis-inf-rec": _settings.CozinhaVistaInferiorRecuoMm = value; break;
                case "coz-drawer-gap": _settings.CozinhaDrawerFrontGapMm = value; break;
                case "dor-arm-h": _settings.DormitorioArmarioHeightMm = value; break;
                case "dor-arm-d": _settings.DormitorioArmarioDepthMm = value; break;
                case "dor-banc-h": _settings.DormitorioBancadaHeightMm = value; break;
                case "dor-banc-d": _settings.DormitorioBancadaDepthMm = value; break;
                case "dor-cri-h": _settings.DormitorioCriadoHeightMm = value; break;
                case "dor-cri-d": _settings.DormitorioCriadoDepthMm = value; break;
                case "dor-sup-h": _settings.DormitorioSuperiorHeightMm = value; break;
                case "dor-sup-d": _settings.DormitorioSuperiorDepthMm = value; break;
                case "dor-tamp-av": _settings.DormitorioTampoAvancoMm = value; break;
                case "dor-mol-eng": _settings.DormitorioMolduraEngrossProfMm = value; break;
                case "pan-w": _settings.PainelWidthMm = value; break;
                case "pan-h": _settings.PainelHeightMm = value; break;
                case "pan-t": _settings.PainelThicknessMm = value; break;
                case "pan-chap-t": _settings.PainelThicknessMm = value; break;
            }
        }

        if (touchedInferior)
            BoxAssemblyConfiguratorService.SyncInferiorToLegacy(_settings.CozinhaInferiorBox);
        if (touchedSuperior)
            BoxAssemblyConfiguratorService.SyncSuperiorToLegacy(_settings.CozinhaSuperiorBox);
        if (touchedDespenseiros)
            BoxAssemblyConfiguratorService.SyncDespenseirosToLegacy(_settings.CozinhaDespenseiroBox);
        if (touchedDorArm)
            BoxAssemblyConfiguratorService.SyncArmarioToLegacy(_settings.DormitorioArmarioBox);
        if (touchedDorBanc)
            BoxAssemblyConfiguratorService.SyncInferiorToLegacy(_settings.DormitorioBancadaCriadoBox);
        if (touchedDorSup)
            BoxAssemblyConfiguratorService.SyncSuperiorToLegacy(_settings.DormitorioSuperiorBox);
        if (touchedPortas)
            FrentesPortasConfiguratorService.SyncToLegacy(_settings);
    }

    private void FlushCavaChoice(string compositeKey, ComboBox combo)
    {
        if (!TryParsePortasFieldKey(compositeKey, out var nodeId, out var fieldKey))
            return;

        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
        {
            _settings.CozinhaCava.Choice[
                CavaConfiguratorService.MakeKey(nodeId, fieldKey)] = option;
        }
    }

    private void FlushGavetasInternasChoice(string compositeKey, ComboBox combo)
    {
        if (!TryParsePortasFieldKey(compositeKey, out var nodeId, out var fieldKey))
            return;

        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
        {
            _settings.CozinhaGavetasInternas.Choice[
                GavetasInternasConfiguratorService.MakeKey(nodeId, fieldKey)] = option;
        }
    }

    private void FlushGavetasChoice(string compositeKey, ComboBox combo)
    {
        if (!TryParsePortasFieldKey(compositeKey, out var nodeId, out var fieldKey))
            return;

        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
        {
            _settings.CozinhaGavetas.Choice[
                GavetasConfiguratorService.MakeKey(nodeId, fieldKey)] = option;
        }
    }

    private void FlushPortasChoice(string compositeKey, ComboBox combo)
    {
        if (!TryParsePortasFieldKey(compositeKey, out var nodeId, out var fieldKey))
            return;

        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
        {
            _settings.CozinhaFrentesPortas.Choice[
                FrentesPortasConfiguratorService.MakeKey(nodeId, fieldKey)] = option;
        }
    }

    private static bool TryParsePortasFieldKey(string compositeKey, out string nodeId, out string fieldKey)
    {
        nodeId = "";
        fieldKey = "";
        int sep = compositeKey.IndexOf("::", StringComparison.Ordinal);
        if (sep <= 0 || sep >= compositeKey.Length - 2)
            return false;

        nodeId = compositeKey[..sep];
        fieldKey = compositeKey[(sep + 2)..];
        return nodeId.Length > 0 && fieldKey.Length > 0;
    }

    private void FlushInferiorChoice(string fieldKey, ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
            _settings.CozinhaInferiorBox.InferiorChoice[fieldKey] = option;
    }

    private void FlushSuperiorChoice(string fieldKey, ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
            _settings.CozinhaSuperiorBox.SuperiorChoice[fieldKey] = option;
    }

    private void FlushDespenseirosChoice(string fieldKey, ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
            _settings.CozinhaDespenseiroBox.DespenseirosChoice[fieldKey] = option;
    }

    private void FlushEletrosChoice(string fieldKey, ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem { Tag: string option })
            _settings.CozinhaEletros.Choice[fieldKey] = option;
    }

    private BoxAssemblySectionSettings GetActiveBoxSection()
    {
        if (_activeSection.StartsWith("dor-box-arm-", StringComparison.Ordinal))
            return _settings.DormitorioArmarioBox;

        if (_activeSection.StartsWith("dor-box-banc-", StringComparison.Ordinal))
            return _settings.DormitorioBancadaCriadoBox;

        if (_activeSection.StartsWith("dor-box-sup-dor-", StringComparison.Ordinal))
            return _settings.DormitorioSuperiorBox;

        if (_activeSection.StartsWith("coz-box-sup-", StringComparison.Ordinal))
            return _settings.CozinhaSuperiorBox;

        if (_activeSection.StartsWith("coz-box-desp-", StringComparison.Ordinal))
            return _settings.CozinhaDespenseiroBox;

        return _settings.CozinhaInferiorBox;
    }

    private void FlushBoxCombo(string key, ComboBox combo)
    {
        if (combo.SelectedItem is not ComboBoxItem item || item.Tag is not BoxBackPanelType type)
            return;

        if (key == "box-fundo-type")
            GetActiveBoxSection().BackPanelType = type;
    }

    private void FlushBoxField(string key, float value)
    {
        if (!key.StartsWith("box-", StringComparison.Ordinal))
            return;

        var section = GetActiveBoxSection();
        string body = key["box-".Length..];

        switch (body)
        {
            case "fundo-recess": section.BackRecessMm = value; break;
            case "fundo-sarrafo-h": section.SarrafoHeightMm = value; break;
            case "fundo-sarrafo-t": section.SarrafoThicknessMm = value; break;
            case "fix-lat-base-overlap": section.LateralBaseOverlapMm = value; break;
            case "sarrafo-sarrafo-h": section.SarrafoHeightMm = value; break;
            case "sarrafo-sarrafo-t": section.SarrafoThicknessMm = value; break;
            case "prateleira-shelf-d": section.ShelfDepthInsetMm = value; break;
            case "prateleira-shelf-w": section.ShelfWidthInsetMm = value; break;
        }
    }

    private static bool IsDormitorioChapaSection(string sectionId) =>
        sectionId.StartsWith("dor-chapa-", StringComparison.Ordinal)
        || sectionId.StartsWith("dor-componentes-", StringComparison.Ordinal)
        || sectionId.StartsWith("dor-gav-sap-", StringComparison.Ordinal);

    private void FlushChapaField(string key, float value)
    {
        const string prefix = "chapa-";
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
            return;

        string body = key[prefix.Length..];
        string field;
        string kind;

        if (body.EndsWith("-max-w", StringComparison.Ordinal))
        {
            field = "max-w";
            kind = body[..^"-max-w".Length];
        }
        else if (body.EndsWith("-max-l", StringComparison.Ordinal))
        {
            field = "max-l";
            kind = body[..^"-max-l".Length];
        }
        else if (body.EndsWith("-thick", StringComparison.Ordinal))
        {
            field = "thick";
            kind = body[..^"-thick".Length];
        }
        else
        {
            return;
        }

        var piece = IsDormitorioChapaSection(_activeSection)
            ? _settings.DormitorioChapas.GetOrCreate(kind)
            : _settings.CozinhaChapas.GetOrCreate(kind);

        switch (field)
        {
            case "max-w": piece.MaxWidthMm = value; break;
            case "max-l": piece.MaxLengthMm = value; break;
            case "thick": piece.ThicknessMm = value; break;
        }
    }

    private static bool TryParseMm(string text, out float value)
    {
        value = 0f;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return float.TryParse(text.Trim().Replace(',', '.'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private bool TryCommitSettings()
    {
        FlushActiveSection();

        if (_settings.MaxWidthMm <= 0f || _settings.MaxHeightMm <= 0f || _settings.MaxDepthMm <= 0f)
        {
            MessageBox.Show(this,
                "Informe medidas máximas maiores que zero.",
                "Configurador de Dimensões",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        ResultSettings = BuildResultSettings();
        return true;
    }

    private void OnDefaultEditFieldChanged() { }

    private void ApplySelectedCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (ApplySelectedCheck.IsChecked == true)
        {
            _mutualExclusiveCheckboxUpdate = true;
            ApplyAllCheck.IsChecked = false;
            _mutualExclusiveCheckboxUpdate = false;
        }
    }

    private void ApplySelectedCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_mutualExclusiveCheckboxUpdate && ApplyAllCheck.IsChecked != true)
        {
            FlushActiveSection();
            RestorePanelFromProjectDefault();
        }
    }

    private void ApplyAllCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (ApplyAllCheck.IsChecked == true)
        {
            _mutualExclusiveCheckboxUpdate = true;
            ApplySelectedCheck.IsChecked = false;
            _mutualExclusiveCheckboxUpdate = false;
        }
    }

    private void ApplyAllCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_mutualExclusiveCheckboxUpdate && ApplySelectedCheck.IsChecked != true)
        {
            FlushActiveSection();
            RestorePanelFromProjectDefault();
        }
    }

    private void ResolveApplyScope()
    {
        if (ApplyAllCheck.IsChecked == true)
            ApplyScope = DimensionConfiguratorApplyScope.AllExistingAndNext;
        else if (ApplySelectedCheck.IsChecked == true)
            ApplyScope = DimensionConfiguratorApplyScope.SelectedAndNext;
        else
            ApplyScope = DimensionConfiguratorApplyScope.NextInsertionsOnly;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCommitSettings())
            return;

        ResolveApplyScope();

        if (ApplyScope == DimensionConfiguratorApplyScope.NextInsertionsOnly)
        {
            // Sem checkbox: salva como padrão de engenharia (projeto + perfil global)
            SaveProjectDefault();
        }
        else
        {
            // Com checkbox: aplica nos módulos da cena; painel volta ao padrão salvo
            if (ApplyScope == DimensionConfiguratorApplyScope.SelectedAndNext && !HasSelectedModule)
            {
                MessageBox.Show(this,
                    "Selecione um ou mais módulos na cena para aplicar a configuração.",
                    "Configurador de Dimensões",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            OnApply?.Invoke(ResultSettings, ApplyScope);
            RestorePanelFromProjectDefault();
            ClearApplyCheckboxes();
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        bool hasCheckbox = ApplySelectedCheck.IsChecked == true || ApplyAllCheck.IsChecked == true;

        if (hasCheckbox)
        {
            // Com checkbox: OK aplica nos módulos (mesmo fluxo do Aplicar) e fecha.
            if (!TryCommitSettings())
                return;

            ResolveApplyScope();

            if (ApplyScope == DimensionConfiguratorApplyScope.SelectedAndNext && !HasSelectedModule)
            {
                MessageBox.Show(this,
                    "Selecione um ou mais módulos na cena para aplicar a configuração.",
                    "Configurador de Dimensões",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            OnApply?.Invoke(ResultSettings, ApplyScope);
            Close();
            return;
        }

        if (!TryCommitSettings())
            return;

        // Sem checkbox: grava padrão e fecha
        SaveProjectDefault();
        Close();
    }

    private void SaveProjectDefault()
    {
        ResultSettings = BuildResultSettings();
        OnAutoSave?.Invoke(ResultSettings);
        _projectDefaultSnapshot = ResultSettings.Clone();
    }

    private void ClearApplyCheckboxes()
    {
        _mutualExclusiveCheckboxUpdate = true;
        ApplySelectedCheck.IsChecked = false;
        ApplyAllCheck.IsChecked = false;
        _mutualExclusiveCheckboxUpdate = false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
    }

    private DimensionConfiguratorSettings BuildResultSettings()
    {
        var clone = _settings.Clone();
        ChapaConfiguratorService.SyncLegacyChapaFields(clone);
        BoxAssemblyConfiguratorService.SyncLegacyShelfFields(clone);
        FrentesPortasConfiguratorService.SyncToLegacy(clone);
        return clone;
    }
}
