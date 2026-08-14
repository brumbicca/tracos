using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class CornerLPartOverrideTests
{
    [Fact]
    public void PortaEsq_SetaProfundidade_AplicaOverrideNoRebuild()
    {
        var definition = ModuleCatalog.GetRequired("canto-l-2p-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("canto-l-2p-esq-950", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        Assert.True(ModulePartDimensionService.TryComputeLocalDimensions(instance, "Porta esq.", out var before));
        float zBefore = before.Z;

        var handle = new PartHandle(PartHandleAxis.Depth, true);
        Assert.True(ModulePartEditService.TryApplyFaceOffset(instance, "Porta esq.", handle, 40f, out _));

        // Rebuild com settings (como MainWindow) — override deve permanecer.
        CornerLModuleBuilder.Rebuild(instance, definition, instance.CornerL!, settings);

        Assert.True(ModulePartDimensionService.TryComputeLocalDimensions(instance, "Porta esq.", out var after));
        Assert.True(after.Z > zBefore + 30f,
            $"Porta esq. deveria crescer no vão Z (antes={zBefore}, depois={after.Z})");
    }
}
