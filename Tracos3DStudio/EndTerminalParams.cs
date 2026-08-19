namespace Tracos3DStudio;

/// <summary>
/// Medidas próprias dos terminais Diagonal e Chanfrado.
/// A é sempre a profundidade da lateral curta; no Chanfrado, B é o trecho
/// reto da frente, da face externa da lateral longa até o encontro angular.
/// </summary>
public sealed class EndTerminalParams
{
    public float SmallSideDepthMm { get; set; }

    public float FrontStraightWidthMm { get; set; }

    public int DoorCount { get; set; } = 1;

    public static EndTerminalParams FromDefinition(ModuleDefinition definition)
    {
        float depth = MathF.Max(80f, definition.DefaultDepth);
        float width = MathF.Max(80f, definition.DefaultWidth);
        return new EndTerminalParams
        {
            SmallSideDepthMm = Math.Clamp(depth * .55f, 60f, depth - 20f),
            FrontStraightWidthMm = definition.ShapeKind == ModuleShapeKind.EndChamfer
                ? Math.Clamp(width * .45f, 0f, width - 20f)
                : 0f,
            DoorCount = Math.Clamp(definition.DoorCount, 1, 2)
        };
    }

    public void ClampToModule(float width, float depth, bool isChamfer)
    {
        float minDepth = MathF.Min(MathF.Max(20f, depth - 20f), 60f);
        SmallSideDepthMm = Math.Clamp(SmallSideDepthMm, minDepth, MathF.Max(minDepth, depth - 20f));
        FrontStraightWidthMm = isChamfer
            ? Math.Clamp(FrontStraightWidthMm, 0f, MathF.Max(0f, width - 20f))
            : 0f;
        DoorCount = Math.Clamp(DoorCount, 1, 2);
    }
}
