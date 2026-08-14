using System.Globalization;
using System.IO;
using System.Text;

namespace Tracos3DStudio;

public static class DxfExporter
{
  public static void ExportFloorPlan(TechnicalDrawingSet drawing, string filePath)
  {
    var sb = new StringBuilder();
    AppendHeader(sb);
    AppendTables(sb, ["PAREDES", "MODULOS"]);
    AppendFloorPlanEntities(sb, drawing);
    AppendFooter(sb);
    File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII);
  }

  public static void ExportPieces(PartsListSummary parts, string filePath)
  {
    var sb = new StringBuilder();
    AppendHeader(sb);
    AppendTables(sb, ["PECAS", "FUROS"]);
    AppendPieceEntities(sb, parts);
    AppendFooter(sb);
    File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII);
  }

  /// <summary>Peças na posição de nesting de cada chapa (mesmas coords do tracos-cnc-job / .tap).</summary>
  public static void ExportCutPlanSheets(MachineCutPlanDocument document, string baseFilePath)
  {
    string directory = Path.GetDirectoryName(baseFilePath) ?? "";
    string baseName = Path.GetExtension(baseFilePath) is { Length: > 0 } ext
        ? Path.GetFileNameWithoutExtension(baseFilePath)
        : baseFilePath;
    string extension = Path.GetExtension(baseFilePath);
    if (string.IsNullOrEmpty(extension))
      extension = ".dxf";

    if (document.Sheets.Count == 1)
    {
      ExportCutPlanSheet(document.Sheets[0], baseFilePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
          ? baseFilePath
          : baseFilePath + extension);
      return;
    }

    Directory.CreateDirectory(directory);

    foreach (var sheet in document.Sheets)
    {
      string sheetPath = Path.Combine(directory, $"{baseName}-chapa-{sheet.Index:D2}{extension}");
      ExportCutPlanSheet(sheet, sheetPath);
    }
  }

  public static void ExportCutPlanSheet(MachineCutSheet sheet, string filePath)
  {
    var sb = new StringBuilder();
    AppendHeader(sb);
    AppendTables(sb, ["CHAPA", "PECAS", "FUROS"]);
    AppendCutPlanSheetEntities(sb, sheet);
    AppendFooter(sb);
    File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII);
  }

  private static void AppendHeader(StringBuilder sb)
  {
    sb.AppendLine("0");
    sb.AppendLine("SECTION");
    sb.AppendLine("2");
    sb.AppendLine("HEADER");
    sb.AppendLine("0");
    sb.AppendLine("ENDSEC");
  }

  private static void AppendTables(StringBuilder sb, IReadOnlyList<string> layers)
  {
    sb.AppendLine("0");
    sb.AppendLine("SECTION");
    sb.AppendLine("2");
    sb.AppendLine("TABLES");
    sb.AppendLine("0");
    sb.AppendLine("TABLE");
    sb.AppendLine("2");
    sb.AppendLine("LAYER");
    sb.AppendLine("70");
    sb.AppendLine(layers.Count.ToString(CultureInfo.InvariantCulture));

    foreach (var layer in layers)
      AppendLayer(sb, layer);

    sb.AppendLine("0");
    sb.AppendLine("ENDTAB");
    sb.AppendLine("0");
    sb.AppendLine("ENDSEC");
  }

  private static void AppendLayer(StringBuilder sb, string name)
  {
    sb.AppendLine("0");
    sb.AppendLine("LAYER");
    sb.AppendLine("2");
    sb.AppendLine(name);
    sb.AppendLine("70");
    sb.AppendLine("0");
    sb.AppendLine("62");
    sb.AppendLine("7");
    sb.AppendLine("6");
    sb.AppendLine("CONTINUOUS");
  }

  private static void AppendFloorPlanEntities(StringBuilder sb, TechnicalDrawingSet drawing)
  {
    sb.AppendLine("0");
    sb.AppendLine("SECTION");
    sb.AppendLine("2");
    sb.AppendLine("ENTITIES");

    foreach (var wall in drawing.FloorPlanWalls)
      AppendLine(sb, "PAREDES", wall.X1, wall.Y1, wall.X2, wall.Y2);

    foreach (var module in drawing.FloorPlanModules)
    {
      float x2 = module.X + module.Width;
      float y2 = module.Y + module.Height;
      AppendLine(sb, "MODULOS", module.X, module.Y, x2, module.Y);
      AppendLine(sb, "MODULOS", x2, module.Y, x2, y2);
      AppendLine(sb, "MODULOS", x2, y2, module.X, y2);
      AppendLine(sb, "MODULOS", module.X, y2, module.X, module.Y);
    }

    sb.AppendLine("0");
    sb.AppendLine("ENDSEC");
  }

  private static void AppendPieceEntities(StringBuilder sb, PartsListSummary parts)
  {
    sb.AppendLine("0");
    sb.AppendLine("SECTION");
    sb.AppendLine("2");
    sb.AppendLine("ENTITIES");

    const float gapMm = 80f;
    const float maxRowWidthMm = 2800f;
    float cursorX = 0f;
    float cursorY = 0f;
    float rowHeight = 0f;

    foreach (var piece in parts.Items)
    {
      for (int q = 0; q < piece.Quantity; q++)
      {
        float width = piece.LengthMm;
        float height = piece.WidthMm;

        if (width <= 0f || height <= 0f)
          continue;

        if (cursorX > 0f && cursorX + width > maxRowWidthMm)
        {
          cursorX = 0f;
          cursorY += rowHeight + gapMm;
          rowHeight = 0f;
        }

        AppendRect(sb, "PECAS", cursorX, cursorY, width, height);

        foreach (var hole in piece.Holes)
        {
          float hx = cursorX + hole.PosXmm;
          float hy = cursorY + hole.PosYmm;
          AppendCircle(sb, "FUROS", hx, hy, hole.DiameterMm * 0.5f);
        }

        cursorX += width + gapMm;
        rowHeight = Math.Max(rowHeight, height);
      }
    }

    sb.AppendLine("0");
    sb.AppendLine("ENDSEC");
  }

  private static void AppendCutPlanSheetEntities(StringBuilder sb, MachineCutSheet sheet)
  {
    sb.AppendLine("0");
    sb.AppendLine("SECTION");
    sb.AppendLine("2");
    sb.AppendLine("ENTITIES");

    AppendRect(sb, "CHAPA", 0f, 0f, sheet.LengthMm, sheet.WidthMm);

    foreach (var piece in sheet.Pieces)
    {
      AppendRect(sb, "PECAS", piece.SheetXmm, piece.SheetYmm, piece.LengthMm, piece.WidthMm);

      foreach (var hole in piece.Holes)
      {
        var (sheetX, sheetY) = CncDrillCoordinateService.ToSheetCoordinates(piece, hole);
        AppendCircle(sb, "FUROS", sheetX, sheetY, hole.DiameterMm * 0.5f);
      }
    }

    sb.AppendLine("0");
    sb.AppendLine("ENDSEC");
  }

  private static void AppendRect(StringBuilder sb, string layer, float x, float y, float width, float height)
  {
    AppendLine(sb, layer, x, y, x + width, y);
    AppendLine(sb, layer, x + width, y, x + width, y + height);
    AppendLine(sb, layer, x + width, y + height, x, y + height);
    AppendLine(sb, layer, x, y + height, x, y);
  }

  private static void AppendCircle(StringBuilder sb, string layer, float cx, float cy, float radius)
  {
    if (radius <= 0f)
      return;

    sb.AppendLine("0");
    sb.AppendLine("CIRCLE");
    sb.AppendLine("8");
    sb.AppendLine(layer);
    sb.AppendLine("10");
    sb.AppendLine(cx.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("20");
    sb.AppendLine(cy.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("30");
    sb.AppendLine("0");
    sb.AppendLine("40");
    sb.AppendLine(radius.ToString(CultureInfo.InvariantCulture));
  }

  private static void AppendLine(StringBuilder sb, string layer, float x1, float y1, float x2, float y2)
  {
    sb.AppendLine("0");
    sb.AppendLine("LINE");
    sb.AppendLine("8");
    sb.AppendLine(layer);
    sb.AppendLine("10");
    sb.AppendLine(x1.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("20");
    sb.AppendLine(y1.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("30");
    sb.AppendLine("0");
    sb.AppendLine("11");
    sb.AppendLine(x2.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("21");
    sb.AppendLine(y2.ToString(CultureInfo.InvariantCulture));
    sb.AppendLine("31");
    sb.AppendLine("0");
  }

  private static void AppendFooter(StringBuilder sb)
  {
    sb.AppendLine("0");
    sb.AppendLine("EOF");
  }
}
