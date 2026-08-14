namespace Tracos3DStudio;

public static class CncDrillCoordinateService
{
    /// <summary>
    /// Converte furo em coordenadas locais da peça (Length × Width) para coordenadas na chapa.
    /// </summary>
    public static (float SheetXmm, float SheetYmm) ToSheetCoordinates(
        MachineCutPlacement placement,
        MachineCutHole hole)
    {
        if (!placement.Rotated)
        {
            return (
                placement.SheetXmm + hole.PosXmm,
                placement.SheetYmm + hole.PosYmm);
        }

        float originalLength = placement.WidthMm;
        float sheetLocalX = hole.PosYmm;
        float sheetLocalY = originalLength - hole.PosXmm;

        return (
            placement.SheetXmm + sheetLocalX,
            placement.SheetYmm + sheetLocalY);
    }
}
