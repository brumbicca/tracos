namespace Tracos3DStudio;

/// <summary>
/// Aplica edições de dimensão a uma peça individual do módulo, registrando um
/// <see cref="PartDimensionOverride"/> respeitado pelo <see cref="ModuleMeshBuilder"/>.
/// </summary>
public static class ModulePartEditService
{
    private const float MinPartSizeMm = 1f;

    public static bool TryApplyDimensions(
        ModuleInstance module,
        ModuleDefinition definition,
        string partLabel,
        float width,
        float height,
        float depth,
        out string? error)
    {
        error = null;

        if (string.IsNullOrEmpty(partLabel))
        {
            error = "Nenhuma peça selecionada.";
            return false;
        }

        bool exists = module.Mesh.Faces.Any(f => f.Label == partLabel);

        if (!exists)
        {
            error = "Peça não encontrada no módulo.";
            return false;
        }

        if (width < MinPartSizeMm || height < MinPartSizeMm || depth < MinPartSizeMm)
        {
            error = $"Dimensões da peça devem ser de no mínimo {MinPartSizeMm:0} mm.";
            return false;
        }

        // Medida absoluta congela a peça; ao definir medida, zera os deslocamentos do eixo.
        var ov = GetOrCreate(module, partLabel);
        ov.Width = width;
        ov.Height = height;
        ov.Depth = depth;
        ov.MinXOffset = 0f;
        ov.MaxXOffset = 0f;
        ov.MinYOffset = 0f;
        ov.MaxYOffset = 0f;
        ov.MinZOffset = 0f;
        ov.MaxZOffset = 0f;

        return true;
    }

    /// <summary>
    /// Aplica um incremento (mm) à face indicada pela seta. Cada face acumula seu
    /// próprio deslocamento, independente do lado oposto (recuar a face direita não
    /// mexe na esquerda). Positivo cresce para fora; negativo recua.
    /// </summary>
    public static bool TryApplyFaceOffset(
        ModuleInstance module,
        string partLabel,
        PartHandle handle,
        float increment,
        out string? error)
    {
        error = null;

        if (string.IsNullOrEmpty(partLabel))
        {
            error = "Nenhuma peça selecionada.";
            return false;
        }

        if (!module.Mesh.Faces.Any(f => f.Label == partLabel))
        {
            error = "Peça não encontrada no módulo.";
            return false;
        }

        if (increment == 0f)
            return true;

        var ov = GetOrCreate(module, partLabel);

        // A seta que aponta no sentido positivo desloca a face máxima; a negativa,
        // a face mínima. O sinal do incremento (para fora/dentro) é o mesmo em ambas.
        switch (handle.Axis)
        {
            case PartHandleAxis.Width:
                if (handle.Positive) ov.MaxXOffset += increment; else ov.MinXOffset += increment;
                break;
            case PartHandleAxis.Height:
                if (handle.Positive) ov.MaxYOffset += increment; else ov.MinYOffset += increment;
                break;
            case PartHandleAxis.Depth:
                if (handle.Positive) ov.MaxZOffset += increment; else ov.MinZOffset += increment;
                break;
        }

        if (!ov.HasAny)
            module.PartOverrides.Remove(partLabel);

        return true;
    }

    /// <summary>
    /// Define o deslocamento acumulado da face (valor absoluto no painel, ex.: -150).
    /// Reaplicar o mesmo valor não soma de novo.
    /// </summary>
    public static bool TrySetFaceOffset(
        ModuleInstance module,
        string partLabel,
        PartHandle handle,
        float absoluteOffset,
        out string? error)
    {
        float current = GetFaceOffset(module, partLabel, handle);
        return TryApplyFaceOffset(module, partLabel, handle, absoluteOffset - current, out error);
    }

    public static float GetFaceOffset(ModuleInstance module, string partLabel, PartHandle handle)
    {
        if (!module.PartOverrides.TryGetValue(partLabel, out var ov) || ov == null)
            return 0f;

        return handle.Axis switch
        {
            PartHandleAxis.Width => handle.Positive ? ov.MaxXOffset : ov.MinXOffset,
            PartHandleAxis.Height => handle.Positive ? ov.MaxYOffset : ov.MinYOffset,
            PartHandleAxis.Depth => handle.Positive ? ov.MaxZOffset : ov.MinZOffset,
            _ => 0f
        };
    }

    /// <summary>
    /// Offset a exibir no campo "+" da linha do painel (face da seta ativa no eixo,
    /// senão a face não-zero daquele eixo).
    /// </summary>
    public static float GetDisplayOffsetForAxis(
        ModuleInstance module,
        string partLabel,
        PartHandleAxis axis,
        bool? preferredPositive)
    {
        if (!module.PartOverrides.TryGetValue(partLabel, out var ov) || ov == null)
            return 0f;

        var (min, max) = axis switch
        {
            PartHandleAxis.Width => (ov.MinXOffset, ov.MaxXOffset),
            PartHandleAxis.Height => (ov.MinYOffset, ov.MaxYOffset),
            PartHandleAxis.Depth => (ov.MinZOffset, ov.MaxZOffset),
            _ => (0f, 0f)
        };

        if (preferredPositive == true)
            return max;
        if (preferredPositive == false)
            return min;
        if (MathF.Abs(max) >= MathF.Abs(min) && max != 0f)
            return max;
        return min != 0f ? min : max;
    }

    private static PartDimensionOverride GetOrCreate(ModuleInstance module, string partLabel)
    {
        if (!module.PartOverrides.TryGetValue(partLabel, out var ov) || ov == null)
        {
            ov = new PartDimensionOverride();
            module.PartOverrides[partLabel] = ov;
        }

        return ov;
    }
}
