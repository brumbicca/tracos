using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class DrawerModuleBuilderTests
{
    [Fact]
    public void Gaveteiro4G_CriaQuatroConjuntosComCaixaFundoECorredicas()
    {
        var module = ModuleCatalog.CreateInstance("gaveteiro", Vector3.Zero);

        for (int i = 1; i <= 4; i++)
        {
            string assembly = DrawerPartNaming.Assembly(i);
            string[] parts =
            [
                "Frente", "Lateral esq.", "Lateral dir.", "Contra-frente",
                "Posterior", "Fundo", "Corrediça esq.", "Corrediça dir."
            ];

            foreach (string part in parts)
                Assert.Contains(module.Mesh.Faces, face =>
                    face.Label == $"{assembly} — {part}");
        }
    }

    [Fact]
    public void FolgaCorredica_AlteraParametricamenteALarguraDaCaixa()
    {
        var definition = ModuleCatalog.GetRequired("gaveteiro");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        settings.CozinhaGavetas.Numeric[
            GavetasConfiguratorService.MakeKey("folgas", "folg-cor-tel")] = 12.5f;

        var module = new ModuleInstance { DefinitionId = definition.Id, Position = Vector3.Zero };
        module.ApplyDefinition(definition);
        module.RebuildMesh(definition, settings);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Lateral esq.", out var leftMin, out _));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Lateral dir.", out _, out var rightMax));
        Assert.Equal(settings.CozinhaPanelThicknessMm + 12.5f, leftMin.X, 3);
        Assert.Equal(module.Width - settings.CozinhaPanelThicknessMm - 12.5f, rightMax.X, 3);
    }

    [Fact]
    public void CaixaDaGaveta_AlinhaNoPlanoFrontalDaCaixaria()
    {
        var module = ModuleCatalog.CreateInstance("gaveteiro", Vector3.Zero);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Lateral esq.", out _, out var lateralMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Contra-frente", out _, out var counterMax));

        Assert.Equal(module.Depth, lateralMax.Z, 3);
        Assert.Equal(module.Depth, counterMax.Z, 3);
    }

    [Fact]
    public void RedimensionarModulo_RecalculaCaixasSemPerderPecas()
    {
        var definition = ModuleCatalog.GetRequired("gaveteiro");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(520f, 900f, 600f, definition,
            DimensionConfiguratorSettings.CreateDefault(), respectCatalogLimits: false);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 4 — Lateral dir.", out _, out var rightMax));
        Assert.True(rightMax.X < module.Width);
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Gaveta 4 — Fundo");
        Assert.All(module.Mesh.Vertices, vertex =>
            Assert.True(float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z)));
    }

    [Fact]
    public void OcultarGavetaInteira_OcultaTodasAsPecasDoConjunto()
    {
        var module = ModuleCatalog.CreateInstance("gaveteiro", Vector3.Zero);

        Assert.True(SceneOcclusionService.HidePart(module, "Gaveta 2"));
        Assert.Contains("Gaveta 2 — Frente", module.HiddenPartLabels);
        Assert.Contains("Gaveta 2 — Fundo", module.HiddenPartLabels);
        Assert.DoesNotContain("Gaveta 1 — Fundo", module.HiddenPartLabels);
    }

    [Fact]
    public void Decomposicao_IncluiPecasInternasDeCadaGaveta()
    {
        var definition = ModuleCatalog.GetRequired("gaveteiro");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        var pieces = ModuleDecompositionService.Decompose(
            module, definition, 18f, 6f, DimensionConfiguratorSettings.CreateDefault());

        Assert.Contains(pieces, piece => piece.Name == "Gaveta 1 — Lateral" && piece.Quantity == 2);
        Assert.Contains(pieces, piece => piece.Name == "Gaveta 4 — Contra-frente");
        Assert.Contains(pieces, piece => piece.Name == "Gaveta 4 — Posterior");
        Assert.Contains(pieces, piece => piece.Name == "Gaveta 4 — Fundo");
    }

    [Fact]
    public void ModuloMisto_MantemPortasEGavetasEmFaixasDistintas()
    {
        var module = ModuleCatalog.CreateInstance("gav-1g-2p-400", Vector3.Zero);
        var labels = module.Mesh.Faces.Select(face => face.Label).Distinct().ToList();

        Assert.Contains("Porta 1", labels);
        Assert.Contains("Porta 2", labels);
        Assert.Contains("Gaveta 1 — Frente", labels);
        Assert.Contains("Gaveta 1 — Fundo", labels);
    }

    [Fact]
    public void GavetasInternas_FicamAtrasDaPortaComFrentePropria()
    {
        var module = ModuleCatalog.CreateInstance("gav-1p-2gav-int-450", Vector3.Zero);
        var labels = module.Mesh.Faces.Select(face => face.Label).Distinct().ToList();

        Assert.Contains("Porta 1", labels);
        Assert.Contains("Gaveta 1 — Frente interna", labels);
        Assert.Contains("Gaveta 2 — Frente interna", labels);
        Assert.Contains("Gaveta 2 — Corrediça dir.", labels);
    }

    [Fact]
    public void IdentidadeHierarquica_DistingueConjuntoEPeça()
    {
        Assert.True(DrawerPartNaming.IsAssemblySelection("Gaveta 3"));
        Assert.True(DrawerPartNaming.BelongsToAssembly("Gaveta 3 — Lateral esq.", "Gaveta 3"));
        Assert.False(DrawerPartNaming.BelongsToAssembly("Gaveta 2 — Fundo", "Gaveta 3"));
        Assert.True(DrawerPartNaming.MatchesSelection("Gaveta 3 — Fundo", "Gaveta 3"));
    }

    [Fact]
    public void ConfiguradorDeGavetas_UsaMedidasLivresAssinadas()
    {
        Assert.All(CozinhaGavetasSchema.AllNodes().SelectMany(node => node.Fields), field =>
        {
            Assert.Equal(BoxFieldKind.Numeric, field.Kind);
            Assert.True(field.AllowNegative);
            Assert.Empty(field.Options);
        });
        Assert.All(CozinhaGavetasInternasSchema.AllNodes().SelectMany(node => node.Fields), field =>
        {
            Assert.Equal(BoxFieldKind.Numeric, field.Kind);
            Assert.True(field.AllowNegative);
            Assert.Empty(field.Options);
        });

        var settings = DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        string key = GavetasConfiguratorService.MakeKey("folgas", "folg-cor-tel");
        settings.CozinhaGavetas.Numeric[key] = -2.75f;
        Assert.Equal(-2.75f, settings.Clone().CozinhaGavetas.Numeric[key]);
    }

    [Fact]
    public void PerfisDeCorredica_TelescopicaEInvisivel_UsamFolgasIndependentes()
    {
        var definition = ModuleCatalog.GetRequired("gaveteiro");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        settings.CozinhaGavetas.Numeric[
            GavetasConfiguratorService.MakeKey("folgas", "folg-cor-tel")] = 14f;
        settings.CozinhaGavetas.Numeric[
            GavetasConfiguratorService.MakeKey("folgas", "folg-cor-inv")] = 5f;

        var telescopic = new ModuleInstance { DefinitionId = definition.Id, Position = Vector3.Zero };
        telescopic.ApplyDefinition(definition);
        telescopic.DrawerSlideType = DrawerSlideType.Telescopic;
        telescopic.RebuildMesh(definition, settings);

        var concealed = new ModuleInstance { DefinitionId = definition.Id, Position = Vector3.Zero };
        concealed.ApplyDefinition(definition);
        concealed.DrawerSlideType = DrawerSlideType.Concealed;
        concealed.RebuildMesh(definition, settings);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            telescopic, "Gaveta 1 — Lateral esq.", out var telMin, out _));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            concealed, "Gaveta 1 — Lateral esq.", out var invMin, out _));
        Assert.Equal(settings.CozinhaPanelThicknessMm + 14f, telMin.X, 3);
        Assert.Equal(settings.CozinhaPanelThicknessMm + 5f, invMin.X, 3);
        Assert.True(invMin.X < telMin.X);
    }

    [Fact]
    public void Gavetao_UsaFolgasVerticaisProprias()
    {
        var definition = ModuleCatalog.GetRequired("gav-2g-1gav-400");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        foreach (string field in new[] { "folg-sup-lat", "folg-inf-lat" })
            settings.CozinhaGavetas.Numeric[GavetasConfiguratorService.MakeKey("folgas", field)] = 0f;
        foreach (string field in new[] { "fgav-sup-lat", "fgav-inf-lat" })
            settings.CozinhaGavetas.Numeric[GavetasConfiguratorService.MakeKey("folgas", field)] = 35f;

        var module = new ModuleInstance { DefinitionId = definition.Id, Position = Vector3.Zero };
        module.ApplyDefinition(definition);
        module.RebuildMesh(definition, settings);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Lateral esq.", out var largeMin, out var largeMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 2 — Lateral esq.", out var normalMin, out var normalMax));
        Assert.True((largeMax.Y - largeMin.Y) < (normalMax.Y - normalMin.Y));
    }

    [Fact]
    public void Folgas_ExibemTodosOsGruposDaDocumentacaoEPortaLatas()
    {
        var node = CozinhaGavetasSchema.FindNode("folgas");
        Assert.NotNull(node);
        string[] groups = node!.Fields.Select(field => field.Group).Where(group => group != null)
            .Distinct().Cast<string>().ToArray();

        Assert.Contains("Gavetas Externas", groups);
        Assert.Contains("Gavetas", groups);
        Assert.Contains("Gavetão", groups);
        Assert.Contains("Gaveta Inferior | Porta-Latas MDF", groups);
        Assert.Contains("Gaveta Superior | Porta-Latas MDF", groups);
        Assert.Contains(node.Fields, field => field.Key == "folg-cor-tel");
        Assert.Contains(node.Fields, field => field.Key == "folg-cor-inv");
        Assert.Equal(14, node.Fields.Count(field => field.Key.StartsWith("pl-", StringComparison.Ordinal)));
    }
}
