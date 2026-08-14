using Xunit;

namespace Tracos3DStudio.Tests;

public class StatusBarPresenterTests
{
    [Fact]
    public void Build_closedRoom_includesWallAndModuleCounts()
    {
        var presentation = StatusBarPresenter.Build(new StatusBarInput
        {
            ProjectName = "fase-2-cozinha-L",
            ViewLabel = "Perspectiva",
            RoomClosed = true,
            WallCount = 4,
            ModuleCount = 4
        });

        Assert.Contains("Ambiente: Fechado (4 paredes)", presentation.ViewContext);
        Assert.Contains("4 módulos", presentation.ViewContext);
        Assert.Contains("Status: Pronto", presentation.Status);
    }

    [Fact]
    public void Build_selectionOverride_usesCustomContext()
    {
        var presentation = StatusBarPresenter.Build(new StatusBarInput
        {
            ViewLabel = "Frontal",
            ContextOverride = StatusBarPresenter.FormatSelection("Módulo", 800, "Balcão 2 Portas")
        });

        Assert.Contains("Módulo: Balcão 2 Portas — 800 mm", presentation.ViewContext);
        Assert.Contains("Vista: Frontal", presentation.ViewContext);
    }

    [Fact]
    public void Build_materialInfo_includesActiveMaterialAndMode()
    {
        var presentation = StatusBarPresenter.Build(new StatusBarInput
        {
            ActiveMaterialName = "MDF Branco",
            ApplicationMode = MaterialApplicationMode.WallBand
        });

        Assert.Equal("Material: MDF Branco   ·   Modo: Faixa", presentation.MaterialInfo);
    }

    [Fact]
    public void Build_collisionStatus_reportsModuleCount()
    {
        var presentation = StatusBarPresenter.Build(new StatusBarInput
        {
            CollisionEnabled = true,
            CollidingModuleCount = 2
        });

        Assert.Equal("Status: Colisão (2 módulos)", presentation.Status);
    }

    [Fact]
    public void Build_fullText_joinsNonEmptySegments()
    {
        var presentation = StatusBarPresenter.Build(new StatusBarInput
        {
            ProjectName = "demo",
            ViewLabel = "Planta",
            ContextOverride = "Face: Nenhuma",
            ActiveMaterialName = "Cerâmica Bege",
            ApplicationMode = MaterialApplicationMode.Auto,
            HintOverride = "Copiar material: clique na origem"
        });

        Assert.Contains("Projeto: demo", presentation.FullText);
        Assert.Contains("Material: Cerâmica Bege", presentation.FullText);
        Assert.Contains("Copiar material: clique na origem", presentation.FullText);
        Assert.Contains("Status: Pronto", presentation.FullText);
    }
}
