using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class CornerLModuleBuilderTests
{
    [Fact]
    public void ContornoL_SeguePontosSolicitados()
    {
        var c = CornerLModuleBuilder.BuildLContour(950f, 900f, 550f, 500f);

        Assert.Equal(6, c.Length);
        Assert.Equal(new Vector2(0f, 0f), c[0]);
        Assert.Equal(new Vector2(950f, 0f), c[1]);
        Assert.Equal(new Vector2(950f, 550f), c[2]);
        Assert.Equal(new Vector2(500f, 550f), c[3]);
        Assert.Equal(new Vector2(500f, 900f), c[4]);
        Assert.Equal(new Vector2(0f, 900f), c[5]);
    }

    [Fact]
    public void ContornoInterno_DescontaLateraisETravessas()
    {
        float cd = 950f, ce = 900f, pd = 550f, pe = 500f, t = 18f, trav = 18f;
        var inner = CornerLModuleBuilder.BuildInternalLContour(cd, ce, pd, pe, t, trav);

        Assert.Equal(new Vector2(trav, trav), inner[0]);
        Assert.Equal(new Vector2(cd - t, trav), inner[1]);
        Assert.Equal(new Vector2(cd - t, pd), inner[2]);
        Assert.Equal(new Vector2(pe, pd), inner[3]);
        Assert.Equal(new Vector2(pe, ce - t), inner[4]);
        Assert.Equal(new Vector2(trav, ce - t), inner[5]);
    }

    [Theory]
    [InlineData("canto-l-2p-esq-950", true)]
    [InlineData("canto-l-2p-dir-950", false)]
    public void CantoL_2p_GeraPecasIndependentes(string id, bool leftHand)
    {
        var instance = ModuleCatalog.CreateInstance(id, Vector3.Zero);
        Assert.NotNull(instance.CornerL);
        Assert.Equal(leftHand, instance.CornerL!.IsLeftHand);

        var labels = instance.Mesh.Faces
            .Select(f => f.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Base L", labels);
        Assert.Contains("Prateleira L", labels);
        Assert.Contains("Lateral esq.", labels);
        Assert.Contains("Lateral dir.", labels);
        Assert.Contains("Sarrafo traseiro dir.", labels);
        Assert.Contains("Sarrafo traseiro esq.", labels);
        Assert.Contains("Sarrafo dianteiro dir.", labels);
        Assert.Contains("Sarrafo dianteiro esq.", labels);
        Assert.Contains("Fundo dir.", labels);
        Assert.Contains("Fundo esq.", labels);
        Assert.Contains("Travessa canto dir.", labels);
        Assert.Contains("Travessa canto esq.", labels);
        Assert.Contains("Porta dir.", labels);
        Assert.Contains("Porta esq.", labels);
    }

    [Fact]
    public void Laterais_UsamSomenteProfundidadeDoProprioLado()
    {
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        var p = instance.CornerL!;
        float pd = p.ProfundidadeDireita;
        float pe = p.ProfundidadeEsquerda;
        float t = p.EspessuraMdf;
        float cd = p.ComprimentoDireito;
        float ce = p.ComprimentoEsquerdo;

        var latDir = FacesOf(instance, "Lateral dir.");
        var latEsq = FacesOf(instance, "Lateral esq.");

        float dirDepth = Extent(latDir, v => v.Z);
        float esqDepth = Extent(latEsq, v => v.X);
        float dirSpanX = Extent(latDir, v => v.X);
        float esqSpanZ = Extent(latEsq, v => v.Z);

        Assert.True(MathF.Abs(dirDepth - pd) < 1f, $"Lateral dir. profundidade={dirDepth}, esperado={pd}");
        Assert.True(MathF.Abs(esqDepth - pe) < 1f, $"Lateral esq. profundidade={esqDepth}, esperado={pe}");
        Assert.True(dirSpanX <= t + 1f, "Lateral dir. não deve ocupar o comprimento total");
        Assert.True(esqSpanZ <= t + 1f, "Lateral esq. não deve ocupar o comprimento total");
        Assert.True(MaxOf(latDir, v => v.X) > cd - t - 1f);
        Assert.True(MaxOf(latEsq, v => v.Z) > ce - t - 1f);
    }

    [Fact]
    public void Base_FicaEntreLaterais_SemSobrepor()
    {
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        float t = instance.CornerL!.EspessuraMdf;
        float cd = instance.CornerL.ComprimentoDireito;
        float ce = instance.CornerL.ComprimentoEsquerdo;

        var baseVerts = FacesOf(instance, "Base L").SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(baseVerts);

        Assert.True(baseVerts.Max(v => v.X) <= cd - t + 0.5f, "Base não pode invadir lateral direita");
        Assert.True(baseVerts.Max(v => v.Z) <= ce - t + 0.5f, "Base não pode invadir lateral esquerda");
        Assert.True(baseVerts.Min(v => v.X) >= t - 0.5f || baseVerts.Min(v => v.X) >= 5f,
            "Base deve descontar fundo esq.");
        Assert.True(baseVerts.Min(v => v.Z) >= 5f, "Base deve descontar fundo dir.");
    }

    [Fact]
    public void Sarrafos_UsamProfundidadesDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 670f;
        settings.CozinhaInferiorDepthMm = 580f;
        var box = settings.CozinhaInferiorBox;
        box.InferiorNumeric["sar-prof-fro"] = 90f;
        box.InferiorNumeric["sar-prof-tra"] = 55f;
        box.InferiorChoice["sar-tipo"] = "Ambos";

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var traDir = FacesOf(instance, "Sarrafo traseiro dir.");
        var froDir = FacesOf(instance, "Sarrafo dianteiro dir.");
        Assert.NotEmpty(traDir);
        Assert.NotEmpty(froDir);

        // Traseiro dir. horizontal: extensão Z ≈ sar-prof-tra (55)
        Assert.True(MathF.Abs(Extent(traDir, v => v.Z) - 55f) < 1.5f,
            $"Sarrafo traseiro dir. profundidade={Extent(traDir, v => v.Z)}, esperado≈55");

        // Contínuo dir. (mão direita): cobre vão direito + canto — profundidade em Z ≈ sFro (90)
        Assert.True(MathF.Abs(Extent(froDir, v => v.Z) - 90f) < 1.5f,
            $"Sarrafo dianteiro dir. (contínuo) profundidade={Extent(froDir, v => v.Z)}, esperado≈90");

        // Contínuo vai até a frente do fundo (recuo + esp. fundo; travessas não deslocam o fundo)
        float pe = instance.CornerL!.ProfundidadeEsquerda;
        float minX = froDir.SelectMany(f => f.Vertices).Min(v => v.X);
        Assert.True(minX < pe - 1f, "Dianteiro contínuo deve passar do vão das portas");
        float recess = settings.CozinhaInferiorBox.BackRecessMm;
        float fundoT = settings.CozinhaBackThicknessMm;
        float fundoFront = recess + fundoT;
        Assert.True(MathF.Abs(minX - fundoFront) < 2f || minX <= MathF.Max(fundoFront, recess + 18f) + 2f,
            $"Dianteiro contínuo deve ir de encontro ao fundo (minX={minX}, fundoFront≈{fundoFront})");

        // Encontro esq. começa na face do contínuo (min Z ≈ Pd)
        var froEsq = FacesOf(instance, "Sarrafo dianteiro esq.");
        float pd = instance.CornerL.ProfundidadeDireita;
        Assert.True(froEsq.SelectMany(f => f.Vertices).Min(v => v.Z) >= pd - 1f,
            "Dianteiro de encontro deve parar no contínuo");
    }

    [Fact]
    public void Fundo_RespeitaRecuoDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaBackThicknessMm = 6f;
        settings.CozinhaInferiorBox.BackRecessMm = 18f;
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var fundoDir = FacesOf(instance, "Fundo dir.");
        var fundoEsq = FacesOf(instance, "Fundo esq.");
        Assert.NotEmpty(fundoDir);
        Assert.NotEmpty(fundoEsq);

        // Default cl-tipo=Travessas: fundos no lado parede (recuo), travessas formam o L.
        float minZDir = fundoDir.SelectMany(f => f.Vertices).Min(v => v.Z);
        float minXEsq = fundoEsq.SelectMany(f => f.Vertices).Min(v => v.X);
        Assert.True(MathF.Abs(minZDir - 18f) < 1.5f,
            $"Fundo dir. no recuo da parede (minZ={minZDir}, esperado≈18)");
        Assert.True(MathF.Abs(minXEsq - 18f) < 1.5f,
            $"Fundo esq. no recuo da parede (minX={minXEsq}, esperado≈18)");

        // Contínuo encosta na frente do fundo (recuo + espessura = 18+6)
        var froDir = FacesOf(instance, "Sarrafo dianteiro dir.");
        float minXFro = froDir.SelectMany(f => f.Vertices).Min(v => v.X);
        Assert.True(MathF.Abs(minXFro - 24f) < 1.5f,
            $"Dianteiro contínuo até frente do fundo 18+6=24 (minX={minXFro})");
    }

    [Fact]
    public void Canto_Travessas_UsaLarguraProfundidadeEAvancoDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaBackThicknessMm = 6f;
        settings.CozinhaInferiorBox.BackRecessMm = 18f;
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo"] = "Travessas";
        settings.CozinhaInferiorBox.InferiorNumeric["cl-larg-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-prof-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-aftv"] = 8f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var travDir = FacesOf(instance, "Travessa canto dir.");
        var travEsq = FacesOf(instance, "Travessa canto esq.");
        Assert.NotEmpty(travDir);
        Assert.NotEmpty(travEsq);

        // Promob L pra frente: ESQ // lat.esq (dZ=18, dX=88) + DIR // lat.dir (dX=18, dZ=70).
        Assert.True(MathF.Abs(Extent(travEsq, v => v.X) - 88f) < 1.5f,
            $"Travessa esq. // lateral esq. comprimento X={Extent(travEsq, v => v.X)}");
        Assert.True(MathF.Abs(Extent(travEsq, v => v.Z) - 18f) < 1.5f,
            $"Travessa esq. espessura Z={Extent(travEsq, v => v.Z)} (// lat.esq)");
        Assert.True(MathF.Abs(Extent(travDir, v => v.Z) - 70f) < 1.5f,
            $"Travessa dir. // lateral dir. comprimento Z={Extent(travDir, v => v.Z)}");
        Assert.True(MathF.Abs(Extent(travDir, v => v.X) - 18f) < 1.5f,
            $"Travessa dir. espessura X={Extent(travDir, v => v.X)} (// lat.dir)");

        // Travessas alinhadas à traseira das laterais (min=0) e L no bordo interno (max=88).
        float minTrav = Math.Min(
            travDir.SelectMany(f => f.Vertices).Min(v => v.Z),
            travEsq.SelectMany(f => f.Vertices).Min(v => v.X));
        float maxX = travEsq.SelectMany(f => f.Vertices).Max(v => v.X);
        float maxZ = Math.Max(
            travEsq.SelectMany(f => f.Vertices).Max(v => v.Z),
            travDir.SelectMany(f => f.Vertices).Max(v => v.Z));
        Assert.True(MathF.Abs(minTrav) < 1.5f, $"Travessas na traseira das laterais (min={minTrav})");
        Assert.True(MathF.Abs(maxX - 88f) < 1.5f, $"Bordo interno X={maxX}");
        Assert.True(MathF.Abs(maxZ - 88f) < 1.5f, $"Bordo interno Z={maxZ}");

        // Fundo no recuo (minZ≈18) e avança 8 mm sobre o envelope (minX≈80).
        var fundoDir = FacesOf(instance, "Fundo dir.");
        float minXFundo = fundoDir.SelectMany(f => f.Vertices).Min(v => v.X);
        float minZFundo = fundoDir.SelectMany(f => f.Vertices).Min(v => v.Z);
        float minYFundo = fundoDir.SelectMany(f => f.Vertices).Min(v => v.Y);
        Assert.True(MathF.Abs(minXFundo - 80f) < 1.5f,
            $"Fundo dir. avança 8 mm sobre envelope (minX={minXFundo}, esperado≈80)");
        Assert.True(MathF.Abs(minZFundo - 18f) < 1.5f,
            $"Fundo dir. no recuo (minZ={minZFundo}, esperado≈18)");
        // fbf-afb=0 → fundo assenta sobre a base (y≈espessura).
        Assert.True(MathF.Abs(minYFundo - 18f) < 1.5f,
            $"Fundo dir. sobre a base (minY={minYFundo}, esperado≈18)");

        // Sarrafos atrás dos fundos (plano Z/X=0), após o envelope (88).
        var sarEsq = FacesOf(instance, "Sarrafo traseiro esq.");
        var sarDir = FacesOf(instance, "Sarrafo traseiro dir.");
        float minXDir = sarDir.SelectMany(f => f.Vertices).Min(v => v.X);
        float minZDir = sarDir.SelectMany(f => f.Vertices).Min(v => v.Z);
        float minZEsq = sarEsq.SelectMany(f => f.Vertices).Min(v => v.Z);
        float minXEsq = sarEsq.SelectMany(f => f.Vertices).Min(v => v.X);
        Assert.True(MathF.Abs(minXDir - 88f) < 1.5f,
            $"Sarrafo dir. inicia após envelope (minX={minXDir}, esperado≈88)");
        Assert.True(MathF.Abs(minZEsq - 88f) < 1.5f,
            $"Sarrafo esq. inicia após envelope (minZ={minZEsq}, esperado≈88)");
        Assert.True(MathF.Abs(minZDir) < 1.5f, $"Sarrafo dir. atrás do fundo (minZ={minZDir})");
        Assert.True(MathF.Abs(minXEsq) < 1.5f, $"Sarrafo esq. atrás do fundo (minX={minXEsq})");
    }

    [Theory]
    [InlineData("canto-l-2p-dir-950")]
    [InlineData("canto-l-2p-esq-950")]
    public void Portas_ProjetadasAFrenteDaCaixaria_EsqEDir(string catalogId)
    {
        var definition = ModuleCatalog.GetRequired(catalogId);
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-pa"] = 3f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-pb"] = 5f;
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-lat")] = "4";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-inf")] = "4";
        settings.CozinhaFrentesPortas.Choice[
            FrentesPortasConfiguratorService.MakeKey("inferiores", "borda-sup")] = "4";

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance(catalogId, Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var portaDir = FacesOf(instance, "Porta dir.").ToList();
        var portaEsq = FacesOf(instance, "Porta esq.").ToList();
        Assert.NotEmpty(portaDir);
        Assert.NotEmpty(portaEsq);

        var (ce, cd, pe, pd) = instance.CornerL!.EffectiveSides();
        float minZDir = portaDir.SelectMany(f => f.Vertices).Min(v => v.Z);
        float maxZDir = portaDir.SelectMany(f => f.Vertices).Max(v => v.Z);
        float minXEsq = portaEsq.SelectMany(f => f.Vertices).Min(v => v.X);
        float maxXEsq = portaEsq.SelectMany(f => f.Vertices).Max(v => v.X);

        // Portas fora da caixaria (frente das aberturas).
        Assert.True(minZDir >= pd - 0.5f, $"Porta dir. deve começar em Pd (minZ={minZDir}, Pd={pd})");
        Assert.True(maxZDir > pd + 10f, $"Porta dir. deve projetar à frente (maxZ={maxZDir})");
        Assert.True(minXEsq >= pe - 0.5f, $"Porta esq. deve começar em Pe (minX={minXEsq}, Pe={pe})");
        Assert.True(maxXEsq > pe + 10f, $"Porta esq. deve projetar à frente (maxX={maxXEsq})");

        // Folga interna A/B no canto.
        float minXDir = portaDir.SelectMany(f => f.Vertices).Min(v => v.X);
        float minZEsq = portaEsq.SelectMany(f => f.Vertices).Min(v => v.Z);
        Assert.True(MathF.Abs(minXDir - (pe + 3f)) < 1.5f,
            $"Folga A na porta dir. (minX={minXDir}, esperado≈{pe + 3f})");
        Assert.True(MathF.Abs(minZEsq - (pd + 5f)) < 1.5f,
            $"Folga B na porta esq. (minZ={minZEsq}, esperado≈{pd + 5f})");
    }

    [Fact]
    public void BaseEPrateleira_Inteiras_UsamPecaLContinuaSemEmenda()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-base"] = "Inteira";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-tampo"] = "Inteiro";

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        // Face poligonal L (6 vértices) = perímetro contínuo, sem aresta de emenda no LineLoop.
        Assert.Contains(FacesOf(instance, "Base L"), f => f.Vertices.Length == 6);
        Assert.Contains(FacesOf(instance, "Prateleira L"), f => f.Vertices.Length == 6);
    }

    [Fact]
    public void BaseEPrateleira_Recortadas_FicamBipartidasComEmenda()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-base"] = "Recortada";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-tampo"] = "Recortado";

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        Assert.DoesNotContain(FacesOf(instance, "Base L"), f => f.Vertices.Length == 6);
        Assert.DoesNotContain(FacesOf(instance, "Prateleira L"), f => f.Vertices.Length == 6);
        Assert.True(FacesOf(instance, "Base L").Count() >= 12,
            "Recortada deve gerar duas caixas (12 faces) com emenda.");
    }

    [Fact]
    public void Fundo_AplicaAvancoSobreLateralEBaseDoConfiguradorInferior()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaBackThicknessMm = 6f;
        settings.CozinhaInferiorBox.BackRecessMm = 18f;
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo"] = "Travessas";
        settings.CozinhaInferiorBox.InferiorNumeric["cl-larg-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-prof-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-aftv"] = 8f;
        // Mesmo local do configurador Inferior dos módulos retos.
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 9f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-afb"] = 7f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var fundoDir = FacesOf(instance, "Fundo dir.");
        float maxX = fundoDir.SelectMany(f => f.Vertices).Max(v => v.X);
        float minY = fundoDir.SelectMany(f => f.Vertices).Min(v => v.Y);
        float t = 18f;

        // afl=9 → fundo dir. avança sobre a lateral (maxX = Cd - t + 9).
        Assert.True(MathF.Abs(maxX - (instance.Width - t + 9f)) < 1.5f,
            $"Fundo dir. sobre lateral (maxX={maxX}, esperado≈{instance.Width - t + 9f})");
        Assert.True(MathF.Abs(minY - (t - 7f)) < 1.5f,
            $"Fundo dir. sobre base (minY={minY}, esperado≈{t - 7f})");
    }

    [Fact]
    public void Canto_TravessasInvertidas_TrocaPecaCheia()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaBackThicknessMm = 6f;
        settings.CozinhaInferiorBox.BackRecessMm = 18f;
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo"] = "Travessas invertidas";
        settings.CozinhaInferiorBox.InferiorNumeric["cl-larg-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-prof-trav"] = 88f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-aftv"] = 8f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var travDir = FacesOf(instance, "Travessa canto dir.");
        var travEsq = FacesOf(instance, "Travessa canto esq.");

        // Invertidas: DIR cheia (dX=18, dZ=88) + ESQ encosta (dX=70, dZ=18).
        Assert.True(MathF.Abs(Extent(travDir, v => v.Z) - 88f) < 1.5f,
            $"Invertidas: travessa dir. cheia comprimento Z={Extent(travDir, v => v.Z)}");
        Assert.True(MathF.Abs(Extent(travEsq, v => v.X) - 70f) < 1.5f,
            $"Invertidas: travessa esq. encosta comprimento X={Extent(travEsq, v => v.X)}");
        Assert.True(MathF.Abs(Extent(travDir, v => v.X) - 18f) < 1.5f,
            $"Invertidas: travessa dir. // lat.dir (dX={Extent(travDir, v => v.X)})");
        Assert.True(MathF.Abs(Extent(travEsq, v => v.Z) - 18f) < 1.5f,
            $"Invertidas: travessa esq. // lat.esq (dZ={Extent(travEsq, v => v.Z)})");
    }

    [Fact]
    public void Rebuild_PreservaPosicaoERotacao()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", new Vector3(1200f, 0f, 800f));
        instance.RotationYDegrees = 45f;
        var pos = instance.Position;
        var rot = instance.RotationYDegrees;

        CornerLModuleBuilder.Rebuild(instance, definition, instance.CornerL!);

        Assert.Equal(pos, instance.Position);
        Assert.Equal(rot, instance.RotationYDegrees);
        Assert.True(instance.Mesh.Vertices.Count > 40);
    }

    [Fact]
    public void Parametrico_AlteraComprimentos_RegeraMalha()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);

        instance.CornerL!.ComprimentoDireito = 1200f;
        instance.CornerL.ComprimentoEsquerdo = 1000f;
        CornerLModuleBuilder.Rebuild(instance, definition, instance.CornerL);

        Assert.Equal(1200f, instance.Width, 1);
        Assert.Equal(1000f, instance.Depth, 1);
        float maxX = instance.Mesh.Faces.SelectMany(f => f.Vertices).Max(v => v.X);
        Assert.True(maxX > 1100f, $"Envelope X deveria crescer (maxX={maxX})");
    }

    [Fact]
    public void Insercao_UsaAlturaEProfundidadeDoConfigurador()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 670f;
        settings.CozinhaInferiorDepthMm = 580f;

        var (width, height, depth) = DimensionConfiguratorService.ResolveInsertionDimensions(
            definition, settings);
        Assert.Equal(950f, width, 1);
        Assert.Equal(670f, height, 1);
        Assert.Equal(580f, depth, 1);

        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(width, height, depth, definition, settings, respectCatalogLimits: false);
        // Simula ApplyPlacement: segundo rebuild não pode sobrescrever Pe/Pd com o envelope.
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        Assert.NotNull(instance.CornerL);
        Assert.Equal(670f, instance.CornerL!.Altura, 1);
        Assert.Equal(580f, instance.CornerL.ProfundidadeDireita, 1);
        Assert.Equal(580f, instance.CornerL.ProfundidadeEsquerda, 1);
        Assert.Equal(950f, instance.CornerL.ComprimentoDireito, 1);
        Assert.Equal(950f, instance.CornerL.ComprimentoEsquerdo, 1);

        var latDir = FacesOf(instance, "Lateral dir.");
        Assert.True(MathF.Abs(Extent(latDir, v => v.Z) - 580f) < 1f,
            "Lateral dir. deve usar profundidade B do configurador");
    }

    [Fact]
    public void Painel_EditaEnvelope_PreservaMedidaAeB()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorHeightMm = 670f;
        settings.CozinhaInferiorDepthMm = 580f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);

        Assert.Equal(580f, instance.CornerL!.ProfundidadeDireita, 1);

        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition,
            width: 1000f, height: 700f, depth: 950f,
            settings,
            cornerMedidaA: 580f,
            cornerMedidaB: 580f,
            cornerLarguraA: 1000f,
            cornerLarguraB: 950f);

        Assert.Equal(580f, instance.CornerL!.ProfundidadeDireita, 1);
        Assert.Equal(580f, instance.CornerL.ProfundidadeEsquerda, 1);
        Assert.Equal(1000f, instance.CornerL.ComprimentoDireito, 1);
        Assert.Equal(950f, instance.CornerL.ComprimentoEsquerdo, 1);
        Assert.Equal(700f, instance.CornerL.Altura, 1);

        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition,
            instance.CornerL.ComprimentoDireito,
            instance.Height,
            instance.CornerL.ComprimentoEsquerdo,
            settings,
            cornerMedidaA: 520f,
            cornerMedidaB: 540f,
            cornerLarguraA: instance.CornerL.ComprimentoDireito,
            cornerLarguraB: instance.CornerL.ComprimentoEsquerdo);

        Assert.Equal(520f, instance.CornerL.ProfundidadeDireita, 1);
        Assert.Equal(540f, instance.CornerL.ProfundidadeEsquerda, 1);

        var latDir = FacesOf(instance, "Lateral dir.");
        Assert.True(MathF.Abs(Extent(latDir, v => v.Z) - 520f) < 1.5f,
            "Lateral dir. deve seguir Medida A");
    }

    [Fact]
    public void Painel_LarguraAeB_Independentes()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-dir-950", Vector3.Zero);
        instance.SetDimensions(950f, 850f, 580f, definition, settings, respectCatalogLimits: false);

        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition, 1500f, 850f, 950f, settings,
            580f, 580f, cornerLarguraA: 1500f, cornerLarguraB: 950f);

        Assert.Equal(1500f, instance.CornerL!.ComprimentoDireito, 1);
        Assert.Equal(950f, instance.CornerL.ComprimentoEsquerdo, 1);

        // Segunda edição: só Largura B — A deve permanecer 1500.
        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition, 1500f, 850f, 1200f, settings,
            580f, 580f, cornerLarguraA: 1500f, cornerLarguraB: 1200f);

        Assert.Equal(1500f, instance.CornerL.ComprimentoDireito, 1);
        Assert.Equal(1200f, instance.CornerL.ComprimentoEsquerdo, 1);
        Assert.Equal(1500f, instance.Width, 1);
        Assert.Equal(1200f, instance.Depth, 1);
    }

    [Fact]
    public void Painel_LarguraAeB_Independentes_LEsq()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 850f, 580f, definition, settings, respectCatalogLimits: false);

        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition, 1500f, 850f, 1100f, settings,
            580f, 580f, cornerLarguraA: 1500f, cornerLarguraB: 1100f);

        Assert.Equal(1500f, instance.CornerL!.ComprimentoDireito, 1);
        Assert.Equal(1100f, instance.CornerL.ComprimentoEsquerdo, 1);

        PropertyPanelInput.ApplyModuleDimensions(
            instance, definition, 1500f, 850f, 1300f, settings,
            580f, 580f, cornerLarguraA: 1500f, cornerLarguraB: 1300f);

        Assert.Equal(1500f, instance.CornerL.ComprimentoDireito, 1);
        Assert.Equal(1300f, instance.CornerL.ComprimentoEsquerdo, 1);
    }

    [Fact]
    public void ModuloReto_NaoRecebeCornerL()
    {
        var balcao = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        Assert.Null(balcao.CornerL);
    }

    private static IEnumerable<SelectableFace> FacesOf(ModuleInstance instance, string label) =>
        instance.Mesh.Faces.Where(f => string.Equals(f.Label, label, StringComparison.OrdinalIgnoreCase));

    private static float Extent(IEnumerable<SelectableFace> faces, Func<Vector3, float> axis)
    {
        var vals = faces.SelectMany(f => f.Vertices).Select(axis).ToList();
        return vals.Count == 0 ? 0f : vals.Max() - vals.Min();
    }

    private static float MaxOf(IEnumerable<SelectableFace> faces, Func<Vector3, float> axis) =>
        faces.SelectMany(f => f.Vertices).Select(axis).DefaultIfEmpty(0f).Max();
}
