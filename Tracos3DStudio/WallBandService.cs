namespace Tracos3DStudio;

public enum WallBandEdgeKind
{
    Start,
    End
}

public static class WallBandService
{
    public const float MinBandHeightMm = 50f;

    public static bool TryAddHorizontalBand(
        WallSegment wall,
        float bottomMm,
        float topMm,
        out WallBand? band,
        out string? error)
    {
        band = null;
        error = null;

        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float minBottom = 0f;
        float maxTop = wallTop;

        bottomMm = MathF.Max(minBottom, bottomMm);
        topMm = MathF.Min(maxTop, topMm);

        if (topMm - bottomMm < MinBandHeightMm)
        {
            error = $"Faixa precisa de altura mínima de {MinBandHeightMm:0} mm.";
            return false;
        }

        foreach (var existing in wall.Bands)
        {
            if (!existing.IsHorizontal)
                continue;

            if (bottomMm < existing.EndMm && existing.StartMm < topMm)
            {
                error = "Faixa sobrepõe outra faixa horizontal.";
                return false;
            }
        }

        band = new WallBand
        {
            IsHorizontal = true,
            StartMm = bottomMm,
            EndMm = topMm
        };

        wall.Bands.Add(band);
        return true;
    }

    public static bool TryAddDefaultUpperBand(WallSegment wall, out WallBand? band, out string? error)
    {
        float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
        float bottom = MathF.Min(2100f, wallTop - MinBandHeightMm);

        return TryAddHorizontalBand(wall, bottom, wallTop, out band, out error);
    }

    public const float DefaultVerticalBandWidthMm = 1200f;

    public static bool TryAddVerticalBand(
        WallSegment wall,
        float startAlongMm,
        float endAlongMm,
        out WallBand? band,
        out string? error)
    {
        band = null;
        error = null;

        float length = wall.Length;

        if (length < MinBandHeightMm)
        {
            error = "Parede muito curta para faixa vertical.";
            return false;
        }

        startAlongMm = Math.Clamp(startAlongMm, 0f, length - MinBandHeightMm);
        endAlongMm = Math.Clamp(endAlongMm, startAlongMm + MinBandHeightMm, length);

        foreach (var existing in wall.Bands)
        {
            if (existing.IsHorizontal)
                continue;

            if (startAlongMm < existing.EndMm && existing.StartMm < endAlongMm)
            {
                error = "Faixa sobrepõe outra faixa vertical.";
                return false;
            }
        }

        band = new WallBand
        {
            IsHorizontal = false,
            StartMm = startAlongMm,
            EndMm = endAlongMm
        };

        wall.Bands.Add(band);
        return true;
    }

    public static bool TryAddVerticalBandAtCenter(
        WallSegment wall,
        float centerAlongMm,
        out WallBand? band,
        out string? error)
    {
        float length = wall.Length;
        float width = MathF.Min(DefaultVerticalBandWidthMm, length);

        if (width >= length)
            return TryAddVerticalBand(wall, 0f, length, out band, out error);

        float half = width * 0.5f;
        float start = Math.Clamp(centerAlongMm - half, 0f, length - width);
        float end = start + width;

        return TryAddVerticalBand(wall, start, end, out band, out error);
    }

    public static bool TrySetBandEdge(
        WallSegment wall,
        Guid bandId,
        WallBandEdgeKind edge,
        float newValueMm,
        out string? error)
    {
        error = null;

        var band = wall.Bands.FirstOrDefault(b => b.Id == bandId);

        if (band == null)
        {
            error = "Faixa não encontrada.";
            return false;
        }

        float start = band.StartMm;
        float end = band.EndMm;

        if (band.IsHorizontal)
        {
            float wallTop = MathF.Max(wall.HeightStart, wall.HeightEnd);
            newValueMm = Math.Clamp(newValueMm, 0f, wallTop);

            if (edge == WallBandEdgeKind.Start)
                start = newValueMm;
            else
                end = newValueMm;

            if (end - start < MinBandHeightMm)
            {
                error = $"Faixa precisa de altura mínima de {MinBandHeightMm:0} mm.";
                return false;
            }
        }
        else
        {
            float length = wall.Length;
            newValueMm = Math.Clamp(newValueMm, 0f, length);

            if (edge == WallBandEdgeKind.Start)
                start = newValueMm;
            else
                end = newValueMm;

            if (end - start < MinBandHeightMm)
            {
                error = $"Faixa precisa de largura mínima de {MinBandHeightMm:0} mm.";
                return false;
            }
        }

        foreach (var existing in wall.Bands)
        {
            if (existing.Id == band.Id || existing.IsHorizontal != band.IsHorizontal)
                continue;

            if (start < existing.EndMm && existing.StartMm < end)
            {
                error = band.IsHorizontal
                    ? "Faixa sobrepõe outra faixa horizontal."
                    : "Faixa sobrepõe outra faixa vertical.";
                return false;
            }
        }

        band.StartMm = start;
        band.EndMm = end;
        return true;
    }

    public static bool TryRemoveBand(WallSegment wall, Guid bandId, out string? error)
    {
        error = null;

        var band = wall.Bands.FirstOrDefault(b => b.Id == bandId);

        if (band == null)
        {
            error = "Faixa não encontrada.";
            return false;
        }

        wall.Bands.Remove(band);
        return true;
    }

    public static string FormatLabel(WallBand band) =>
        band.IsHorizontal
            ? $"Horizontal {band.StartMm:0}–{band.EndMm:0} mm"
            : $"Vertical {band.StartMm:0}–{band.EndMm:0} mm";

    public static string FormatSummaryLine(WallBand band)
    {
        string span = FormatLabel(band);
        string material = WallSurfaceMaterialCatalog.GetDisplayName(band.MaterialId);
        return string.IsNullOrWhiteSpace(band.MaterialId) ? span : $"{span} · {material}";
    }
}
