namespace Tracos3DStudio;

public enum SpecialColumnPosition
{
    Left,
    Center,
    Right
}

/// <summary>
/// Recorte traseiro dos balcões especiais para pilares/colunas.
/// As medidas pertencem à instância para que cada módulo possa ser ajustado no projeto.
/// </summary>
public sealed class SpecialColumnParams
{
    public SpecialColumnPosition Position { get; set; }

    public float WidthMm { get; set; } = 200f;

    public float DepthMm { get; set; } = 200f;

    /// <summary>Distância da lateral esquerda até o início da coluna central.</summary>
    public float LeftOffsetMm { get; set; } = 100f;

    /// <summary>True recorta as prateleiras; false inicia as prateleiras depois da coluna.</summary>
    public bool ShelfNotched { get; set; } = true;

    public static SpecialColumnParams FromDefinition(ModuleDefinition definition)
    {
        var position = definition.Id.Contains("central", StringComparison.OrdinalIgnoreCase)
            ? SpecialColumnPosition.Center
            : definition.Id.Contains("dir", StringComparison.OrdinalIgnoreCase)
                ? SpecialColumnPosition.Right
                : SpecialColumnPosition.Left;

        float width = Math.Clamp(200f, 80f, MathF.Max(80f, definition.DefaultWidth - 80f));
        return new SpecialColumnParams
        {
            Position = position,
            WidthMm = width,
            DepthMm = Math.Clamp(200f, 50f, MathF.Max(50f, definition.DefaultDepth - 50f)),
            LeftOffsetMm = MathF.Max(0f, (definition.DefaultWidth - width) * 0.5f),
            ShelfNotched = true
        };
    }

    public void ClampToModule(float moduleWidth, float moduleDepth)
    {
        WidthMm = Math.Clamp(WidthMm, 20f, MathF.Max(20f, moduleWidth - 40f));
        DepthMm = Math.Clamp(DepthMm, 20f, MathF.Max(20f, moduleDepth - 20f));
        LeftOffsetMm = Position == SpecialColumnPosition.Center
            ? Math.Clamp(LeftOffsetMm, 20f, MathF.Max(20f, moduleWidth - WidthMm - 20f))
            : Position == SpecialColumnPosition.Right
                ? MathF.Max(0f, moduleWidth - WidthMm)
                : 0f;
    }

    public (float Start, float End) GetHorizontalRange(float moduleWidth)
    {
        ClampToModule(moduleWidth, float.MaxValue / 4f);
        float start = Position switch
        {
            SpecialColumnPosition.Right => moduleWidth - WidthMm,
            SpecialColumnPosition.Center => LeftOffsetMm,
            _ => 0f
        };
        return (start, start + WidthMm);
    }
}
