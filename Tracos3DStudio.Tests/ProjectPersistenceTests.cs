using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public class ProjectPersistenceTests
{
    [Fact]
    public void RoundTrip_AmbienteComPortaEJanela_PreservaGeometria()
    {
        var room = new Room();
        var wall1 = new WallSegment(new Vector2(0, 0), new Vector2(5000, 0), 150, 2600, WallOrientation.Right);
        wall1.Openings.Add(WallOpening.Door(500, 800, 2100));
        var wall2 = new WallSegment(new Vector2(5000, 0), new Vector2(5000, 3000), 150, 2600, WallOrientation.Right);
        wall2.Openings.Add(WallOpening.Window(400, 1200, 1000, 1100));
        var wall3 = new WallSegment(new Vector2(5000, 3000), new Vector2(0, 3000), 150, 2600, WallOrientation.Right);
        var wall4 = new WallSegment(new Vector2(0, 3000), new Vector2(0, 0), 150, 2600, WallOrientation.Right);

        room.SetWalls([wall1, wall2, wall3, wall4]);

        var document = ProjectPersistence.CreateFromRoom(room);
        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");

        try
        {
            ProjectPersistence.SaveToFile(document, path);
            var loaded = ProjectPersistence.LoadFromFile(path);
            var restored = ProjectPersistence.LoadRoom(loaded);

            Assert.True(restored.IsClosed);
            Assert.Equal(4, restored.Walls.Count);
            Assert.Equal(2, restored.Walls[0].Openings.Count + restored.Walls[1].Openings.Count);
            Assert.Equal(OpeningType.Door, restored.Walls[0].Openings[0].Type);
            Assert.Equal(800f, restored.Walls[0].Openings[0].Width);
            Assert.Equal(OpeningType.Window, restored.Walls[1].Openings[0].Type);
            Assert.Equal(1100f, restored.Walls[1].Openings[0].SillHeight);
            Assert.NotNull(restored.Floor);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_SchemaFuturo_LancaErro()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");
        File.WriteAllText(path, """{"schemaVersion":99,"metadata":{"name":"x"},"walls":[]}""");

        try
        {
            var ex = Assert.Throws<InvalidDataException>(() => ProjectPersistence.LoadFromFile(path));
            Assert.Contains("não é suportado", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_AmbienteComModulos_PreservaInstanciasNoArquivo()
    {
        var project = BuildClosedRoomProject();
        var moduleId = Guid.NewGuid();

        var counter = project.AddModule("balcao-2-portas", new Vector3(1100, 0, 75));
        counter.Id = moduleId;
        var counterDef = ModuleCatalog.GetRequired("balcao-2-portas");
        counter.SetDimensions(1000f, 900f, 600f, counterDef);
        counter.RotationYDegrees = 90f;

        var aerial = project.AddModule("aereo", new Vector3(400, 1400, 200));
        aerial.RotationYDegrees = 0f;

        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");

        try
        {
            var document = ProjectPersistence.CreateFromProject(project);
            Assert.Equal(2, document.Modules.Count);

            ProjectPersistence.SaveToFile(document, path);

            var json = File.ReadAllText(path);
            Assert.Contains("\"modules\"", json, StringComparison.Ordinal);

            var loaded = ProjectPersistence.LoadFromFile(path);
            var restored = ProjectPersistence.LoadProject(loaded);

            Assert.True(restored.Room.IsClosed);
            Assert.Equal(2, restored.Modules.Count);

            var restoredCounter = restored.Modules.Single(m => m.Id == moduleId);
            Assert.Equal("balcao-2-portas", restoredCounter.DefinitionId);
            Assert.Equal(1000f, restoredCounter.Width);
            Assert.Equal(900f, restoredCounter.Height);
            Assert.Equal(600f, restoredCounter.Depth);
            Assert.Equal(1100f, restoredCounter.Position.X);
            Assert.Equal(75f, restoredCounter.Position.Z);
            Assert.Equal(90f, restoredCounter.RotationYDegrees);
            Assert.True(restoredCounter.Mesh.Vertices.Count > 0);

            var restoredAerial = restored.Modules.Single(m => m.DefinitionId == "aereo");
            Assert.Equal(1400f, restoredAerial.Position.Y);
            Assert.Contains(restoredAerial.Mesh.Faces, f => f.Kind == FaceKind.ModuleFront);

            var reimported = new Project();
            reimported.ImportFrom(restored);
            Assert.Equal(restoredCounter.Width, reimported.Modules.Single(m => m.Id == moduleId).Width);
            Assert.NotSame(restored.Modules[0], reimported.Modules[0]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_MaterialECliente_PreservaCampos()
    {
        var project = BuildClosedRoomProject();
        project.Metadata.ClientName = "João";
        project.Metadata.ClientPhone = "11999990000";
        project.Metadata.WorkName = "Cozinha apto 402";
        project.Metadata.EnvironmentName = "Cozinha — Ambiente principal";
        project.Metadata.ClientCode = "CLI-001";
        project.Metadata.ClientCustomerType = ClientCustomerType.LegalEntity;
        project.Metadata.ClientTaxId = "12.345.678/0001-99";
        project.Metadata.ClientAddressNumber = "120";
        project.Metadata.ClientAddressComplement = "Apto 402";
        project.Metadata.ClientNotes = "Entrega após alvenaria";
        var module = project.AddModule("gaveteiro", new Vector3(500, 0, 300));
        module.MaterialId = "mdf-madeirado";

        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");

        try
        {
            var document = ProjectPersistence.CreateFromProject(project);
            ProjectPersistence.SaveToFile(document, path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal("João", restored.Metadata.ClientName);
            Assert.Equal("11999990000", restored.Metadata.ClientPhone);
            Assert.Equal("Cozinha apto 402", restored.Metadata.WorkName);
            Assert.Equal("Cozinha — Ambiente principal", restored.Metadata.EnvironmentName);
            Assert.Equal("CLI-001", restored.Metadata.ClientCode);
            Assert.Equal(ClientCustomerType.LegalEntity, restored.Metadata.ClientCustomerType);
            Assert.Equal("12.345.678/0001-99", restored.Metadata.ClientTaxId);
            Assert.Equal("120", restored.Metadata.ClientAddressNumber);
            Assert.Equal("Apto 402", restored.Metadata.ClientAddressComplement);
            Assert.Equal("Entrega após alvenaria", restored.Metadata.ClientNotes);
            Assert.Equal("mdf-madeirado", restored.Modules[0].MaterialId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_EspessuraPainel_PreservaValor()
    {
        var project = BuildClosedRoomProject();
        project.Metadata.PanelThicknessMm = 25f;
        project.Metadata.BackThicknessMm = 6f;

        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.tracos");

        try
        {
            var document = ProjectPersistence.CreateFromProject(project);
            ProjectPersistence.SaveToFile(document, path);
            var restored = ProjectPersistence.LoadProject(ProjectPersistence.LoadFromFile(path));

            Assert.Equal(25f, restored.Metadata.PanelThicknessMm);
            Assert.Equal(6f, restored.Metadata.BackThicknessMm);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void LoadProject_ModuloDesconhecido_LancaErro()
    {
        var document = new ProjectDocument
        {
            Modules =
            {
                new ModuleInstanceData
                {
                    DefinitionId = "modulo-inexistente",
                    Width = 800,
                    Height = 850,
                    Depth = 550
                }
            }
        };

        var ex = Assert.Throws<InvalidDataException>(() => ProjectPersistence.LoadProject(document));
        Assert.Contains("modulo-inexistente", ex.Message);
    }

    private static Project BuildClosedRoomProject()
    {
        var project = new Project();
        project.Room.SetWalls([
            new WallSegment(new Vector2(0, 0), new Vector2(3000, 0), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3000, 0), new Vector2(3000, 2000), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(3000, 2000), new Vector2(0, 2000), 150, 2600, WallOrientation.Right),
            new WallSegment(new Vector2(0, 2000), new Vector2(0, 0), 150, 2600, WallOrientation.Right)
        ]);

        return project;
    }
}
