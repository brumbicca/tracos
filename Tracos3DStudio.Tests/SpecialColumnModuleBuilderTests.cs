using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class SpecialColumnModuleBuilderTests
{
    [Fact]
    public void Catalogo_Especiais_PossuiTres1PEtres2PComMedidasCorretas()
    {
        string[] oneDoor = ["esp-1p-col-esq-400", "esp-1p-col-central-400", "esp-1p-col-dir-400"];
        string[] twoDoors = ["esp-2p-col-esq-800", "esp-2p-col-central-800", "esp-2p-col-dir-800"];

        Assert.All(oneDoor, id =>
        {
            var definition = ModuleCatalog.GetRequired(id);
            Assert.Equal(400f, definition.DefaultWidth);
            Assert.Equal(1, definition.DoorCount);
        });
        Assert.All(twoDoors, id =>
        {
            var definition = ModuleCatalog.GetRequired(id);
            Assert.Equal(800f, definition.DefaultWidth);
            Assert.Equal(2, definition.DoorCount);
        });
    }

    [Theory]
    [InlineData("esp-1p-col-esq-400", 1)]
    [InlineData("esp-2p-col-central-800", 2)]
    public void Engenharia_GeraPortasERecorteRealNaBase(string id, int doors)
    {
        var module = ModuleCatalog.CreateInstance(id, Vector3.Zero);

        Assert.Equal(doors, module.Mesh.Faces
            .Where(f => f.Kind == FaceKind.ModuleFront && f.Label.StartsWith("Porta", StringComparison.Ordinal))
            .Select(f => f.Label).Distinct().Count());

        var baseTop = module.Mesh.Faces.First(f =>
            f.Label == "Base inferior" && f.Kind == FaceKind.ModuleTop && f.Vertices.Length > 4);
        Assert.Contains(baseTop.Vertices, v => MathF.Abs(v.Z - module.SpecialColumn!.DepthMm) < .1f);
    }

    [Fact]
    public void ColunaEsquerda_RecuaSomenteALateralEsquerda()
    {
        var module = ModuleCatalog.CreateInstance("esp-1p-col-esq-400", Vector3.Zero);
        float leftMinZ = module.Mesh.Faces.Where(f => f.Label == "Lateral esq.")
            .SelectMany(f => f.Vertices).Min(v => v.Z);
        float rightMinZ = module.Mesh.Faces.Where(f => f.Label == "Lateral dir.")
            .SelectMany(f => f.Vertices).Min(v => v.Z);

        Assert.Equal(200f, leftMinZ, 1);
        Assert.Equal(0f, rightMinZ, 1);
    }

    [Theory]
    [InlineData("esp-1p-col-esq-400", 2)]
    [InlineData("esp-1p-col-dir-400", 2)]
    [InlineData("esp-1p-col-central-400", 3)]
    [InlineData("esp-2p-col-esq-800", 2)]
    [InlineData("esp-2p-col-dir-800", 2)]
    [InlineData("esp-2p-col-central-800", 3)]
    public void TravessasTraseiras_FechamContornoDaColuna(string id, int expectedCount)
    {
        var module = ModuleCatalog.CreateInstance(id, Vector3.Zero);
        var labels = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Travessa traseira ", StringComparison.Ordinal) &&
                        f.Label.EndsWith("da coluna", StringComparison.Ordinal))
            .Select(f => f.Label)
            .Distinct()
            .ToList();

        Assert.Equal(expectedCount, labels.Count);
        Assert.Contains("Travessa traseira frontal da coluna", labels);
    }

    [Fact]
    public void Configurador_AplicaSarrafoVerticalAvancoFundoEPrateleiraInterna()
    {
        var module = ModuleCatalog.CreateInstance("esp-2p-col-central-800", Vector3.Zero);
        var settings = CreateRearSarrafoSettings();
        module.RebuildMesh(ModuleCatalog.GetRequired(module.DefinitionId), settings);
        var column = module.SpecialColumn!;
        float start = column.LeftOffsetMm;
        float end = start + column.WidthMm;

        var backs = module.Mesh.Faces
            .Where(f => f.Label is "Fundo 1" or "Fundo 2")
            .GroupBy(f => f.Label)
            .Select(g => g.SelectMany(f => f.Vertices).ToList())
            .ToList();
        Assert.Contains(backs, vertices => MathF.Abs(vertices.Max(v => v.X) - (start - 18f + 8f)) < .1f);
        Assert.Contains(backs, vertices => MathF.Abs(vertices.Min(v => v.X) - (end + 18f - 8f)) < .1f);

        var rearRail = module.Mesh.Faces.Where(f => f.Label == "Travessa traseira frontal da coluna")
            .SelectMany(f => f.Vertices).ToList();
        Assert.Equal(18f, rearRail.Max(v => v.Z) - rearRail.Min(v => v.Z), 1);
        Assert.Equal(module.Height - 18f, rearRail.Max(v => v.Y) - rearRail.Min(v => v.Y), 1);

        var shelfTop = module.Mesh.Faces.First(f =>
            f.Label == "Prateleira" && f.Kind == FaceKind.ModuleTop && f.Vertices.Length > 4);
        Assert.Contains(shelfTop.Vertices, v => MathF.Abs(v.Z - (column.DepthMm + 18f)) < .1f);

        var frontRail = module.Mesh.Faces.Where(f => f.Label == "Sarrafo dianteiro")
            .SelectMany(f => f.Vertices).ToList();
        Assert.Equal(18f, frontRail.Min(v => v.X), 1);
        Assert.Equal(module.Width - 18f, frontRail.Max(v => v.X), 1);
    }

    [Theory]
    [InlineData("esp-1p-col-esq-400")]
    [InlineData("esp-1p-col-dir-400")]
    public void Alinhamentos_EspelhamTravessasESarrafoTraseiro(string id)
    {
        var module = ModuleCatalog.CreateInstance(id, Vector3.Zero);
        module.RebuildMesh(ModuleCatalog.GetRequired(id), CreateRearSarrafoSettings());
        var column = module.SpecialColumn!;
        float start = column.LeftOffsetMm;
        float end = start + column.WidthMm;

        var frontRail = module.Mesh.Faces
            .Where(f => f.Label == "Travessa traseira frontal da coluna")
            .SelectMany(f => f.Vertices).ToList();
        var rearSarrafo = module.Mesh.Faces
            .Where(f => f.Label == "Sarrafo traseiro")
            .SelectMany(f => f.Vertices).ToList();

        if (column.Position == SpecialColumnPosition.Left)
        {
            Assert.Equal(18f, frontRail.Min(v => v.X), 1);
            Assert.Equal(end + 18f, frontRail.Max(v => v.X), 1);
            Assert.Equal(end + 18f, rearSarrafo.Min(v => v.X), 1);
        }
        else
        {
            Assert.Equal(start - 18f, frontRail.Min(v => v.X), 1);
            Assert.Equal(module.Width - 18f, frontRail.Max(v => v.X), 1);
            Assert.Equal(start - 18f, rearSarrafo.Max(v => v.X), 1);
        }
    }

    [Fact]
    public void BaseTemProfundidadeTotalETravessasApoiamSobreEla()
    {
        var module = ModuleCatalog.CreateInstance("esp-2p-col-central-800", Vector3.Zero);
        var baseFaces = module.Mesh.Faces.Where(f => f.Label == "Base inferior")
            .SelectMany(f => f.Vertices).ToList();
        Assert.Equal(0f, baseFaces.Min(v => v.Z), 1);
        Assert.Equal(module.Depth, baseFaces.Max(v => v.Z), 1);

        var rails = module.Mesh.Faces
            .Where(f => f.Label.StartsWith("Travessa traseira ", StringComparison.Ordinal) &&
                        f.Label.EndsWith("da coluna", StringComparison.Ordinal))
            .SelectMany(f => f.Vertices).ToList();
        Assert.Equal(18f, rails.Min(v => v.Y), 1);
    }

    [Fact]
    public void Parametros_AlteracaoERoundTrip_SaoPreservados()
    {
        var project = new Project();
        var module = ModuleCatalog.CreateInstance("esp-2p-col-central-800", Vector3.Zero);
        module.SpecialColumn!.WidthMm = 180f;
        module.SpecialColumn.DepthMm = 160f;
        module.SpecialColumn.LeftOffsetMm = 270f;
        module.SpecialColumn.ShelfNotched = false;
        module.RebuildMesh(ModuleCatalog.GetRequired(module.DefinitionId));
        project.Modules.Add(module);

        var restored = ProjectPersistence.LoadProject(ProjectPersistence.CreateFromProject(project)).Modules.Single();
        Assert.NotNull(restored.SpecialColumn);
        Assert.Equal(180f, restored.SpecialColumn!.WidthMm);
        Assert.Equal(160f, restored.SpecialColumn.DepthMm);
        Assert.Equal(270f, restored.SpecialColumn.LeftOffsetMm);
        Assert.False(restored.SpecialColumn.ShelfNotched);
    }

    private static DimensionConfiguratorSettings CreateRearSarrafoSettings()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["sar-tipo"] = "Ambos";
        settings.CozinhaInferiorBox.InferiorChoice["sar-sent-tra"] = "Vertical";
        settings.CozinhaInferiorBox.InferiorNumeric["sar-prof-tra"] = 70f;
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 8f;
        return settings;
    }
}
