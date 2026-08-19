using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleCornerMeshBuilderTests
{
    [Fact]
    public void Cr_Configurador_ExpoeParametrosPromobComDefaultsCorretos()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;

        Assert.Equal(18f, box.InferiorNumeric["cr-affb"]);
        Assert.Equal(18f, box.InferiorNumeric["cr-affs"]);
        Assert.Equal(18f, box.InferiorNumeric["cr-affl"]);
        Assert.Equal(-12f, box.InferiorNumeric["cr-affd"]);
        Assert.Equal(0f, box.InferiorNumeric["cr-rec-prat"]);
        Assert.Equal(27f, box.InferiorNumeric["cr-ava-por"]);
        Assert.Equal(80f, box.InferiorNumeric["crf-recuo-fro"]);
        Assert.Equal(30f, box.InferiorNumeric["crf-dim-fro"]);
        Assert.Equal(30f, box.InferiorNumeric["cr-afa-lat"]);
        Assert.Equal("Usar", box.InferiorChoice["cr-uso-dist"]);
        Assert.Equal("Total", box.InferiorChoice["crs-tipo-fro"]);

        var canto = BoxAssemblyInferiorSchema.FindNode("canto-reto-canto");
        Assert.NotNull(canto);
        Assert.Equal(14, canto!.Fields.Count());
        Assert.Contains(canto!.Fields, f => f.Key == "cr-rec-prat"
            && f.Label.Contains("M — Recuo Prateleira", StringComparison.Ordinal));
        var fechamentos = BoxAssemblyInferiorSchema.FindNode("canto-reto-fechamentos");
        Assert.NotNull(fechamentos);
        Assert.Equal(7, fechamentos!.Fields.Count());
        Assert.Contains(fechamentos.Fields, f => f.Key == "crf-pos-lat"
            && f.Label.StartsWith("D —", StringComparison.Ordinal)
            && f.Kind == BoxFieldKind.Numeric
            && f.AllowNegative);
        Assert.True(BoxAssemblyInferiorSchema.AllowsNegative("cr-affd"));
        Assert.True(BoxAssemblyInferiorSchema.AllowsNegative("crf-recuo-fro"));
    }

    [Theory]
    [InlineData("canto-cr-esq-950")]
    [InlineData("canto-cr-2p-esq-1245")]
    [InlineData("canto-cr-dir-950")]
    [InlineData("canto-cr-2p-dir-1245")]
    public void Cr_ContemPecasDaCaixaEFrentes(string definitionId)
    {
        var instance = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);
        var labels = instance.Mesh.Faces
            .Select(f => f.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Lateral esq.", labels);
        Assert.Contains("Lateral dir.", labels);
        Assert.Contains("Base inferior", labels);
        Assert.Contains("Fundo", labels);
        Assert.Contains("Prateleira", labels);
        // Sarrafo traseiro segue sar-tipo do configurador (default Frontal = só dianteiro).
        Assert.Contains("Sarrafo dianteiro", labels);
        Assert.Contains("Frente falsa", labels);
        Assert.Contains("Fechamento frontal", labels);
        Assert.DoesNotContain("Fechamento lateral", labels);
        Assert.Contains(labels, l => l.StartsWith("Porta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cr_Esq_Fechamento_30xEspessuraFrente_RecuaSemMoverPorta()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["crf-dim-fro"] = 30f;
        settings.CozinhaInferiorBox.InferiorNumeric["crf-recuo-fro"] = 80f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-ava-por"] = 0f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 670f, 550f, definition, settings, respectCatalogLimits: false);

        var porta = instance.Mesh.Faces.Where(f => f.Label is "Porta" or "Porta 1").ToList();
        var fech = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal").ToList();
        var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa").ToList();
        Assert.NotEmpty(porta);
        Assert.NotEmpty(fech);
        Assert.NotEmpty(falsa);

        float portaMinX = porta.SelectMany(f => f.Vertices).Min(v => v.X);
        float fechMinX = fech.SelectMany(f => f.Vertices).Min(v => v.X);
        float fechMaxX = fech.SelectMany(f => f.Vertices).Max(v => v.X);
        float fechMinZ = fech.SelectMany(f => f.Vertices).Min(v => v.Z);
        float fechMaxZ = fech.SelectMany(f => f.Vertices).Max(v => v.Z);
        float portaMinZ = porta.SelectMany(f => f.Vertices).Min(v => v.Z);
        float falsaMinZ = falsa.SelectMany(f => f.Vertices).Min(v => v.Z);
        float falsaMaxZ = falsa.SelectMany(f => f.Vertices).Max(v => v.Z);

        Assert.True(portaMinX >= fechMaxX - 2f, "CR Esq: canto cego à esquerda e porta à direita");
        // Tipo Lateral (padrão Promob): espessura ~18 em X, dimensão 30 avançando em Z.
        Assert.True(MathF.Abs((fechMaxX - fechMinX) - 18f) < 2f,
            $"Espessura do fechamento (X) ~18 mm, foi {fechMaxX - fechMinX}");
        Assert.True(MathF.Abs((fechMaxZ - fechMinZ) - 30f) < 2f,
            $"Dimensão do fechamento (Z) = 30 mm (Promob), foi {fechMaxZ - fechMinZ}");
        Assert.True(portaMinZ >= 550f - 1f,
            $"Porta deve começar na face da caixaria (minZ={portaMinZ}, depth=550)");
        Assert.True(MathF.Abs(portaMinZ - falsaMinZ) < 1.5f,
            $"Porta e frente falsa paralelas na frente da caixaria (porta={portaMinZ}, falsa={falsaMinZ})");
        Assert.True(MathF.Abs(fechMaxZ - falsaMaxZ) < 1.5f,
            $"D=18 deve alinhar a borda frontal ao exterior da frente falsa (fechMaxZ={fechMaxZ}, falsaMaxZ={falsaMaxZ})");
        Assert.True(MathF.Abs(fechMaxX - (550f - 80f)) < 3f,
            $"B=80 deve deslocar lateralmente o fechamento, X=[{fechMinX},{fechMaxX}]");
        Assert.True(falsaMaxZ > 550f + 5f,
            $"Frente falsa deve ultrapassar a caixaria (maxZ={falsaMaxZ})");
    }

    [Fact]
    public void Cr_FechamentoLateral_AletaAlinhadaAoSequencialOutraParede()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["crf-tipo"] = "Lateral";
        settings.CozinhaInferiorBox.InferiorNumeric["crf-dim-fro"] = 30f;
        settings.CozinhaInferiorBox.InferiorNumeric["crf-recuo-fro"] = 80f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-dir-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);

        var fech = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal").SelectMany(f => f.Vertices).ToList();
        var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa").SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(fech);
        Assert.NotEmpty(falsa);

        float xSpan = fech.Max(v => v.X) - fech.Min(v => v.X);
        float zSpan = fech.Max(v => v.Z) - fech.Min(v => v.Z);
        float zMax = fech.Max(v => v.Z);
        float xMax = fech.Max(v => v.X);
        float falsaMinZ = falsa.Min(v => v.Z);

        Assert.True(MathF.Abs(xSpan - 18f) < 2f, $"Espessura X={xSpan}, esperado ~18");
        Assert.True(MathF.Abs(zSpan - 30f) < 2f, $"Dimensão Z={zSpan}, esperado 30");
        Assert.True(MathF.Abs(fech.Max(v => v.Z) - (550f + 18f)) < 1.5f,
            $"D=18 deve avançar a borda frontal 18 mm (zMax={fech.Max(v => v.Z)})");
        float xMin = fech.Min(v => v.X);
        Assert.True(MathF.Abs(xMin - (400f + 80f)) < 2f,
            $"B=80 deve deslocar lateralmente a peça (xMin={xMin}, esperado 480)");
    }

    [Fact]
    public void Cr_FechamentoLateral_AletaParaModuloSequencial()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["crf-tipo"] = "Lateral";
        settings.CozinhaInferiorBox.InferiorNumeric["crf-dim-fro"] = 30f;
        settings.CozinhaInferiorBox.InferiorNumeric["crf-recuo-fro"] = 80f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        var fech = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal").SelectMany(f => f.Vertices).ToList();
        var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa").SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(fech);

        float xSpan = fech.Max(v => v.X) - fech.Min(v => v.X);
        float zSpan = fech.Max(v => v.Z) - fech.Min(v => v.Z);
        float zMax = fech.Max(v => v.Z);
        float falsaMinZ = falsa.Min(v => v.Z);
        float hingeAlign = 580f; // CR Esq: canto cego à esquerda, face em X=d
        float xMax = fech.Max(v => v.X);

        Assert.True(MathF.Abs(xSpan - 18f) < 2f, $"Espessura X={xSpan}, esperado ~18");
        Assert.True(MathF.Abs(zSpan - 30f) < 2f, $"Dimensão Z={zSpan}, esperado 30");
        Assert.True(zSpan > xSpan, "Aleta: Z (dimensão) > X (espessura)");
        Assert.True(MathF.Abs(fech.Max(v => v.Z) - (580f + 18f)) < 1.5f,
            $"D=18 deve avançar a borda frontal 18 mm (zMax={fech.Max(v => v.Z)})");
        Assert.True(MathF.Abs(xMax - (hingeAlign - 80f)) < 2f,
            $"B=80 deve alterar somente a distância lateral (xMax={xMax})");
    }

    [Fact]
    public void Cr_FechamentoLateral_BNegativoMoveParaOLadoEDPositivoAvancaParaFora()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorChoice["crf-tipo"] = "Lateral";
        box.InferiorChoice["cr-uso-dist"] = "Não usar";
        box.InferiorNumeric["crf-recuo-fro"] = -18f;
        box.InferiorNumeric["crf-dim-fro"] = 30f;
        box.InferiorNumeric["crf-pos-lat"] = 18f;
        box.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);

        var fech = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal")
            .SelectMany(f => f.Vertices).ToList();
        var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa")
            .SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(fech);
        Assert.NotEmpty(falsa);

        float zMin = fech.Min(v => v.Z);
        float zMax = fech.Max(v => v.Z);
        float xMax = fech.Max(v => v.X);
        Assert.Equal(550f + 18f, zMax, 0.5f);
        Assert.Equal(30f, zMax - zMin, 0.5f);
        Assert.Equal(550f + 18f, xMax, 0.5f);
        Assert.Equal(550f + 18f - 30f, zMin, 0.5f);
    }

    [Fact]
    public void Cr_FechamentoLateral_DAssinadoMoveEmProfundidadeSemAlterarB()
    {
        static (float MinX, float MaxX, float MinZ, float MaxZ) Build(float position)
        {
            var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
            var settings = DimensionConfiguratorSettings.CreateDefault();
            BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
            var box = settings.CozinhaInferiorBox;
            box.InferiorChoice["crf-tipo"] = "Lateral";
            box.InferiorNumeric["crf-pos-lat"] = position;
            box.InferiorNumeric["crf-recuo-fro"] = 40f;
            box.InferiorNumeric["crf-dim-fro"] = 30f;
            box.InferiorNumeric["cr-afa-lat"] = 0f;

            var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
            instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);
            var vertices = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal")
                .SelectMany(f => f.Vertices).ToList();
            return (vertices.Min(v => v.X), vertices.Max(v => v.X),
                vertices.Min(v => v.Z), vertices.Max(v => v.Z));
        }

        var external = Build(18f);
        var internalPosition = Build(-18f);

        Assert.Equal(external.MinX, internalPosition.MinX, 0.5f);
        Assert.Equal(external.MaxX, internalPosition.MaxX, 0.5f);
        Assert.Equal(538f, external.MinZ, 0.5f);
        Assert.Equal(568f, external.MaxZ, 0.5f);
        Assert.Equal(502f, internalPosition.MinZ, 0.5f);
        Assert.Equal(532f, internalPosition.MaxZ, 0.5f);
    }

    [Theory]
    [InlineData("canto-cr-esq-950", 0f, 950f)]
    [InlineData("canto-cr-dir-950", 0f, 950f)]
    public void Cr_AfastamentoDeslocaEnvelopeDeSelecaoCompleto(
        string definitionId,
        float expectedMinX,
        float expectedMaxX)
    {
        var definition = ModuleCatalog.GetRequired(definitionId);
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 30f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-tra"] = 0f;

        var instance = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);
        instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);
        var (min, max) = instance.GetBounds();

        Assert.Equal(expectedMinX, min.X, 0.5f);
        Assert.Equal(expectedMaxX, max.X, 0.5f);
        Assert.Equal(950f, max.X - min.X, 0.5f);
    }

    [Fact]
    public void Cr_FechamentoFrontal_FaixaNoPlanoDaPorta()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["crf-tipo"] = "Frontal";
        settings.CozinhaInferiorBox.InferiorNumeric["crf-dim-fro"] = 30f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        var fech = instance.Mesh.Faces.Where(f => f.Label == "Fechamento frontal").SelectMany(f => f.Vertices).ToList();
        float xSpan = fech.Max(v => v.X) - fech.Min(v => v.X);
        float zSpan = fech.Max(v => v.Z) - fech.Min(v => v.Z);
        Assert.True(MathF.Abs(xSpan - 30f) < 1.5f, $"Largura frontal X={xSpan}");
        Assert.True(zSpan < 40f && zSpan > 10f, $"Espessura Z={zSpan}");
        Assert.True(xSpan > zSpan, "Tipo Frontal: faixa no plano da porta (X > Z)");
    }

    [Fact]
    public void Cr_AfastamentoLateral_PositivoAfastaDoCantoNosDoisLados()
    {
        static (float MinX, float MaxX) FundoBoundsX(string id, float afastamento)
        {
            var definition = ModuleCatalog.GetRequired(id);
            var settings = DimensionConfiguratorSettings.CreateDefault();
            BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
            settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = afastamento;
            settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-tra"] = 0f;
            settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Não usar";

            var instance = ModuleCatalog.CreateInstance(id, Vector3.Zero);
            instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);
            var vertices = instance.Mesh.Faces.Where(f => f.Label == "Fundo")
                .SelectMany(f => f.Vertices).ToList();
            return (vertices.Min(v => v.X), vertices.Max(v => v.X));
        }

        var esqZero = FundoBoundsX("canto-cr-esq-950", 0f);
        var esqTrinta = FundoBoundsX("canto-cr-esq-950", 30f);
        Assert.Equal(30f, esqTrinta.MinX - esqZero.MinX, 0.5f);
        Assert.Equal(0f, esqTrinta.MaxX - esqZero.MaxX, 0.5f);

        var dirZero = FundoBoundsX("canto-cr-dir-950", 0f);
        var dirTrinta = FundoBoundsX("canto-cr-dir-950", 30f);
        Assert.Equal(0f, dirTrinta.MinX - dirZero.MinX, 0.5f);
        Assert.Equal(-30f, dirTrinta.MaxX - dirZero.MaxX, 0.5f);
    }

    [Fact]
    public void Cr_AvancosBCD_ControlamSobreposicaoDaFrenteFalsa()
    {
        static (float MinX, float MinY, float MaxY) FrenteFalsa(float avanco)
        {
            var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
            var settings = DimensionConfiguratorSettings.CreateDefault();
            BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
            var numeric = settings.CozinhaInferiorBox.InferiorNumeric;
            settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Não usar";
            numeric["cr-afa-lat"] = 0f;
            numeric["cr-affb"] = avanco;
            numeric["cr-affs"] = avanco;
            numeric["cr-affl"] = avanco;

            var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
            instance.SetDimensions(950f, 720f, 550f, definition, settings, respectCatalogLimits: false);
            var vertices = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa")
                .SelectMany(f => f.Vertices).ToList();
            return (vertices.Min(v => v.X), vertices.Min(v => v.Y), vertices.Max(v => v.Y));
        }

        var zero = FrenteFalsa(0f);
        var dezoito = FrenteFalsa(18f);
        Assert.Equal(-18f, dezoito.MinX - zero.MinX, 0.5f);
        Assert.Equal(-18f, dezoito.MinY - zero.MinY, 0.5f);
        Assert.Equal(18f, dezoito.MaxY - zero.MaxY, 0.5f);
    }

    [Fact]
    public void Cr_PortaEFalsa_NaoFicamEmbutidasNaCaixaria()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        float d = instance.Depth;
        var laterais = instance.Mesh.Faces.Where(f =>
            f.Label is "Lateral esq." or "Lateral dir.").ToList();
        var porta = instance.Mesh.Faces.Where(f => f.Label is "Porta" or "Porta 1").ToList();
        var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa").ToList();

        float lateralMaxZ = laterais.SelectMany(f => f.Vertices).Max(v => v.Z);
        float portaMinZ = porta.SelectMany(f => f.Vertices).Min(v => v.Z);
        float portaMaxZ = porta.SelectMany(f => f.Vertices).Max(v => v.Z);
        float falsaMinZ = falsa.SelectMany(f => f.Vertices).Min(v => v.Z);

        Assert.True(MathF.Abs(lateralMaxZ - d) < 2f, $"Caixaria termina em z={d} (foi {lateralMaxZ})");
        Assert.True(portaMinZ >= lateralMaxZ - 1f,
            $"Porta não pode ficar embutida (portaMinZ={portaMinZ} < lateralMaxZ={lateralMaxZ})");
        Assert.True(portaMaxZ > lateralMaxZ + 10f,
            $"Porta deve avançar à frente da caixaria (portaMaxZ={portaMaxZ})");
        Assert.True(MathF.Abs(portaMinZ - falsaMinZ) < 1.5f,
            "Porta e frente falsa alinhadas no mesmo plano frontal");
    }

    [Fact]
    public void Cr_Esq_FrentesFicamAposProfundidadeDaCaixa()
    {
        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        float boxDepth = instance.Depth;

        var frontFaces = instance.Mesh.Faces.Where(f =>
            f.Label is "Frente falsa" or "Fechamento frontal" or "Porta" or "Porta 1" or "Porta 2").ToList();

        Assert.NotEmpty(frontFaces);

        float maxFrontZ = frontFaces
            .SelectMany(f => f.Vertices)
            .Max(v => v.Z);

        Assert.True(maxFrontZ > boxDepth + 5f,
            $"Frentes devem ultrapassar a caixa (maxZ={maxFrontZ}, depth={boxDepth})");
    }

    [Fact]
    public void Cr_2p_Esq_GeraDuasPortas()
    {
        var instance = ModuleCatalog.CreateInstance("canto-cr-2p-esq-1245", Vector3.Zero);
        var labels = instance.Mesh.Faces.Select(f => f.Label).Distinct().ToList();

        Assert.Contains("Porta 1", labels);
        Assert.Contains("Porta 2", labels);
    }

    [Fact]
    public void Cr_FundoESarrafoTraseiro_IguaisAoBalcaoReto()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 8f;
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 9f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-afb"] = 7f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Ambos";
        settings.CozinhaInferiorBox.InferiorNumeric["sar-prof-tra"] = 55f;
        settings.CozinhaInferiorHeightMm = 720f;
        settings.CozinhaInferiorDepthMm = 580f;

        var balDef = ModuleCatalog.GetRequired("balcao-2-portas");
        var crDef = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var (bw, bh, bd) = DimensionConfiguratorService.ResolveInsertionDimensions(balDef, settings);
        var (cw, ch, cd) = DimensionConfiguratorService.ResolveInsertionDimensions(crDef, settings);

        var balcao = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        balcao.SetDimensions(bw, bh, bd, balDef, settings, respectCatalogLimits: false);

        var cr = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        cr.SetDimensions(cw, ch, cd, crDef, settings, respectCatalogLimits: false);

        Assert.Equal(bh, ch, 0.1f);
        Assert.Equal(bd, cd, 0.1f);

        static (float MinX, float MaxX, float MinY, float MinZ, float MaxZ) Bounds(
            ModuleInstance m, string label)
        {
            var faces = m.Mesh.Faces.Where(f => f.Label == label).ToList();
            Assert.True(faces.Count > 0, $"Peça '{label}' ausente");
            var verts = faces.SelectMany(f => f.Vertices).ToList();
            return (verts.Min(v => v.X), verts.Max(v => v.X),
                verts.Min(v => v.Y), verts.Min(v => v.Z), verts.Max(v => v.Z));
        }

        var fundoBal = Bounds(balcao, "Fundo");
        var fundoCr = Bounds(cr, "Fundo");
        // Mesmos avanços fundo↔lateral e fundo↔base (X/Y), independente da largura do módulo.
        Assert.True(MathF.Abs(fundoBal.MinX - fundoCr.MinX) < 1.5f,
            $"Fundo minX balcão={fundoBal.MinX} CR={fundoCr.MinX}");
        Assert.True(MathF.Abs(fundoBal.MinY - fundoCr.MinY) < 1.5f,
            $"Fundo minY balcão={fundoBal.MinY} CR={fundoCr.MinY}");
        Assert.True(MathF.Abs(fundoBal.MinZ - fundoCr.MinZ) < 1.5f,
            $"Fundo minZ (recuo) balcão={fundoBal.MinZ} CR={fundoCr.MinZ}");

        var sarBal = Bounds(balcao, "Sarrafo traseiro");
        var sarCr = Bounds(cr, "Sarrafo traseiro");
        Assert.True(MathF.Abs((sarBal.MaxZ - sarBal.MinZ) - (sarCr.MaxZ - sarCr.MinZ)) < 1.5f,
            "Profundidade do sarrafo traseiro deve ser a mesma (sar-prof-tra)");
        Assert.True(MathF.Abs(sarBal.MaxZ - sarCr.MaxZ) < 1.5f,
            "Sarrafo traseiro deve terminar na mesma profundidade");
    }

    [Fact]
    public void Cr_ApplyToModules_AtualizaAlturaProfundidadeEFundo()
    {
        var project = new Project();
        DimensionConfiguratorService.EnsureProjectSettings(project);
        var settings = DimensionConfiguratorService.GetSettings(project);
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorHeightMm = 800f;
        settings.CozinhaInferiorDepthMm = 600f;
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 10f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Ambos";
        project.Metadata.DimensionSettings = settings;

        var definition = ModuleCatalog.GetRequired("canto-cr-dir-950");
        var module = project.AddModule("canto-cr-dir-950", Vector3.Zero);
        module.SetDimensions(950f, 670f, 550f, definition, settings, respectCatalogLimits: false);

        settings.CozinhaInferiorHeightMm = 810f;
        settings.CozinhaInferiorDepthMm = 610f;
        DimensionConfiguratorService.ApplyToModules(
            project, settings, DimensionConfiguratorApplyScope.AllExistingAndNext, null);

        Assert.Equal(810f, module.Height, 0.1f);
        Assert.Equal(610f, module.Depth, 0.1f);

        var fundo = module.Mesh.Faces.Where(f => f.Label == "Fundo").ToList();
        Assert.NotEmpty(fundo);
        float minX = fundo.SelectMany(f => f.Vertices).Min(v => v.X);
        Assert.True(MathF.Abs(minX - (18f - 10f)) < 1.5f, $"Fundo deve aplicar afl=10 (minX={minX})");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Sarrafo traseiro");
    }

    [Fact]
    public void Cr_SemDistanciador_NaoGeraPeca()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Não";

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        Assert.NotNull(instance.BlindCorner);
        Assert.False(instance.BlindCorner!.UseSpacer);
        Assert.DoesNotContain(instance.Mesh.Faces, f => f.Label == "Distanciador");
        Assert.Equal(720f, instance.Height, 0.1f);
        Assert.Equal(580f, instance.Depth, 0.1f);
    }

    [Fact]
    public void Cr_ComDistanciador_GeraPecaERecuaPrateleira()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Sim";
        settings.CozinhaInferiorBox.InferiorNumeric["cr-rec-prat"] = 70f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        Assert.True(instance.BlindCorner!.UseSpacer);
        Assert.Contains(instance.Mesh.Faces, f => f.Label == "Distanciador");

        var shelf = instance.Mesh.Faces.Where(f => f.Label == "Prateleira").ToList();
        Assert.NotEmpty(shelf);
        float shelfMaxZ = shelf.SelectMany(f => f.Vertices).Max(v => v.Z);
        Assert.True(shelfMaxZ <= 580f - 70f + 2f,
            $"Com distanciador, prateleira deve respeitar recuo 70 mm (maxZ={shelfMaxZ})");
    }

    [Fact]
    public void Cr_Distanciador_JKLM_AplicamRelacoesPromobNosEixosCorretos()
    {
        static (float FalsaMaxX, float DistMinZ, float DistMaxZ, float ShelfMaxZ) Build(
            float j, float k, float l, float m)
        {
            var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
            var settings = DimensionConfiguratorSettings.CreateDefault();
            BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
            var box = settings.CozinhaInferiorBox;
            box.InferiorChoice["cr-uso-dist"] = "Usar";
            box.InferiorChoice["cr-tipo-ff"] = "Inteira";
            box.InferiorNumeric["cr-afa-lat"] = 0f;
            box.InferiorNumeric["cr-affd"] = j;
            box.InferiorNumeric["cr-adff"] = k;
            box.InferiorNumeric["cr-adp"] = l;
            box.InferiorNumeric["cr-rec-prat"] = m;

            var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
            instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);
            var falsa = instance.Mesh.Faces.Where(f => f.Label == "Frente falsa")
                .SelectMany(f => f.Vertices).ToList();
            var dist = instance.Mesh.Faces.Where(f => f.Label == "Distanciador")
                .SelectMany(f => f.Vertices).ToList();
            var shelf = instance.Mesh.Faces.Where(f => f.Label == "Prateleira")
                .SelectMany(f => f.Vertices).ToList();
            return (falsa.Max(v => v.X), dist.Min(v => v.Z), dist.Max(v => v.Z), shelf.Max(v => v.Z));
        }

        var zero = Build(0f, 0f, 0f, 70f);
        var j = Build(12f, 0f, 0f, 70f);
        var k = Build(0f, 20f, 0f, 70f);
        var l = Build(0f, 0f, 20f, 70f);
        var m = Build(0f, 0f, 0f, 100f);

        Assert.Equal(12f, j.FalsaMaxX - zero.FalsaMaxX, 0.5f);
        Assert.Equal(20f, k.DistMaxZ - zero.DistMaxZ, 0.5f);
        Assert.Equal(-20f, l.DistMinZ - zero.DistMinZ, 0.5f);
        Assert.Equal(-30f, m.ShelfMaxZ - zero.ShelfMaxZ, 0.5f);
        Assert.Equal(-30f, m.DistMinZ - zero.DistMinZ, 0.5f);
    }

    [Fact]
    public void Cr_RecuoPrateleiraM_ZeroNaoHerdaRecuoGenerico()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Usar";
        settings.CozinhaInferiorBox.InferiorNumeric["prat-recuo"] = 20f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-rec-prat"] = 0f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        var shelf = instance.Mesh.Faces.Where(f => f.Label == "Prateleira").SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(shelf);
        Assert.Equal(580f, shelf.Max(v => v.Z), 0.5f);
        Assert.Contains(instance.Mesh.Faces, f => f.Label == "Distanciador");
    }

    [Fact]
    public void Cr_TravessaSustentacao_FicaInteiramenteAtrasDoFundo()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorChoice["fundo-tipo"] = "Inteiro";
        box.InferiorChoice["fundo-trav-sust"] = "1";
        box.InferiorNumeric["fundo-recuo"] = 20f;
        box.InferiorNumeric["fundo-dim-trav-sust"] = 60f;
        box.InferiorNumeric["cr-afa-lat"] = 0f;

        var instance = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        var fundo = instance.Mesh.Faces.Where(f => f.Label == "Fundo").SelectMany(f => f.Vertices).ToList();
        var travessa = instance.Mesh.Faces.Where(f => f.Label == "Travessa de sustentação")
            .SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(fundo);
        Assert.NotEmpty(travessa);
        Assert.True(travessa.Max(v => v.Z) <= fundo.Min(v => v.Z) + 0.1f,
            $"Travessa maxZ={travessa.Max(v => v.Z)} deve terminar atrás do fundo minZ={fundo.Min(v => v.Z)}");
    }

    [Fact]
    public void Cr_SarrafoParcial_UsaMesmoRecuoFrontalDoBalcao()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["crs-tipo-fro"] = "Parcial";
        settings.CozinhaInferiorBox.InferiorChoice["cr-uso-dist"] = "Não usar";
        settings.CozinhaInferiorBox.InferiorNumeric["sar-recuo-fro"] = 35f;
        settings.CozinhaInferiorBox.InferiorNumeric["cr-afa-lat"] = 0f;

        var crDefinition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var cr = ModuleCatalog.CreateInstance(crDefinition.Id, Vector3.Zero);
        cr.SetDimensions(950f, 720f, 580f, crDefinition, settings, respectCatalogLimits: false);

        var balcaoDefinition = ModuleCatalog.GetRequired("balcao-2-portas");
        var balcao = ModuleCatalog.CreateInstance(balcaoDefinition.Id, Vector3.Zero);
        balcao.SetDimensions(800f, 720f, 580f, balcaoDefinition, settings, respectCatalogLimits: false);

        static float MaxZ(ModuleInstance module) => module.Mesh.Faces
            .Where(f => f.Label == "Sarrafo dianteiro").SelectMany(f => f.Vertices).Max(v => v.Z);

        Assert.Equal(MaxZ(balcao), MaxZ(cr), 0.5f);
        Assert.Equal(580f - 35f, MaxZ(cr), 0.5f);
    }

    [Fact]
    public void Cr_AplicaAlturaProfundidadeDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 810f;
        settings.CozinhaInferiorDepthMm = 600f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-cr-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);

        Assert.Equal(810f, instance.Height, 0.1f);
        Assert.Equal(600f, instance.Depth, 0.1f);

        var lateral = instance.Mesh.Faces.Where(f => f.Label == "Lateral esq.").ToList();
        float latH = lateral.SelectMany(f => f.Vertices).Max(v => v.Y)
                   - lateral.SelectMany(f => f.Vertices).Min(v => v.Y);
        Assert.True(MathF.Abs(latH - 810f) < 2f, $"Altura da lateral deve seguir configurador ({latH})");
    }

    [Fact]
    public void Cr_SemSarrafoFrontal_OmitePeca()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["crs-tipo-fro"] = "Sem sarrafo";

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 670f, 550f, definition, settings, respectCatalogLimits: false);

        Assert.DoesNotContain(instance.Mesh.Faces, f => f.Label == "Sarrafo dianteiro");
    }

    [Theory]
    [InlineData("canto-l-2p-esq-950")]
    [InlineData("canto-obliquo-1p-900")]
    public void OutrosCantos_AindaGeramMalha(string definitionId)
    {
        var instance = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);
        Assert.True(instance.Mesh.Vertices.Count > 24);
        Assert.Contains(instance.Mesh.Faces, f => f.Kind == FaceKind.ModuleFront);
    }
}
