using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class EndTerminalModuleBuilderTests
{
    [Fact]
    public void Catalogo_DiagonaisPossuiSomenteDiagonalEChanfradoEspelhadosPorAtalho()
    {
        var definitions = ModuleCatalog.GetCozinhaCatalog()
            .Where(item => item.LibrarySubGroup == ModuleLibraryHierarchy.SubDiagonais)
            .OrderBy(item => item.CatalogOrder)
            .ToList();

        Assert.Equal(["diag-300", "diag-chanf-300"], definitions.Select(item => item.Id));
        Assert.All(definitions, item => Assert.Equal(1, item.DoorCount));
    }

    [Fact]
    public void Diagonal_MedidaAControlaProfundidadeDaLateralCurta_EEspelhoTrocaDescricao()
    {
        var definition = ModuleCatalog.GetRequired("diag-300");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(420f, 850f, 580f, definition, respectCatalogLimits: false);
        module.EndTerminal!.SmallSideDepthMm = 230f;
        module.RebuildMesh(definition);

        var shortSide = module.Mesh.Faces.Where(face => face.Label == "Lateral curta direita")
            .SelectMany(face => face.Vertices).ToList();
        Assert.NotEmpty(shortSide);
        Assert.InRange(shortSide.Max(point => point.Z), 229.9f, 230.1f);
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Travessa frontal diagonal");
        Assert.DoesNotContain(module.Mesh.Faces, face => face.Label == "Travessa frontal reta");
        var baseVertices = module.Mesh.Faces.Where(face => face.Label == "Base inferior")
            .SelectMany(face => face.Vertices).ToList();
        Assert.InRange(baseVertices.Min(point => point.X), 17.9f, 18.1f);
        Assert.InRange(baseVertices.Max(point => point.X), 401.9f, 402.1f);
        var shelf = module.Mesh.Faces.Where(face => face.Label == "Prateleira")
            .SelectMany(face => face.Vertices).ToList();
        Assert.NotEmpty(shelf);
        Assert.All(shelf, point => Assert.True(float.IsFinite(point.X) && float.IsFinite(point.Z)));

        module.IsMirrored = true;
        module.RebuildMesh(definition);
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Lateral curta esquerda");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Lateral longa direita");
    }

    [Fact]
    public void Chanfrado_MedidasAEBMantemEncontroUnicoDasTravessasEPortasIndividuais()
    {
        var definition = ModuleCatalog.GetRequired("diag-chanf-300");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(500f, 850f, 600f, definition, respectCatalogLimits: false);
        module.EndTerminal!.SmallSideDepthMm = 220f;
        module.EndTerminal.FrontStraightWidthMm = 180f;
        module.EndTerminal.DoorCount = 2;
        module.RebuildMesh(definition);

        Assert.Contains(module.Mesh.Faces, face => face.Label == "Porta frontal 1");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Porta frontal 2");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Travessa frontal reta");
        Assert.Contains(module.Mesh.Faces, face => face.Label == "Travessa frontal diagonal");

        var straight = module.Mesh.Faces.Where(face => face.Label == "Travessa frontal reta")
            .SelectMany(face => face.Vertices).ToList();
        var diagonal = module.Mesh.Faces.Where(face => face.Label == "Travessa frontal diagonal")
            .SelectMany(face => face.Vertices).ToList();
        Assert.True(straight.Any(a => diagonal.Any(b => Vector3.DistanceSquared(a, b) < .01f)),
            "As travessas reta e diagonal devem compartilhar o mesmo vértice paramétrico.");
    }

    [Fact]
    public void Chanfrado_TravessasParametricasEPortasRespeitamLateraisInternasEFolgaLateral()
    {
        var definition = ModuleCatalog.GetRequired("diag-chanf-300");
        var module = ModuleCatalog.CreateInstance(definition.Id, Vector3.Zero);
        module.SetDimensions(500f, 850f, 600f, definition, respectCatalogLimits: false);
        module.EndTerminal!.SmallSideDepthMm = 220f;
        module.EndTerminal.FrontStraightWidthMm = 180f;
        module.EndTerminal.DoorCount = 1;
        module.RebuildMesh(definition);

        var railVertices = module.Mesh.Faces
            .Where(face => face.Label is "Travessa frontal reta" or "Travessa frontal diagonal")
            .SelectMany(face => face.Vertices)
            .ToList();
        Assert.InRange(railVertices.Min(point => point.X), 17.9f, 18.1f);
        Assert.InRange(railVertices.Max(point => point.X), 481.9f, 482.1f);
        // A ponta interna direita da travessa deve alcançar a mesma face interna
        // da lateral curta. Duas cotas Z nesse plano comprovam que existe a face
        // de topo da ponta, e não apenas o vértice externo solto.
        var rightCapDepths = railVertices
            .Where(point => MathF.Abs(point.X - 482f) < .1f)
            .Select(point => point.Z)
            .DistinctBy(value => MathF.Round(value, 2))
            .ToList();
        Assert.True(rightCapDepths.Max() - rightCapDepths.Min() > 10f);

        // O MDF da porta começa depois da folga externa de 2 mm, medida a
        // partir da face interna da lateral esquerda (18 + 2 = 20 mm).
        var doorVertices = module.Mesh.Faces.Where(face => face.Label == "Porta frontal")
            .SelectMany(face => face.Vertices)
            .ToList();
        Assert.NotEmpty(doorVertices);
        Assert.True(doorVertices.Min(point => point.X) >= 19.9f);
    }

}
