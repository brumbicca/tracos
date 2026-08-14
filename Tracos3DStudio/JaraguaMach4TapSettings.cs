namespace Tracos3DStudio;

/// <summary>
/// Parâmetros do post Jaraguá CNC SOLID TAF (Mach4, .tap).
/// Valores padrão calibrados contra <c>teste corte.tap</c> e <c>JRGCNC - TAF.pp</c>.
/// </summary>
public sealed class JaraguaMach4TapSettings
{
    public int ToolNumber { get; init; } = 3;

    public int SpindleRpm { get; init; } = 18000;

    /// <summary>Raio da fresa de contorno (arcos G2/G3 no .tap de referência).</summary>
    public float ToolRadiusMm { get; init; } = 3.5f;

    /// <summary>Deslocamento de origem da chapa no Mach4 (X do primeiro corte ≈ margem + 9,5 mm).</summary>
    public float OriginOffsetXMm { get; init; } = 9.5f;

    public float OriginOffsetYMm { get; init; } = 0f;

    public float SafeZMm { get; init; } = 23.080f;

    public float ClearanceZMm { get; init; } = 18.0f;

    public float RampZMm { get; init; } = 8.950f;

    public float RampLengthMm { get; init; } = 40f;

    public float CutDepthZMm { get; init; } = -0.100f;

    /// <summary>Altura Z para furos horizontais (minifix) em chapa 18 mm.</summary>
    public float HorizontalDrillZMm { get; init; } = 9.0f;

    public float PlungeFeedMm { get; init; } = 7000f;

    public float CutFeedMm { get; init; } = 5000f;
}
