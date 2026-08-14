using System.Globalization;
using System.IO;
using System.Text;

namespace Tracos3DStudio;

public static class CncDrillCsvExporter
{
    public static void Export(MachineCutPlanDocument document, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Chapa;Material;Espessura_mm;Instancia;Modulo;Peca;PecaX_mm;PecaY_mm;PecaL_mm;PecaA_mm;Rotacionada;" +
            "FuroTipo;Aresta;LocalX_mm;LocalY_mm;ChapaX_mm;ChapaY_mm;Diametro_mm;Profundidade_mm");

        foreach (var sheet in document.Sheets)
        {
            foreach (var piece in sheet.Pieces)
            {
                foreach (var hole in piece.Holes)
                {
                    var (sheetX, sheetY) = CncDrillCoordinateService.ToSheetCoordinates(piece, hole);

                    sb.Append(sheet.Index.ToString(CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(Escape(sheet.MaterialName));
                    sb.Append(';');
                    sb.Append(sheet.ThicknessMm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(piece.InstanceId.ToString(CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(Escape(piece.ModuleName));
                    sb.Append(';');
                    sb.Append(Escape(piece.PieceName));
                    sb.Append(';');
                    sb.Append(piece.SheetXmm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(piece.SheetYmm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(piece.LengthMm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(piece.WidthMm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(piece.Rotated ? "Sim" : "Não");
                    sb.Append(';');
                    sb.Append(Escape(hole.Kind.ToString()));
                    sb.Append(';');
                    sb.Append(Escape(hole.Edge.ToString()));
                    sb.Append(';');
                    sb.Append(hole.PosXmm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(hole.PosYmm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(sheetX.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(sheetY.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(hole.DiameterMm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.Append(';');
                    sb.Append(hole.DepthMm.ToString("0.##", CultureInfo.InvariantCulture));
                    sb.AppendLine();
                }
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static int CountDrillRows(MachineCutPlanDocument document) =>
        document.Sheets.Sum(s => s.Pieces.Sum(p => p.Holes.Count));

    private static string Escape(string value)
    {
        if (value.Contains(';', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        return value;
    }
}
