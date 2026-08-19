using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class InferiorCornerConfiguratorTests
{
    [Fact]
    public void CantoReto_AvancoEntreFrentesEAfastamentosMudamGeometria()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorChoice["cr-tipo-ff"] = "Parcial Dupla";
        box.InferiorNumeric["cr-dim-ffp"] = 100f;
        box.InferiorNumeric["cr-affffp"] = 0f;
        box.InferiorNumeric["cr-afa-lat"] = 12f;
        box.InferiorNumeric["cr-afa-tra"] = 16f;

        var definition = ModuleCatalog.GetRequired("canto-cr-dir-950");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(950f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        var partialBase = module.Mesh.Faces.Where(f => f.Label == "Frente falsa parcial").SelectMany(f => f.Vertices).ToList();
        Assert.NotEmpty(partialBase);

        box.InferiorNumeric["cr-affffp"] = 18f;
        module.SetDimensions(950f, 850f, 550f, definition, settings, respectCatalogLimits: false);
        var partialAdvanced = module.Mesh.Faces.Where(f => f.Label == "Frente falsa parcial").SelectMany(f => f.Vertices).ToList();
        Assert.True(partialAdvanced.Min(v => v.X) < partialBase.Min(v => v.X) - 17f,
            "O avanço E deve levar a parcial 18 mm sobre a frente falsa inteira.");
        Assert.InRange(module.Mesh.Vertices.Min(v => v.Z), 15.5f, 16.5f);
    }

    [Fact]
    public void CantoL_DistanciadoresEAfastamentoSaoAplicados()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cl-prof-dist"] = 65f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-afa-lat"] = 10f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-afa-tra"] = 14f;

        var definition = ModuleCatalog.GetRequired("canto-l-2p-esq-950");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(950f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        Assert.Contains(module.Mesh.Faces, f => f.Label == "Distanciador canto L A");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Distanciador canto L B");
        Assert.InRange(module.Mesh.Vertices.Min(v => v.Z), 13.5f, 14.5f);
    }

    [Fact]
    public void Obliquo_UsaBasePrateleiraInteirasETravessasConfiguradas()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.CozinhaInferiorBox;
        box.InferiorChoice["cl-tipo"] = "Travessas invertidas";
        box.InferiorChoice["cl-tipo-base"] = "Única";
        box.InferiorChoice["cl-tipo-tampo"] = "Única";
        box.InferiorNumeric["cl-prof-dist"] = 55f;

        var definition = ModuleCatalog.GetRequired("canto-obliquo-1p-900");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(800f, 850f, 800f, definition, settings, respectCatalogLimits: false);

        Assert.Contains(module.Mesh.Faces, f => f.Label == "Base oblíqua inteira");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Prateleira oblíqua inteira");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Travessa canto dir.");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Travessa canto esq.");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Fundo dir.");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Fundo esq.");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Distanciador oblíquo A");

        var diagonal = module.Mesh.Faces
            .Where(f => f.Label == "Sarrafo frontal oblíquo")
            .SelectMany(f => f.Vertices)
            .ToList();
        Assert.NotEmpty(diagonal);
        Assert.InRange(diagonal.Max(v => v.X), 781.5f, 782.5f);
        Assert.InRange(diagonal.Max(v => v.Z), 781.5f, 782.5f);

        var wholeBaseHorizontalFaces = module.Mesh.Faces
            .Where(f => f.Label == "Base oblíqua inteira" && MathF.Abs(f.Normal.Y) > 0.9f)
            .ToList();
        var wholeShelfHorizontalFaces = module.Mesh.Faces
            .Where(f => f.Label == "Prateleira oblíqua inteira" && MathF.Abs(f.Normal.Y) > 0.9f)
            .ToList();
        Assert.Equal(2, wholeBaseHorizontalFaces.Count);
        Assert.Equal(2, wholeShelfHorizontalFaces.Count);

        var doors = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Porta", StringComparison.Ordinal))
            .SelectMany(f => f.Vertices)
            .ToList();
        Assert.NotEmpty(doors);
        float sarrafoFrontPlane = diagonal.Max(v => v.X + v.Z);
        float doorBackPlane = doors.Min(v => v.X + v.Z);
        float doorFrontPlane = doors.Max(v => v.X + v.Z);
        Assert.InRange(doorBackPlane - sarrafoFrontPlane, -0.1f, 0.1f);
        Assert.True(doorFrontPlane > sarrafoFrontPlane + 20f);

        var visibleDoorEdge = doors
            .Where(v => MathF.Abs((v.X + v.Z) - doorFrontPlane) < 0.1f)
            .Distinct()
            .ToList();
        float visibleDoorWidth = visibleDoorEdge
            .SelectMany(a => visibleDoorEdge.Select(b =>
                new Vector2(a.X - b.X, a.Z - b.Z).Length))
            .Max();
        float internalOpeningWidth = MathF.Sqrt(2f * 232f * 232f);
        Assert.InRange(visibleDoorWidth, internalOpeningWidth - 4.1f, internalOpeningWidth - 3.9f);
    }

    [Fact]
    public void Obliquo_UmModuloAlternaUmaEDuasPortasNoMesmoChanfro()
    {
        var definitions = ModuleCatalog.GetCozinhaCatalog()
            .Where(d => d.LibrarySubGroup == ModuleLibraryHierarchy.SubCantos)
            .ToList();
        Assert.Single(definitions, d => d.ShapeKind == ModuleShapeKind.Oblique);
        Assert.DoesNotContain(definitions, d => d.ShapeKind == ModuleShapeKind.CornerCurved);

        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-pa"] = 3f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-pb"] = 5f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-entre"] = 12f;
        var definition = ModuleCatalog.GetRequired("canto-obliquo-1p-900");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(900f, 850f, 900f, definition, settings, respectCatalogLimits: false);

        var singleDoor = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Porta", StringComparison.Ordinal))
            .SelectMany(f => f.Vertices)
            .ToList();
        Assert.NotEmpty(singleDoor);
        Assert.True(singleDoor.Max(v => v.X) - singleDoor.Min(v => v.X) > 250f);
        Assert.True(singleDoor.Max(v => v.Z) - singleDoor.Min(v => v.Z) > 250f);
        Assert.DoesNotContain(module.Mesh.Faces, f => f.Label == "Caixa");

        float SingleVisibleWidth()
        {
            var vertices = module.Mesh.Faces
                .Where(f => f.Label.StartsWith("Porta", StringComparison.Ordinal))
                .SelectMany(f => f.Vertices)
                .ToList();
            float frontPlane = vertices.Max(v => v.X + v.Z);
            var front = vertices.Where(v => MathF.Abs(v.X + v.Z - frontPlane) < 0.1f).ToList();
            return front.SelectMany(a => front.Select(b =>
                new Vector2(a.X - b.X, a.Z - b.Z).Length)).Max();
        }

        float oneDoorWidth = SingleVisibleWidth();
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-entre"] = -10f;
        module.RebuildMesh(definition, settings);
        Assert.InRange(SingleVisibleWidth() - oneDoorWidth, -0.1f, 0.1f);

        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-entre"] = 12f;
        module.ObliqueDoorCount = 2;
        module.RebuildMesh(definition, settings);
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Porta 1");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Porta 2");
        Assert.DoesNotContain(module.Mesh.Faces,
            f => f.Label.StartsWith("Porta —", StringComparison.Ordinal));

        List<Vector2> VisibleEdge(string label)
        {
            var vertices = module.Mesh.Faces.Where(f => f.Label == label)
                .SelectMany(f => f.Vertices).ToList();
            float frontPlane = vertices.Max(v => v.X + v.Z);
            return vertices
                .Where(v => MathF.Abs(v.X + v.Z - frontPlane) < 0.1f)
                .Select(v => new Vector2(v.X, v.Z))
                .Distinct()
                .ToList();
        }

        var door1 = VisibleEdge("Porta 1");
        var door2 = VisibleEdge("Porta 2");
        float centerGap = door1.SelectMany(a => door2.Select(b => (a - b).Length)).Min();
        Assert.InRange(centerGap, 11.9f, 12.1f);

        var cantoFields = BoxAssemblyInferiorSchema.FindNode("canto-l-canto")!.Fields;
        Assert.Equal(
            ["cl-folga-pa", "cl-folga-pb", "cl-folga-entre"],
            cantoFields.Skip(6).Take(3).Select(f => f.Key).ToArray());
        Assert.All(cantoFields.Skip(6).Take(3), field => Assert.True(field.AllowNegative));
    }

    [Fact]
    public void Catalogo_Cantos_OcultaLDeUmaPortaEGaveteiro()
    {
        var ids = ModuleCatalog.GetCozinhaCatalog().Select(d => d.Id).ToHashSet();

        Assert.DoesNotContain("canto-l-esq-950", ids);
        Assert.DoesNotContain("canto-l-dir-950", ids);
        Assert.DoesNotContain("canto-gav-3g-900", ids);
        Assert.Contains("canto-l-2p-esq-950", ids);
        Assert.DoesNotContain("canto-l-2p-dir-950", ids);
        Assert.Contains("canto-bifold-l-esq-950", ids);
        Assert.DoesNotContain("canto-bifold-l-dir-950", ids);
    }

    [Theory]
    [InlineData("canto-bifold-l-esq-950", "Porta esq. 1", "Porta esq. 2", "Porta dir.")]
    [InlineData("canto-bifold-l-dir-950", "Porta dir. 1", "Porta dir. 2", "Porta esq.")]
    public void CantoL3P_GeraTresPortasReaisEConservaOrientacao(
        string definitionId,
        string splitA,
        string splitB,
        string single)
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorNumeric["cl-folga-entre"] = 6f;
        var definition = ModuleCatalog.GetRequired(definitionId);
        var module = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);

        module.SetDimensions(1000f, 850f, 550f, definition, settings, respectCatalogLimits: false);

        Assert.Equal(3, definition.DoorCount);
        Assert.Equal(3, module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Porta", StringComparison.Ordinal))
            .Select(f => f.Label)
            .Distinct()
            .Count());
        Assert.Contains(module.Mesh.Faces, f => f.Label == splitA);
        Assert.Contains(module.Mesh.Faces, f => f.Label == splitB);
        Assert.Contains(module.Mesh.Faces, f => f.Label == single);
    }

    [Fact]
    public void CantoL2PE3P_UsamFolgasInternasIndependentesSemSobreporPortas()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var numeric = settings.CozinhaInferiorBox.InferiorNumeric;
        numeric["cl2-folga-pa"] = 7f;
        numeric["cl2-folga-pb"] = 9f;
        numeric["cl3-folga-pa"] = 17f;
        numeric["cl3-folga-pb"] = 19f;
        numeric["cl3-folga-entre"] = 11f;

        var definition2P = ModuleCatalog.GetRequired("canto-l-2p-dir-950");
        var canto2P = ModuleCatalog.CreateInstance(definition2P.Id, Vector3.Zero);
        canto2P.SetDimensions(950f, 850f, 550f, definition2P, settings, respectCatalogLimits: false);
        var (_, _, pe2, pd2) = canto2P.CornerL!.EffectiveSides();
        var porta2PDir = canto2P.Mesh.Faces.Where(f => f.Label == "Porta dir.")
            .SelectMany(f => f.Vertices).ToList();
        var porta2PEsq = canto2P.Mesh.Faces.Where(f => f.Label == "Porta esq.")
            .SelectMany(f => f.Vertices).ToList();
        Assert.InRange(porta2PDir.Min(v => v.X), pe2 + 25f - 0.1f, pe2 + 25f + 0.1f);
        Assert.InRange(porta2PEsq.Min(v => v.Z), pd2 + 9f - 0.1f, pd2 + 9f + 0.1f);

        var definition3P = ModuleCatalog.GetRequired("canto-bifold-l-dir-950");
        var canto3P = ModuleCatalog.CreateInstance(definition3P.Id, Vector3.Zero);
        canto3P.SetDimensions(950f, 850f, 550f, definition3P, settings, respectCatalogLimits: false);
        var (_, _, pe3, pd3) = canto3P.CornerL!.EffectiveSides();
        var porta3PDir1 = canto3P.Mesh.Faces.Where(f => f.Label == "Porta dir. 1")
            .SelectMany(f => f.Vertices).ToList();
        var porta3PDir2 = canto3P.Mesh.Faces.Where(f => f.Label == "Porta dir. 2")
            .SelectMany(f => f.Vertices).ToList();
        var porta3PEsq = canto3P.Mesh.Faces.Where(f => f.Label == "Porta esq.")
            .SelectMany(f => f.Vertices).ToList();
        Assert.InRange(porta3PDir1.Min(v => v.X), pe3 + 35f - 0.1f, pe3 + 35f + 0.1f);
        Assert.InRange(porta3PEsq.Min(v => v.Z), pd3 + 19f - 0.1f, pd3 + 19f + 0.1f);
        Assert.InRange(porta3PDir2.Min(v => v.X) - porta3PDir1.Max(v => v.X), 10.9f, 11.1f);

        var fields = BoxAssemblyInferiorSchema.FindNode("canto-l-canto")!.Fields;
        Assert.Contains(fields, f => f.Key == "cl2-folga-pa" && f.Group == "Portas — Canto L 2P");
        Assert.Contains(fields, f => f.Key == "cl3-folga-pa" && f.Group == "Portas — Canto L 3P");
    }

    [Theory]
    [InlineData("canto-l-2p-esq-950", true, 2)]
    [InlineData("canto-l-2p-dir-950", false, 2)]
    [InlineData("canto-bifold-l-esq-950", true, 3)]
    [InlineData("canto-bifold-l-dir-950", false, 3)]
    public void CantoL_EsqEDir_EspelhamQualPortaFicaNaFrente(
        string definitionId,
        bool leftHand,
        int doorCount)
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        string prefix = doorCount == 3 ? "cl3" : "cl2";
        settings.CozinhaInferiorBox.InferiorNumeric[$"{prefix}-folga-pa"] = 6f;
        settings.CozinhaInferiorBox.InferiorNumeric[$"{prefix}-folga-pb"] = 8f;

        var definition = ModuleCatalog.GetRequired(definitionId);
        var module = ModuleCatalog.CreateInstance(definitionId, Vector3.Zero);
        module.SetDimensions(950f, 850f, 550f, definition, settings, respectCatalogLimits: false);
        var (_, _, pe, pd) = module.CornerL!.EffectiveSides();
        var right = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Porta dir.", StringComparison.Ordinal))
            .SelectMany(f => f.Vertices).ToList();
        var left = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Porta esq.", StringComparison.Ordinal))
            .SelectMany(f => f.Vertices).ToList();

        float expectedRight = pe + 6f + (leftHand ? 0f : definition.FrontThickness);
        float expectedLeft = pd + 8f + (leftHand ? definition.FrontThickness : 0f);
        Assert.InRange(right.Min(v => v.X), expectedRight - 0.1f, expectedRight + 0.1f);
        Assert.InRange(left.Min(v => v.Z), expectedLeft - 0.1f, expectedLeft + 0.1f);
    }

    [Fact]
    public void Obliquo_BaseEPrateleiraBipartidasContinuamChanfradas()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-base"] = "Bipartida";
        settings.CozinhaInferiorBox.InferiorChoice["cl-tipo-tampo"] = "Bipartida";
        settings.CozinhaInferiorBox.InferiorNumeric["cl-prof-dist"] = 48f;
        settings.CozinhaInferiorBox.InferiorNumeric["cl-afa-tra"] = 12f;

        var definition = ModuleCatalog.GetRequired("canto-obliquo-1p-900");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(900f, 850f, 900f, definition, settings, respectCatalogLimits: false);

        Assert.Equal(ModuleShapeKind.Oblique, definition.ShapeKind);
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Base oblíqua A");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Prateleira oblíqua B");
        Assert.Contains(module.Mesh.Faces, f => f.Label == "Distanciador oblíquo A");
        var baseA = module.Mesh.Faces.Where(f => f.Label == "Base oblíqua A")
            .SelectMany(f => f.Vertices).ToList();
        Assert.True(baseA.Select(v => MathF.Round(v.X, 2)).Distinct().Count() >= 3);
        Assert.True(baseA.Select(v => MathF.Round(v.Z, 2)).Distinct().Count() >= 3);
    }
}
