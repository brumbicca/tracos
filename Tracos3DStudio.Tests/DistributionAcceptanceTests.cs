using Xunit;

namespace Tracos3DStudio.Tests;

/// <summary>Valida artefatos de distribuição local (instalador + publish).</summary>
public sealed class DistributionAcceptanceTests
{
    [Fact]
    public void Instalador_RegistroDeBuildValido()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string stampPath = Path.Combine(root, "dist", "last-build.txt");
        string installerPath = Path.Combine(root, "dist", "Tracos3DStudio-setup.exe");

        Assert.True(File.Exists(stampPath), "Execute installer\\publish.ps1 para gerar dist\\last-build.txt");
        Assert.True(File.Exists(installerPath), "Instalador dist\\Tracos3DStudio-setup.exe ausente");

        var lines = File.ReadAllLines(stampPath);
        Assert.Contains(lines, l => l.StartsWith("Build=", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Version=", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("Tracos3DStudio-setup.exe", StringComparison.Ordinal));

        Assert.True(new FileInfo(installerPath).Length > 50_000_000);
    }

    [Fact]
    public void PublishWinX64_ExecutavelReleaseExiste()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string exePath = Path.Combine(root, "publish", "win-x64", "Tracos3DStudio.exe");

        Assert.True(File.Exists(exePath), "Execute installer\\publish.ps1 para gerar publish\\win-x64\\Tracos3DStudio.exe");
        Assert.True(new FileInfo(exePath).Length > 50_000_000);
    }
}
