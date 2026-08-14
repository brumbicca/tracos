namespace Tracos3DStudio;

public static class BudgetAuditService
{
    public static BudgetAuditReport Run(Project project)
    {
        var findings = new List<BudgetAuditFinding>();
        var summary = BudgetService.Build(project);

        if (project.Modules.Count == 0)
        {
            findings.Add(Finding(
                BudgetAuditSeverity.Error,
                "NO_MODULES",
                "O projeto não possui módulos para orçar."));
        }

        foreach (var item in summary.Items.Where(i => !i.HasPrice))
        {
            findings.Add(Finding(
                BudgetAuditSeverity.Error,
                "MODULE_NO_PRICE",
                $"Módulo \"{item.Description}\" está sem preço base.",
                item.Description));
        }

        var partItems = summary.Sections
            .Where(s => s.Name == "— Peças")
            .SelectMany(s => s.Items)
            .Where(i => !i.HasPrice);

        foreach (var item in partItems)
        {
            findings.Add(Finding(
                BudgetAuditSeverity.Warning,
                "PART_NO_PRICE",
                $"Peça \"{item.Description}\" sem preço de material configurado.",
                item.Description));
        }

        AddCollisionFindings(project, findings);
        AddCustomModuleFindings(project, findings);
        AddDimensionFindings(project, findings);

        if (string.IsNullOrWhiteSpace(project.Metadata.ClientName))
        {
            findings.Add(Finding(
                BudgetAuditSeverity.Warning,
                "CLIENT_MISSING",
                "Nome do cliente não informado — recomendado para o PDF comercial."));
        }

        if (project.Room.Walls.Count == 0)
        {
            findings.Add(Finding(
                BudgetAuditSeverity.Info,
                "NO_WALLS",
                "Ambiente sem paredes — confira as medidas do local antes de fechar o orçamento."));
        }

        return new BudgetAuditReport { Findings = findings };
    }

    private static void AddCollisionFindings(Project project, List<BudgetAuditFinding> findings)
    {
        var collidingIds = ModuleCollisionService.FindCollidingModuleIds(project.Modules);

        if (collidingIds.Count == 0)
            return;

        var names = collidingIds
            .Select(id => project.FindModule(id))
            .Where(m => m != null)
            .Select(m => ModuleCatalog.GetRequired(m!.DefinitionId).DisplayName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        string list = string.Join(", ", names);

        findings.Add(Finding(
            BudgetAuditSeverity.Warning,
            "MODULE_COLLISION",
            names.Count == 1
                ? $"Módulo em colisão: {list}."
                : $"{names.Count} módulos em colisão: {list}.",
            list));
    }

    private static void AddCustomModuleFindings(Project project, List<BudgetAuditFinding> findings)
    {
        foreach (var module in project.Modules)
        {
            if (!ModuleCatalog.IsCustom(module.DefinitionId))
                continue;

            if (project.Metadata.TryGetModulePrice(module.DefinitionId, module.Id, out _))
                continue;

            if (LibraryState.TryGetModulePrice(module.DefinitionId, out _))
                continue;

            var definition = ModuleCatalog.GetRequired(module.DefinitionId);
            decimal fallback = BudgetService.GetDefaultBasePrice(module.DefinitionId);

            findings.Add(Finding(
                BudgetAuditSeverity.Warning,
                "CUSTOM_NO_LIBRARY_PRICE",
                $"Módulo personalizado \"{definition.DisplayName}\" usa preço padrão ({fallback:C2}).",
                definition.DisplayName));
        }
    }

    private static void AddDimensionFindings(Project project, List<BudgetAuditFinding> findings)
    {
        foreach (var module in project.Modules)
        {
            if (module.Width > 0 && module.Height > 0 && module.Depth > 0)
                continue;

            var definition = ModuleCatalog.GetRequired(module.DefinitionId);

            findings.Add(Finding(
                BudgetAuditSeverity.Error,
                "INVALID_DIMENSIONS",
                $"Módulo \"{definition.DisplayName}\" com dimensões inválidas.",
                definition.DisplayName));
        }
    }

    private static BudgetAuditFinding Finding(
        BudgetAuditSeverity severity,
        string code,
        string message,
        string? relatedItem = null) =>
        new()
        {
            Severity = severity,
            Code = code,
            Message = message,
            RelatedItem = relatedItem
        };
}
