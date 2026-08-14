using System.Globalization;
using System.IO;
using System.Text;

namespace Tracos3DStudio;

public static class CutPlanCsvExporter
{
    public static void Export(CutPlanSummary plan, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Chapa;Material;Espessura_mm;Modulo;Peca;X_mm;Y_mm;Largura_mm;Altura_mm;Rotacionada;Fita_borda");

        foreach (var sheet in plan.Sheets)
        {
            foreach (var placement in sheet.Placements)
            {
                sb.Append(sheet.Index.ToString(CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(Escape(sheet.MaterialName));
                sb.Append(';');
                sb.Append(sheet.ThicknessMm.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(Escape(placement.Piece.ModuleName));
                sb.Append(';');
                sb.Append(Escape(placement.Piece.PieceName));
                sb.Append(';');
                sb.Append(placement.X.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(placement.Y.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(placement.WidthMm.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(placement.HeightMm.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append(';');
                sb.Append(placement.Rotated ? "Sim" : "Não");
                sb.Append(';');
                sb.Append(Escape(placement.Piece.EdgeBand ?? ""));
                sb.AppendLine();
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value)
    {
        if (value.Contains(';', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        return value;
    }
}
