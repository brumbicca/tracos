using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class BalconyModuleBuilderTests
{
    [Theory]
    [InlineData("bal-toalheiro-150", "Barra do toalheiro")]
    [InlineData("bal-adega-150", "Berço inclinado")]
    [InlineData("bal-porta-latas-200", "Cesto porta-latas")]
    [InlineData("bal-porta-latas-mdf-200", "Cesto MDF")]
    [InlineData("bal-porta-temperos-150", "Cesto de temperos")]
    [InlineData("bal-tulha-400", "Cesto interno da tulha")]
    [InlineData("bal-lixeira-400", "Lixeira interna")]
    [InlineData("bal-1p-basc-600", "Pistão basculante")]
    [InlineData("bal-ilha-800", "Painel traseiro de acabamento da ilha")]
    [InlineData("balcao-1p-400", "Dobradiça caneco")]
    [InlineData("balcao-2-portas", "Dobradiça caneco")]
    [InlineData("balcao-3-portas", "Dobradiça caneco")]
    public void Balcao_ContemEngenhariaEspecificaEAcessorio3D(string definitionId, string expectedLabel)
    {
        var module = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);

        Assert.Contains(module.Mesh.Faces, face =>
            face.Label.Contains(expectedLabel, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Lateral esq.");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Lateral dir.");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Base inferior");
        Assert.All(module.Mesh.Vertices, vertex =>
            Assert.True(float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z)));
        Assert.All(module.Mesh.Normals, normal =>
            Assert.True(float.IsFinite(normal.X) && float.IsFinite(normal.Y) && float.IsFinite(normal.Z)));
    }

    [Fact]
    public void BalcoesDescartados_ForamRemovidosCompletamente()
    {
        string[] hiddenIds =
        [
            "bal-adega-circ-150",
            "bal-2p-curvo-800",
            "bal-2p-bifold-800",
            "bal-escamoteavel-800"
        ];

        foreach (string id in hiddenIds)
            Assert.False(ModuleCatalog.TryGet(id, out _));
    }

    [Fact]
    public void Toalheiro_TemVaoFrontalAbertoSemPortaOuFrenteExtraivel()
    {
        var module = ModuleCatalog.CreateInstance("bal-toalheiro-150", Vector3.Zero);

        Assert.DoesNotContain(module.Mesh.Faces, face =>
            face.Label.Contains("Frente extraível", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(module.Mesh.Faces, face =>
            face.Label.StartsWith("Porta", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Barra do toalheiro");
    }

    [Fact]
    public void Balcao3P_UsaUmUnicoSkuEAtalhoIParaEspelharOVaoDuplo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-3-portas");
        var left = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        var right = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        right.IsMirrored = true;
        right.RebuildMesh(definition, DimensionConfiguratorSettings.CreateDefault());

        Assert.Equal("3P 1200mm", definition.DisplayName);
        Assert.False(ModuleCatalog.TryGet("bal-3p-alt-1200", out _));

        static float DivisionCenter(ModuleInstance module)
        {
            var vertices = module.Mesh.Faces
                .Where(face => face.Label == "Divisória")
                .SelectMany(face => face.Vertices)
                .ToList();
            Assert.NotEmpty(vertices);
            return (vertices.Min(vertex => vertex.X) + vertices.Max(vertex => vertex.X)) * 0.5f;
        }

        float leftCenter = DivisionCenter(left);
        float rightCenter = DivisionCenter(right);

        Assert.True(leftCenter > left.Width * 0.5f);   // vão duplo à esquerda
        Assert.True(rightCenter < right.Width * 0.5f); // vão duplo à direita
        Assert.Equal(left.Width, leftCenter + rightCenter, 3);
        Assert.Equal(3, left.Mesh.Faces.Count(face =>
            face.Kind == FaceKind.ModuleFront && face.Label.StartsWith("Porta", StringComparison.Ordinal)));
        Assert.Equal(3, right.Mesh.Faces.Count(face =>
            face.Kind == FaceKind.ModuleFront && face.Label.StartsWith("Porta", StringComparison.Ordinal)));
        Assert.DoesNotContain(left.Mesh.Faces, face => face.Label.StartsWith("Divisória ", StringComparison.Ordinal));
        Assert.DoesNotContain(right.Mesh.Faces, face => face.Label.StartsWith("Divisória ", StringComparison.Ordinal));

        static IReadOnlyList<string> ShelfParts(ModuleInstance module) => module.Mesh.Faces
            .Where(face => face.Label.StartsWith("Prateleira ", StringComparison.Ordinal))
            .Select(face => face.Label)
            .Distinct()
            .OrderBy(label => label)
            .ToList();

        Assert.Equal(["Prateleira 1", "Prateleira 2"], ShelfParts(left));
        Assert.Equal(["Prateleira 1", "Prateleira 2"], ShelfParts(right));

        // A posição da divisão e o comprimento das prateleiras acompanham uma
        // alteração de largura, mantendo o vão duplo à esquerda/direita.
        left.SetDimensions(1500f, left.Height, left.Depth, definition,
            DimensionConfiguratorSettings.CreateDefault(), respectCatalogLimits: false);
        right.SetDimensions(1500f, right.Height, right.Depth, definition,
            DimensionConfiguratorSettings.CreateDefault(), respectCatalogLimits: false);
        leftCenter = DivisionCenter(left);
        rightCenter = DivisionCenter(right);
        Assert.Equal(left.Width, leftCenter + rightCenter, 3);
        Assert.Equal(["Prateleira 1", "Prateleira 2"], ShelfParts(left));
        Assert.Equal(["Prateleira 1", "Prateleira 2"], ShelfParts(right));
    }

    [Fact]
    public void Ilha_UsaPerfilDimensionalProprio()
    {
        Assert.Equal(
            ModuleDimensionSlot.CozinhaIlha,
            DimensionConfiguratorService.ResolveSlot(ModuleCatalog.GetRequired("bal-ilha-800")));

        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 810f;
        settings.CozinhaInferiorDepthMm = 520f;
        settings.CozinhaIlhaDepthMm = 680f;

        var dimensions = DimensionConfiguratorService.ResolveInsertionDimensions(
            ModuleCatalog.GetRequired("bal-ilha-800"), settings);

        Assert.Equal(810f, dimensions.Height);
        Assert.Equal(680f, dimensions.Depth);
    }

    [Fact]
    public void BalcaoExtraivel_ObedeceFolgasDeFrenteDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("bal-porta-latas-200");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-lat")] = "11";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-inf")] = "7";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-sup")] = "9";

        var module = new ModuleInstance
        {
            DefinitionId = definition.Id,
            Position = Vector3.Zero
        };
        module.ApplyDefinition(definition);
        module.RebuildMesh(definition, settings);

        var front = module.Mesh.Faces.First(face =>
            face.Label == "Frente extraível porta-latas" && face.Kind == FaceKind.ModuleFront);
        float minX = front.Vertices.Min(vertex => vertex.X);
        float maxX = front.Vertices.Max(vertex => vertex.X);
        float minY = front.Vertices.Min(vertex => vertex.Y);
        float maxY = front.Vertices.Max(vertex => vertex.Y);

        Assert.Equal(11f, minX, 3);
        Assert.Equal(module.Width - 11f, maxX, 3);
        Assert.Equal(7f, minY, 3);
        Assert.Equal(module.Height - 9f, maxY, 3);
    }

    [Fact]
    public void PortaLatasMdf_AplicaRecuosSuperiorEInferiorNo3D()
    {
        var definition = ModuleCatalog.GetRequired("bal-porta-latas-mdf-200");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        GavetasConfiguratorService.EnsureInitialized(settings);
        settings.CozinhaGavetas.Numeric[
            GavetasConfiguratorService.MakeKey("folgas", "pl-sup-sup-cf")] = 24f;
        settings.CozinhaGavetas.Numeric[
            GavetasConfiguratorService.MakeKey("folgas", "pl-inf-inf-lat")] = 8f;

        var module = new ModuleInstance { DefinitionId = definition.Id, Position = Vector3.Zero };
        module.ApplyDefinition(definition);
        module.RebuildMesh(definition, settings);

        var upperFront = module.Mesh.Faces
            .Where(face => face.Label == "Cesto MDF 3" && face.Kind == FaceKind.ModuleFront)
            .SelectMany(face => face.Vertices)
            .ToList();
        Assert.NotEmpty(upperFront);
        Assert.Equal(module.Depth - 40f - 24f, upperFront.Max(vertex => vertex.Z), 3);

        var lowerBase = module.Mesh.Faces
            .Where(face => face.Label == "Cesto MDF 1" && face.Kind == FaceKind.ModuleTop)
            .SelectMany(face => face.Vertices)
            .ToList();
        Assert.NotEmpty(lowerBase);
        Assert.Equal(22f + 8f, lowerBase.Min(vertex => vertex.X), 3);
    }

    [Fact]
    public void Pia2P4G_SeparaVaoDePortasEColunaDeQuatroGavetas()
    {
        var module = ModuleCatalog.CreateInstance("pia-2p-4g-1200", Vector3.Zero);
        var labels = module.Mesh.Faces.Select(face => face.Label).Distinct().ToList();

        Assert.Contains("Porta 1", labels);
        Assert.Contains("Porta 2", labels);
        Assert.Contains("Prateleira pia", labels);
        Assert.Contains("Gaveta 4 — Fundo", labels);
        Assert.DoesNotContain("Gaveta 5 — Frente", labels);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Porta 1", out _, out var doorMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Frente", out var drawerMin, out _));
        Assert.True(doorMax.X < drawerMin.X);
    }

    [Fact]
    public void Pia2P8G_UsaDuasColunasLateraisEDeixaVaoCentralLivre()
    {
        var module = ModuleCatalog.CreateInstance("pia-2p-8g-1600", Vector3.Zero);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Frente", out var leftMin, out var leftMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 5 — Frente", out var rightMin, out var rightMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Prateleira pia", out var shelfMin, out var shelfMax));

        Assert.True(leftMax.X < shelfMin.X);
        Assert.True(shelfMax.X < rightMin.X);
        Assert.True(leftMin.X < module.Width * 0.25f);
        Assert.True(rightMax.X > module.Width * 0.75f);
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Gaveta 8 — Corrediça dir.");
    }

    [Fact]
    public void Pia3P4G_ConservaTresPortasNoVaoEQuatroGavetasNaColuna()
    {
        var module = ModuleCatalog.CreateInstance("pia-3p-4g-1600", Vector3.Zero);
        var labels = module.Mesh.Faces.Select(face => face.Label).Distinct().ToList();

        Assert.Contains("Porta 1", labels);
        Assert.Contains("Porta 2", labels);
        Assert.Contains("Porta 3", labels);
        Assert.Contains("Gaveta 4 — Fundo", labels);
        Assert.DoesNotContain("Gaveta 5 — Frente", labels);
    }

    [Fact]
    public void Pia1GavBasc_TemSomenteUmaGavetaSuperiorEPortaInferior()
    {
        var module = ModuleCatalog.CreateInstance("pia-1gav-basc-800", Vector3.Zero);
        var labels = module.Mesh.Faces.Select(face => face.Label).Distinct().ToList();

        Assert.Contains("Porta basculante", labels);
        Assert.Contains("Prateleira interna", labels);
        Assert.Contains("Gaveta 1 — Fundo", labels);
        Assert.DoesNotContain("Gaveta 2 — Frente", labels);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Porta basculante", out _, out var doorMax));
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Frente", out var drawerMin, out _));
        Assert.True(drawerMin.Y < doorMax.Y);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Base inferior", out var baseMin, out var baseMax));
        Assert.Equal(0f, baseMin.Z, 3);
        Assert.Equal(module.Depth, baseMax.Z, 3);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Prateleira interna", out var shelfMin, out var shelfMax));
        Assert.Equal(18f, shelfMin.X, 3);
        Assert.Equal(module.Width - 18f, shelfMax.X, 3);
        Assert.Equal(module.Depth, shelfMax.Z, 3);
    }

    [Fact]
    public void AtalhoIEspelhaEngenhariaSemSkuDuplicado()
    {
        var definition = ModuleCatalog.GetRequired("pia-2p-4g-1200");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Frente", out var normalMin, out _));

        module.IsMirrored = true;
        module.RebuildMesh(definition, DimensionConfiguratorSettings.CreateDefault());
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Gaveta 1 — Frente", out var mirroredMin, out var mirroredMax));

        Assert.True(normalMin.X > module.Width * 0.5f);
        Assert.True(mirroredMax.X < module.Width * 0.5f);
        Assert.False(ModuleCatalog.TryGet("pia-2p-4g-1200-b", out _));
    }

    [Fact]
    public void ListaDeCorteDosCompostos_UsaAsPecasReaisDo3D()
    {
        var definition = ModuleCatalog.GetRequired("pia-2p-4g-1200");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        var pieces = ModuleDecompositionService.Decompose(
            module, definition, 18f, 6f, DimensionConfiguratorSettings.CreateDefault());

        Assert.Contains(pieces, piece => piece.Name == "Porta 1");
        Assert.Contains(pieces, piece => piece.Name == "Prateleira pia");
        Assert.Contains(pieces, piece => piece.Name == "Gaveta 4 — Fundo");
        Assert.DoesNotContain(pieces, piece => piece.Name == "Frente gaveta 1");
        Assert.DoesNotContain(pieces, piece => piece.Name.Contains("Corrediça", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("pia-2p-4g-1200")]
    [InlineData("pia-2p-8g-1600")]
    [InlineData("pia-3p-4g-1600")]
    public void BalcoesCompostos_BaseTotalEDivisoesDepoisDoFundo(string definitionId)
    {
        var module = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Base inferior", out var baseMin, out var baseMax));
        Assert.Equal(0f, baseMin.Z, 3);
        Assert.Equal(module.Depth, baseMax.Z, 3);

        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, "Fundo", out _, out var backMax));
        string divisionLabel = definitionId == "pia-2p-8g-1600"
            ? "Divisória 1"
            : "Divisória";
        Assert.True(ModulePartDimensionService.TryComputeLocalBounds(
            module, divisionLabel, out var divisionMin, out var divisionMax));
        Assert.True(divisionMin.Z >= backMax.Z - 0.01f);
        Assert.True(divisionMax.Z <= module.Depth + 0.01f);
    }
}
