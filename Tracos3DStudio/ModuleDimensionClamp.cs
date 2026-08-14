namespace Tracos3DStudio;

/// <summary>
/// Limites de dimensão de módulos: catálogo (inserção) vs. edição livre no painel.
/// </summary>
public static class ModuleDimensionClamp
{
    public const float AbsoluteMinMm = 1f;

    public static float ClampForFreeEdit(float value, float globalMaxMm)
    {
        float max = globalMaxMm > 0f ? globalMaxMm : 10000f;
        return Math.Clamp(value, AbsoluteMinMm, max);
    }
}
