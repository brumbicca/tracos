using OpenTK.Mathematics;
using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class ModuleMeshBuilderBackAdvanceTests
{
    [Fact]
    public void BalcaoReto_AplicaAvancoFundoSobreLateralEBase()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 8f;
        settings.CozinhaInferiorBox.InferiorNumeric["ffl-afl"] = 9f;
        settings.CozinhaInferiorBox.InferiorNumeric["fbf-afb"] = 7f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);
        instance.ApplyPlacement(instance.Position, 0f, definition, dimensionSettings: settings);

        var fundo = instance.Mesh.Faces
            .Where(f => string.Equals(f.Label, "Fundo", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(fundo);

        float minX = fundo.SelectMany(f => f.Vertices).Min(v => v.X);
        float maxX = fundo.SelectMany(f => f.Vertices).Max(v => v.X);
        float minY = fundo.SelectMany(f => f.Vertices).Min(v => v.Y);
        float t = 18f;

        // afl=9 → fundo entra 9 mm em cada lateral (x0=t-9, x1=w-t+9).
        Assert.True(MathF.Abs(minX - (t - 9f)) < 1.5f, $"minX={minX}, esperado≈{t - 9f}");
        Assert.True(MathF.Abs(maxX - (w - t + 9f)) < 1.5f, $"maxX={maxX}, esperado≈{w - t + 9f}");
        // afb=7 → fundo assenta em y=t-7.
        Assert.True(MathF.Abs(minY - (t - 7f)) < 1.5f, $"minY={minY}, esperado≈{t - 7f}");
    }

    [Fact]
    public void Prateleira_TraseiraAlinhaComFaceDoFundo()
    {
        var definition = ModuleCatalog.GetRequired("balcao-2-portas");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm = 6f;
        settings.CozinhaInferiorBox.ShelfDepthInsetMm = 20f;

        var (w, h, d) = DimensionConfiguratorService.ResolveInsertionDimensions(definition, settings);
        var instance = ModuleCatalog.CreateInstance("balcao-2-portas", Vector3.Zero);
        instance.SetDimensions(w, h, d, definition, settings, respectCatalogLimits: false);

        var fundo = instance.Mesh.Faces.Where(f => f.Label == "Fundo").ToList();
        var prat = instance.Mesh.Faces.Where(f => f.Label == "Prateleira").ToList();
        Assert.NotEmpty(fundo);
        Assert.NotEmpty(prat);

        float fundoMaxZ = fundo.SelectMany(f => f.Vertices).Max(v => v.Z);
        float pratMinZ = prat.SelectMany(f => f.Vertices).Min(v => v.Z);
        float pratMaxZ = prat.SelectMany(f => f.Vertices).Max(v => v.Z);

        // Face interna do fundo = recuo 18 + espessura 6 = 24.
        Assert.True(MathF.Abs(fundoMaxZ - 24f) < 1.5f, $"Face do fundo maxZ={fundoMaxZ}, esperado≈24");
        Assert.True(MathF.Abs(pratMinZ - fundoMaxZ) < 1.5f,
            $"Traseira da prateleira deve alinhar à face do fundo (pratMinZ={pratMinZ}, fundoMaxZ={fundoMaxZ})");
        Assert.True(pratMaxZ <= d - 20f + 2f,
            $"Recuo frontal da prateleira (maxZ={pratMaxZ}, depth={d})");
    }

    [Fact]
    public void Cr_Prateleira_TambemAlinhaComFaceDoFundo()
    {
        var definition = ModuleCatalog.GetRequired("canto-cr-esq-950");
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        settings.CozinhaInferiorBox.InferiorChoice["fundo-tipo"] = "Inteiro";
        settings.CozinhaInferiorBox.InferiorNumeric["fundo-recuo"] = 18f;
        settings.CozinhaChapas.GetOrCreate(ChapaPieceKinds.FundoInferior).ThicknessMm = 6f;

        var instance = ModuleCatalog.CreateInstance("canto-cr-esq-950", Vector3.Zero);
        instance.SetDimensions(950f, 720f, 580f, definition, settings, respectCatalogLimits: false);

        var fundo = instance.Mesh.Faces.Where(f => f.Label == "Fundo").ToList();
        var prat = instance.Mesh.Faces.Where(f => f.Label == "Prateleira").ToList();
        Assert.NotEmpty(fundo);
        Assert.NotEmpty(prat);

        float fundoMaxZ = fundo.SelectMany(f => f.Vertices).Max(v => v.Z);
        float pratMinZ = prat.SelectMany(f => f.Vertices).Min(v => v.Z);
        Assert.True(MathF.Abs(pratMinZ - fundoMaxZ) < 1.5f,
            $"CR: prateleira minZ={pratMinZ} deve = face fundo {fundoMaxZ}");
    }
}
