namespace Tracos3DStudio;

public static class WallBandGeometry
{
    public static bool ContainsPoint(
        WallBand band,
        float along,
        float height,
        float wallLength,
        float wallTop)
    {
        if (band.IsHorizontal)
            return height >= band.StartMm && height <= band.EndMm && along >= 0f && along <= wallLength;

        return along >= band.StartMm && along <= band.EndMm && height >= 0f && height <= wallTop;
    }

    public static float EstimateArea(WallBand band, float wallLength, float wallTop) =>
        band.IsHorizontal
            ? wallLength * (band.EndMm - band.StartMm)
            : (band.EndMm - band.StartMm) * wallTop;
}
