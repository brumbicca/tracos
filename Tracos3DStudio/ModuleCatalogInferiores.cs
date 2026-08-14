namespace Tracos3DStudio;

/// <summary>
/// Catálogo Cozinhas → Inferiores — nomes, ordem e silhuetas do Promob Plus 5.60 (imagens de referência).
/// </summary>
public static class ModuleCatalogInferiores
{
    private const float H = 850f;
    private const float D = 550f;

    public static void AddAll(Action<ModuleDefinition> add)
    {
        int order = 0;

        // —— Cantos (1/11) ——
        add(Inf("canto-cr-esq-950", "CR Esq 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 1, shape: ModuleShapeKind.BlindCornerLeft));
        add(Inf("canto-cr-2p-esq-1245", "CR 2P Esq 1245mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 1245f, doors: 2, shape: ModuleShapeKind.BlindCornerLeft));
        add(Inf("canto-cr-dir-950", "CR Dir 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 1, shape: ModuleShapeKind.BlindCornerRight));
        add(Inf("canto-cr-2p-dir-1245", "CR 2P Dir 1245mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 1245f, doors: 2, shape: ModuleShapeKind.BlindCornerRight));
        add(Inf("canto-l-esq-950", "\"L\" Esq 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 1, shape: ModuleShapeKind.CornerLLeft));
        add(Inf("canto-l-2p-esq-950", "\"L\" 2P Esq 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 2, shape: ModuleShapeKind.CornerLLeft));
        add(Inf("canto-l-dir-950", "\"L\" Dir 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 1, shape: ModuleShapeKind.CornerLRight));
        add(Inf("canto-l-2p-dir-950", "\"L\" 2P Dir 950mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 950f, doors: 2, shape: ModuleShapeKind.CornerLRight));
        add(Inf("canto-obliquo-1p-900", "Obliquo 1P 900mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 900f, doors: 1, shape: ModuleShapeKind.Oblique));
        add(Inf("canto-obliquo-2p-900", "Obliquo 2P 900mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 900f, doors: 2, shape: ModuleShapeKind.Oblique));
        add(Inf("canto-obliquo-1p-ajust-900", "Obliquo 1P Ajust 900mm", ModuleLibraryHierarchy.SubCantos,
            ref order, 900f, doors: 1, shape: ModuleShapeKind.Oblique));

        // —— Cantos Bifold (1/2) ——
        order = 0;
        add(Inf("canto-bifold-l-esq-950", "\"L\" Esq 950mm", ModuleLibraryHierarchy.SubCantosBifold,
            ref order, 950f, doors: 2, shape: ModuleShapeKind.Bifold));
        add(Inf("canto-bifold-l-dir-950", "\"L\" Dir 950mm", ModuleLibraryHierarchy.SubCantosBifold,
            ref order, 950f, doors: 2, shape: ModuleShapeKind.Bifold));

        // —— Balcões (1/17) ——
        order = 0;
        add(Inf("bal-toalheiro-150", "Toalheiro 150mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 150f, doors: 0, shape: ModuleShapeKind.PullOutNarrow, minW: 100f, maxW: 200f));
        add(Inf("bal-adega-150", "Adega 150mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 150f, doors: 0, shape: ModuleShapeKind.WineRack, minW: 100f, maxW: 200f));
        add(Inf("bal-adega-circ-150", "Adega Circ 150mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 150f, doors: 0, shape: ModuleShapeKind.WineRack, minW: 100f, maxW: 200f));
        add(Inf("bal-porta-latas-200", "Porta-Latas 200mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 200f, doors: 0, shape: ModuleShapeKind.PullOutNarrow, minW: 150f, maxW: 250f));
        add(Inf("bal-porta-latas-mdf-200", "Porta-Latas MDF 200mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 200f, doors: 0, shape: ModuleShapeKind.PullOutNarrow, minW: 150f, maxW: 250f));
        add(Inf("bal-porta-temperos-150", "Porta Temperos 150mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 150f, doors: 0, shape: ModuleShapeKind.PullOutNarrow, minW: 100f, maxW: 200f));
        add(Inf("bal-tulha-400", "Tulha 400mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 400f, doors: 1, shape: ModuleShapeKind.Standard));
        add(Inf("balcao-1p-400", "1P 400mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 400f, doors: 1, shape: ModuleShapeKind.Standard));
        add(Inf("bal-lixeira-400", "Lixeira 400mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 400f, doors: 1, shape: ModuleShapeKind.PullOutNarrow));
        // IDs legados (AutomationId / testes / projetos antigos)
        add(Inf("balcao-2-portas", "2P 800mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.Standard,
            minW: 400f, maxW: 1200f, minD: 450f, maxD: 650f));
        add(Inf("bal-2p-bifold-800", "2P Bifold 800mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.Bifold));
        add(Inf("bal-1p-basc-600", "1P Basc 600mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 600f, doors: 1, shape: ModuleShapeKind.Standard));
        add(Inf("bal-ilha-800", "Ilha 800mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.Standard, depthMm: 600f));
        add(Inf("bal-escamoteavel-800", "Escamoteável 800mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 800f, doors: 1, shape: ModuleShapeKind.PullOutNarrow));
        add(Inf("bal-2p-curvo-800", "2P Curvo 800mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.CurvedFront));
        add(Inf("balcao-3-portas", "3P 1200mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 1200f, doors: 3, shape: ModuleShapeKind.Standard, minW: 900f, maxW: 1800f));
        add(Inf("bal-3p-alt-1200", "_3P 1200mm", ModuleLibraryHierarchy.SubBalcoes,
            ref order, 1200f, doors: 3, shape: ModuleShapeKind.Standard, minW: 900f, maxW: 1800f));

        // —— Especiais (1/6) ——
        order = 0;
        add(Inf("esp-2p-col-esq", "2P Col Esq", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));
        add(Inf("esp-2p-col-dir-800", "2P Col Dir 800mm", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));
        add(Inf("esp-2p-col-central-800", "2P Col Central 800mm", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));
        add(Inf("esp-2p-col-esq-800", "2P Col Esq 800mm", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));
        add(Inf("esp-2p-col-dir-800-b", "2P Col Dir 800mm", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));
        add(Inf("esp-2p-col-central-800-b", "2P Col Central 800mm", ModuleLibraryHierarchy.SubEspeciais,
            ref order, 800f, doors: 2, shape: ModuleShapeKind.ColumnDoors));

        // —— Gaveteiros (1/17) ——
        order = 0;
        add(Inf("gaveteiro", "4G 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 4, shape: ModuleShapeKind.Standard));
        add(Inf("gav-4g-curvo-400", "4G Curvo 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 4, shape: ModuleShapeKind.CurvedFront));
        add(Inf("gav-2g-1gav-400", "2G+1Gav 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 3, shape: ModuleShapeKind.Standard));
        add(Inf("gav-2g-1p-400", "2G+1P 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, doors: 1, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1g-1p-400", "1G+1P 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, doors: 1, drawers: 1, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1g-2p-400", "1G+2P 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, doors: 2, drawers: 1, shape: ModuleShapeKind.Standard));
        add(Inf("gav-2g-2p-800", "2G+2P 800mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 800f, doors: 2, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("gav-3g-400", "3G 400", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 3, shape: ModuleShapeKind.Standard));
        add(Inf("gav-tabua-400", "Tábua 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 1, shape: ModuleShapeKind.PullOutNarrow));
        add(Inf("gav-1g-1p-desli-400", "1G+1P Desli 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, doors: 1, drawers: 1, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1g-2gav-600", "1G+2Gav 600mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 600f, drawers: 3, shape: ModuleShapeKind.Standard));
        add(Inf("gav-2gav-800", "2Gav 800mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 800f, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("gav-2gav-1g-aux-800", "2Gav+1G Aux 800mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 800f, drawers: 3, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1p-2gav-int-450", "1P+2Gav Int 450mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 450f, doors: 1, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1p-4gav-int-450", "1P+4Gav Int 450mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 450f, doors: 1, drawers: 4, shape: ModuleShapeKind.Standard));
        add(Inf("gav-2gav-aram-800", "2Gav Aram 800mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 800f, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("gav-1g-400", "1G 400mm", ModuleLibraryHierarchy.SubGaveteiros,
            ref order, 400f, drawers: 1, shape: ModuleShapeKind.Standard));

        // —— p/ Eletros (1/8) ——
        order = 0;
        add(Inf("elet-forno-600", "Forno 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, doors: 0, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-forno-gav-600", "Forno c/ Gav 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, drawers: 1, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-forno-gav-600-b", "Forno c/ Gav 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, drawers: 1, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-forno-respiros-600", "Forno Respiros 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, doors: 0, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-lava-louca-500", "Lava-Louça 500mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 500f, doors: 0, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-lava-louca-600", "Lava-Louça 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, doors: 0, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-fogao-600", "Fogão 600mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 600f, doors: 0, shape: ModuleShapeKind.ApplianceBay));
        add(Inf("elet-6q-900", "6Q 900mm", ModuleLibraryHierarchy.SubEletros,
            ref order, 900f, doors: 0, shape: ModuleShapeKind.ApplianceBay));

        // —— Pias (1/10) ——
        order = 0;
        add(Inf("pia-2gav-800", "2Gav 800mm", ModuleLibraryHierarchy.SubPias,
            ref order, 800f, drawers: 2, shape: ModuleShapeKind.Standard));
        add(Inf("pia-1gav-basc-800", "1Gav/Basc 800mm", ModuleLibraryHierarchy.SubPias,
            ref order, 800f, drawers: 1, shape: ModuleShapeKind.Standard));
        add(Inf("pia-1p-600", "1P 600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 600f, doors: 1, shape: ModuleShapeKind.Standard));
        add(Inf("pia-2p-600", "2P 600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 600f, doors: 2, shape: ModuleShapeKind.Standard));
        add(Inf("pia-2p-4g-1200", "2P+4G 1200mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1200f, doors: 2, drawers: 4, shape: ModuleShapeKind.Standard, minW: 1000f, maxW: 1600f));
        add(Inf("pia-2p-8g-1600", "2P+8G 1600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1600f, doors: 2, drawers: 8, shape: ModuleShapeKind.Standard, minW: 1400f, maxW: 1800f));
        add(Inf("pia-3p-4g-1600", "3P+4G 1600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1600f, doors: 3, drawers: 4, shape: ModuleShapeKind.Standard, minW: 1400f, maxW: 1800f));
        add(Inf("pia-2p-4g-1200-b", "2P+4G 1200mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1200f, doors: 2, drawers: 4, shape: ModuleShapeKind.Standard, minW: 1000f, maxW: 1600f));
        add(Inf("pia-2p-8g-1600-b", "2P+8G 1600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1600f, doors: 2, drawers: 8, shape: ModuleShapeKind.Standard, minW: 1400f, maxW: 1800f));
        add(Inf("pia-3p-4g-1600-b", "3P+4G 1600mm", ModuleLibraryHierarchy.SubPias,
            ref order, 1600f, doors: 3, drawers: 4, shape: ModuleShapeKind.Standard, minW: 1400f, maxW: 1800f));

        // —— Diagonais (1/8) ——
        order = 0;
        add(Inf("diag-esq-300", "Diag Esq 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndDiagonal, minW: 250f, maxW: 400f));
        add(Inf("diag-dir-300", "Diag Dir 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndDiagonal, minW: 250f, maxW: 400f));
        add(Inf("diag-curvo-esq-300", "Curvo Esq 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndCurved, minW: 250f, maxW: 400f));
        add(Inf("diag-curvo-dir-300", "Curvo Dir 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndCurved, minW: 250f, maxW: 400f));
        add(Inf("diag-chanf-esq-300", "Chanf Esq 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndChamfer, minW: 250f, maxW: 400f));
        add(Inf("diag-chanf-dir-300", "Chanf Dir 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndChamfer, minW: 250f, maxW: 400f));
        add(Inf("diag-z-esq-300", "Z Esq 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndZ, minW: 250f, maxW: 400f));
        add(Inf("diag-z-dir-300", "Z Dir 300mm", ModuleLibraryHierarchy.SubDiagonais,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.EndZ, minW: 250f, maxW: 400f));

        // —— Cantoneiras (1/6) ——
        order = 0;
        add(Inf("canton-chanf-esq-300", "Chanf Esq 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));
        add(Inf("canton-chanf-dir-300", "Chanf Dir 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));
        add(Inf("canton-diag-esq-300", "Diag Esq 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));
        add(Inf("canton-diag-dir-300", "Diag Dir 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));
        add(Inf("canton-curva-esq-300", "Curva Esq 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));
        add(Inf("canton-curva-dir-300", "Curva Dir 300mm", ModuleLibraryHierarchy.SubCantoneiras,
            ref order, 300f, doors: 0, shape: ModuleShapeKind.OpenCornerShelves, minW: 250f, maxW: 400f));

        // —— Fechamentos (1/1) ——
        order = 0;
        add(Inf("fechamento", "Fechamento", ModuleLibraryHierarchy.SubFechamentos,
            ref order, 50f, doors: 0, shape: ModuleShapeKind.Filler, minW: 18f, maxW: 200f, depthMm: 18f));
    }

    private static ModuleDefinition Inf(
        string id,
        string name,
        string sub,
        ref int order,
        float width,
        int doors = 0,
        int drawers = 0,
        ModuleShapeKind shape = ModuleShapeKind.Standard,
        float minW = 0f,
        float maxW = 0f,
        float depthMm = D,
        float heightMm = H,
        float minD = 0f,
        float maxD = 0f) =>
        new()
        {
            Id = id,
            DisplayName = name,
            Category = ModuleCategory.Cozinha,
            LibraryGroup = ModuleLibraryHierarchy.GroupInferiores,
            LibrarySubGroup = sub,
            CatalogOrder = order++,
            ShapeKind = shape,
            DefaultWidth = width,
            DefaultHeight = heightMm,
            DefaultDepth = depthMm,
            MinWidth = minW > 0 ? minW : MathF.Max(100f, width * 0.7f),
            MaxWidth = maxW > 0 ? maxW : width * 1.4f,
            MinHeight = shape == ModuleShapeKind.Filler ? 200f : 700f,
            MaxHeight = shape == ModuleShapeKind.Filler ? 3000f : 1000f,
            MinDepth = minD > 0 ? minD : (shape == ModuleShapeKind.Filler ? 18f : depthMm * 0.8f),
            MaxDepth = maxD > 0 ? maxD : (shape == ModuleShapeKind.Filler ? 50f : depthMm * 1.2f),
            DoorCount = doors,
            DrawerCount = drawers
        };
}
