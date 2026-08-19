namespace Tracos3DStudio;

/// <summary>
/// Parâmetros do Canto Reto (CR) por instância.
/// <see cref="UseSpacer"/> espelha o padrão do fabricante (configurador
/// <c>cr-uso-dist</c>) e pode ser gravado no módulo.
/// </summary>
public sealed class BlindCornerParams
{
    /// <summary>
    /// Promob «I — Utilização do Distanciador».
    /// true = gera a peça Distanciador e aplica avanços/recuos J–M;
    /// false = monta o canto sem essa peça.
    /// </summary>
    public bool UseSpacer { get; set; }

    public static BlindCornerParams FromConfigurator(DimensionConfiguratorSettings? settings) =>
        new() { UseSpacer = ReadUseSpacerFromSettings(settings) };

    public static bool ReadUseSpacerFromSettings(DimensionConfiguratorSettings? settings)
    {
        settings ??= DimensionConfiguratorSettings.CreateDefault();
        BoxAssemblyConfiguratorService.EnsureBoxInitialized(settings);
        if (settings.CozinhaInferiorBox.InferiorChoice.TryGetValue("cr-uso-dist", out var uso) &&
            !string.IsNullOrWhiteSpace(uso))
        {
            return uso.Equals("Sim", StringComparison.OrdinalIgnoreCase)
                   || uso.Equals("Usar", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public void SyncFromConfigurator(DimensionConfiguratorSettings? settings) =>
        UseSpacer = ReadUseSpacerFromSettings(settings);
}
