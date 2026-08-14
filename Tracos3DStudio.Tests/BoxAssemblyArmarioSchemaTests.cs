using Xunit;

namespace Tracos3DStudio.Tests;

public sealed class BoxAssemblyArmarioSchemaTests
{
    [Fact]
    public void Lateral_ParidadePromob_TemSeteCamposComGrupos()
    {
        var node = BoxAssemblyArmarioSchema.FindNode("lateral");
        Assert.NotNull(node);
        Assert.Equal(7, node!.Fields.Length);

        Assert.Equal("Tipo Lateral", node.Fields[0].Group);
        Assert.Equal("tip-lat", node.Fields[0].Key);
        Assert.Equal("A — Lateral", node.Fields[0].Label);
        Assert.Equal("Fixo", node.Fields[0].DefaultOption);

        Assert.Equal("arm-rlb", node.Fields[2].Key);
        Assert.Equal("C — Avanço Lateral Fixo sobre Base (mm)", node.Fields[2].Label);
        Assert.Equal(58f, node.Fields[2].DefaultValue);

        Assert.Equal("lat-ali", node.Fields[6].Key);
        Assert.Equal("Central", node.Fields[6].DefaultOption);
        Assert.Equal(["Traseiro", "Central", "Frontal"], node.Fields[6].Options);
    }

    [Fact]
    public void EnsureArmarioInitialized_Lateral_SemeiaDefaultsPromob()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.DormitorioArmarioBox;

        Assert.Equal("Fixo", box.ArmarioChoice["tip-lat"]);
        Assert.Equal(58f, box.ArmarioNumeric["arm-rlb"]);
        Assert.Equal(10f, box.ArmarioNumeric["arm-rlb-sup"]);
        Assert.Equal("Central", box.ArmarioChoice["lat-ali"]);
        Assert.False(box.ArmarioNumeric.ContainsKey("lat-rebaixo"));
    }

    [Fact]
    public void Rodape_ParidadePromob_TemQuatroCamposComGrupos()
    {
        var node = BoxAssemblyArmarioSchema.FindNode("rodape");
        Assert.NotNull(node);
        Assert.Equal(4, node!.Fields.Length);

        Assert.Equal("Tipo Rodapé", node.Fields[0].Group);
        Assert.Equal("tip-rod", node.Fields[0].Key);
        Assert.Equal("A — Rodapé", node.Fields[0].Label);
        Assert.Equal("Fixo", node.Fields[0].DefaultOption);

        Assert.Equal("rod-rec-fro", node.Fields[1].Key);
        Assert.Equal("B — Recuo Rodapé Frontal (mm)", node.Fields[1].Label);
        Assert.Equal(50f, node.Fields[1].DefaultValue);

        Assert.Equal("Rodapé Fixo", node.Fields[3].Group);
        Assert.Equal("rod-alt-fix", node.Fields[3].Key);
        Assert.Equal(80f, node.Fields[3].DefaultValue);
    }

    [Fact]
    public void EnsureArmarioInitialized_Rodape_SemeiaDefaultsPromob()
    {
        var settings = DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        var box = settings.DormitorioArmarioBox;

        Assert.Equal("Fixo", box.ArmarioChoice["tip-rod"]);
        Assert.Equal(50f, box.ArmarioNumeric["rod-rec-fro"]);
        Assert.Equal(0f, box.ArmarioNumeric["rod-rec-tra"]);
        Assert.Equal(80f, box.ArmarioNumeric["rod-alt-fix"]);
    }
}
