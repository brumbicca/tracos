using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ModuleTests
{
    [Fact]
    public void ModuleCatalog_ContemModulosBuiltInComHierarquiaCozinha()
    {
        Assert.True(ModuleCatalog.BuiltIn.Count >= 90);
        Assert.True(ModuleCatalog.BuiltIn.Count(m => m.Category == ModuleCategory.Cozinha) >= 80);
        Assert.Equal(3, ModuleCatalog.BuiltIn.Count(m => m.Category == ModuleCategory.Paineis));
        Assert.True(ModuleCatalog.TryGet("balcao-2-portas", out _));
        Assert.True(ModuleCatalog.TryGet("gav-4g-curvo-400", out _));
        Assert.Equal("2P 800mm", ModuleCatalog.GetRequired("balcao-2-portas").DisplayName);
    }

    [Fact]
    public void ModuleInstance_SetDimensions_RespeitaLimites()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);

        instance.SetDimensions(2000f, 500f, 300f, definition, respectCatalogLimits: true);

        Assert.Equal(1200f, instance.Width);
        Assert.Equal(700f, instance.Height);
        Assert.Equal(450f, instance.Depth);
    }

    [Fact]
    public void ModuleInstance_SetDimensions_LivreNoPainel_Aceita670Altura()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        var settings = DimensionConfiguratorSettings.CreateDefault();

        instance.SetDimensions(800f, 670f, 550f, definition, settings, respectCatalogLimits: false);

        Assert.Equal(800f, instance.Width);
        Assert.Equal(670f, instance.Height);
        Assert.Equal(550f, instance.Depth);
    }

    [Fact]
    public void ModuleMeshBuilder_GeraMalhaComFrentes()
    {
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", new Vector3(100, 0, 200));

        Assert.True(instance.Mesh.Vertices.Count > 0);
        Assert.True(instance.Mesh.Indices.Count > 0);
        Assert.Contains(instance.Mesh.Faces, f => f.Kind == FaceKind.ModuleFront);
    }

    [Fact]
    public void Project_AddModule_RegistraInstancia()
    {
        var project = new Project();
        var module = project.AddModule("gaveteiro", new Vector3(0, 0, 0));

        Assert.Single(project.Modules);
        Assert.Equal("gaveteiro", module.DefinitionId);
        Assert.Equal(4, ModuleCatalog.GetRequired("gaveteiro").DrawerCount);
    }

    [Fact]
    public void SetDimensions_AtualizaMalha()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        float initialMaxX = instance.Mesh.Vertices.Max(v => v.X);

        instance.SetDimensions(1000f, 900f, 600f, definition);

        Assert.Equal(1000f, instance.Width);
        Assert.True(instance.Mesh.Vertices.Max(v => v.X) > initialMaxX);
    }

    [Fact]
    public void RoundTrip_ProjetoComModulo_PreservaDados()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3000, 0), 150, 2600, WallOrientation.Right)
        ]);

        var module = project.AddModule("aereo", new Vector3(500, 1400, 0));
        module.RotationYDegrees = 90f;

        var document = ProjectPersistence.CreateFromProject(project);
        var restored = ProjectPersistence.LoadProject(document);

        Assert.Single(restored.Modules);
        Assert.Equal(800f, restored.Modules[0].Width);
        Assert.Equal(720f, restored.Modules[0].Height);
        Assert.Equal(350f, restored.Modules[0].Depth);
        Assert.Equal(500f, restored.Modules[0].Position.X);
        Assert.Equal(1400f, restored.Modules[0].Position.Y);
        Assert.Equal(90f, restored.Modules[0].RotationYDegrees);
        Assert.True(restored.Modules[0].Mesh.Vertices.Count > 0);
    }

    [Fact]
    public void ImportFrom_ClonaModulos_IndependenteDoOrigem()
    {
        var source = new Project();
        var original = source.AddModule("gaveteiro", new Vector3(100, 0, 50));
        original.RotationYDegrees = 180f;

        var target = new Project();
        target.ImportFrom(source);

        Assert.NotSame(original, target.Modules[0]);
        Assert.Equal(original.Id, target.Modules[0].Id);
        Assert.Equal(180f, target.Modules[0].RotationYDegrees);

        target.Modules[0].Position = new Vector3(999, 0, 0);
        Assert.Equal(100f, original.Position.X);
    }
}
