using System.Globalization;
using System.IO;
using System.Text;

namespace Tracos3DStudio;

public static class JaraguaMach4TapExporter
{
    public static string Export(CncJobDocument job, JaraguaMach4TapSettings? settings = null) =>
        ExportSheet(job.Sheets[0], job.Settings.PanelThicknessMm, settings);

    public static string ExportSheet(CncJobSheet sheet, float panelThicknessMm, JaraguaMach4TapSettings? settings = null)
    {
        settings ??= new JaraguaMach4TapSettings();
        var sb = new StringBuilder();
        var writer = new TapLineWriter(sb);

        WriteHeader(sb, writer, settings);

        int cutIndex = 1;
        foreach (var operation in sheet.Operations)
        {
            switch (operation)
            {
                case CncCutOperation cut:
                    WriteContourCut(writer, cut, settings, cutIndex++);
                    break;
                case CncDrillOperation drill:
                    WriteDrill(writer, drill, panelThicknessMm, settings);
                    break;
            }
        }

        WriteFooter(sb, writer, settings);
        return sb.ToString();
    }

    public static IReadOnlyList<string> ExportAllSheets(CncJobDocument job, JaraguaMach4TapSettings? settings = null)
    {
        settings ??= new JaraguaMach4TapSettings();
        float thickness = job.Settings.PanelThicknessMm;

        return job.Sheets
            .Select(sheet => ExportSheet(sheet, thickness, settings))
            .ToList();
    }

    public static void ExportToFile(CncJobDocument job, string filePath, JaraguaMach4TapSettings? settings = null)
    {
        settings ??= new JaraguaMach4TapSettings();

        if (job.Sheets.Count == 1)
        {
            File.WriteAllText(filePath, Export(job, settings));
            return;
        }

        string directory = Path.GetDirectoryName(filePath) ?? "";
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        if (string.IsNullOrEmpty(extension))
            extension = ".tap";

        for (int i = 0; i < job.Sheets.Count; i++)
        {
            string sheetPath = Path.Combine(directory, $"{baseName}-chapa-{job.Sheets[i].Index:D2}{extension}");
            string content = ExportSheet(job.Sheets[i], job.Settings.PanelThicknessMm, settings);
            File.WriteAllText(sheetPath, content);
        }
    }

    private static void WriteHeader(StringBuilder sb, TapLineWriter writer, JaraguaMach4TapSettings settings)
    {
        sb.AppendLine("(JrgCnC - Vision V1.01 - by Aspire)");
        sb.AppendLine("(######## Troca de ferramentas ########)");
        sb.AppendLine($"(   NUMERO DA FERRAMENTA:{settings.ToolNumber})");
        sb.AppendLine(" ");
        writer.Write($"M6T{settings.ToolNumber}");
        writer.Write($"S{settings.SpindleRpm}");
        writer.Write($"G0 G43 H{settings.ToolNumber}");
    }

    private static void WriteFooter(StringBuilder sb, TapLineWriter writer, JaraguaMach4TapSettings settings)
    {
        writer.Write("G0 G53 Z0");
        writer.Write("M5");
        writer.Write("M30");
        writer.Write("M30");
        sb.AppendLine("%");
    }

    private static void WriteContourCut(
        TapLineWriter writer,
        CncCutOperation cut,
        JaraguaMach4TapSettings settings,
        int cutIndex)
    {
        var bounds = GetBounds(cut.ContourMm);
        float r = settings.ToolRadiusMm;
        float ox = settings.OriginOffsetXMm;
        float oy = settings.OriginOffsetYMm;

        float left = bounds.MinX + ox;
        float right = bounds.MaxX + ox;
        float bottom = bounds.MinY + oy;
        float top = bounds.MaxY + oy;

        writer.AppendRaw($"(Corte {cutIndex})");
        writer.AppendRaw("()");

        writer.Rapid(left, bottom, settings.SafeZMm);
        writer.PlungeSequence(left, bottom, settings);

        writer.Feed(left, top, settings.CutDepthZMm, settings.CutFeedMm);
        writer.ArcCw(left + r, top + r, r, 0f, settings.CutDepthZMm, settings.CutFeedMm);
        writer.Feed(right, top + r, settings.CutDepthZMm, settings.CutFeedMm);
        writer.ArcCw(right + r, top, 0f, -r, settings.CutDepthZMm, settings.CutFeedMm);
        writer.Feed(right + r, bottom, settings.CutDepthZMm, settings.CutFeedMm);
        writer.ArcCw(right, bottom - r, -r, 0f, settings.CutDepthZMm, settings.CutFeedMm);
        writer.Feed(left + r, bottom - r, settings.CutDepthZMm, settings.CutFeedMm);
        writer.ArcCw(left, bottom, 0f, r, settings.CutDepthZMm, settings.CutFeedMm);

        writer.Rapid(left, bottom, settings.SafeZMm);
    }

    private static void WriteDrill(
        TapLineWriter writer,
        CncDrillOperation drill,
        float panelThicknessMm,
        JaraguaMach4TapSettings settings)
    {
        float x = drill.SheetXmm + settings.OriginOffsetXMm;
        float y = drill.SheetYmm + settings.OriginOffsetYMm;

        if (drill.Kind == DrillHoleKind.HingeCup)
        {
            WriteVerticalDrill(writer, x, y, panelThicknessMm, drill.DepthMm, settings);
            return;
        }

        WriteHorizontalDrill(writer, x, y, drill, settings);
    }

    private static void WriteVerticalDrill(
        TapLineWriter writer,
        float x,
        float y,
        float panelThicknessMm,
        float depthMm,
        JaraguaMach4TapSettings settings)
    {
        float targetZ = Math.Max(settings.CutDepthZMm, panelThicknessMm - depthMm);

        writer.Rapid(x, y, settings.SafeZMm);
        writer.PlungeSequence(x, y, settings);
        writer.Feed(x, y, targetZ, settings.CutFeedMm);
        writer.Rapid(x, y, settings.SafeZMm);
    }

    private static void WriteHorizontalDrill(
        TapLineWriter writer,
        float x,
        float y,
        CncDrillOperation drill,
        JaraguaMach4TapSettings settings)
    {
        float z = settings.HorizontalDrillZMm;
        float endX = x;
        float endY = y;

        switch (drill.Edge)
        {
            case DrillHoleEdge.Front:
                endY += drill.DepthMm;
                break;
            case DrillHoleEdge.Back:
                endY -= drill.DepthMm;
                break;
            case DrillHoleEdge.Left:
                endX -= drill.DepthMm;
                break;
            case DrillHoleEdge.Right:
                endX += drill.DepthMm;
                break;
        }

        writer.Rapid(x, y, settings.SafeZMm);
        writer.Rapid(x, y, settings.SafeZMm);
        writer.Feed(x, y, settings.ClearanceZMm, settings.PlungeFeedMm);
        writer.Feed(x, y + 0.07f, settings.ClearanceZMm - 0.008f, settings.PlungeFeedMm);
        writer.Feed(x + settings.RampLengthMm, y + 0.07f, settings.RampZMm + 4.55f, settings.PlungeFeedMm);
        writer.Feed(x, y + 0.07f, z + 0.008f, settings.PlungeFeedMm);
        writer.Feed(x, y, z, settings.PlungeFeedMm);
        writer.Feed(x, y + 0.07f, z, settings.CutFeedMm);
        writer.Feed(endX, endY + 0.07f, z, settings.CutFeedMm);
        writer.Feed(x, y + 0.07f, z, settings.CutFeedMm);
        writer.Feed(x, y, z, settings.CutFeedMm);
        writer.Rapid(x, y, settings.SafeZMm);
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) GetBounds(IReadOnlyList<float[]> contour)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var point in contour)
        {
            minX = Math.Min(minX, point[0]);
            minY = Math.Min(minY, point[1]);
            maxX = Math.Max(maxX, point[0]);
            maxY = Math.Max(maxY, point[1]);
        }

        return (minX, minY, maxX, maxY);
    }

    private sealed class TapLineWriter
    {
        private readonly StringBuilder _sb;
        private int _lineNumber = 1;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public TapLineWriter(StringBuilder sb) => _sb = sb;

        public void AppendRaw(string text) => _sb.AppendLine(text);

        public void Write(string gcode) => _sb.AppendLine($"N{_lineNumber++}{gcode}");

        public void Rapid(float x, float y, float z) =>
            Write($"G00X{Format(x)}Y{Format(y)}Z{Format(z)}");

        public void Feed(float x, float y, float z, float feed) =>
            Write($"G1X{Format(x)}Y{Format(y)}Z{Format(z)}F{Format(feed)}");

        public void ArcCw(float x, float y, float i, float j, float z, float feed) =>
            Write($"G2X{Format(x)}Y{Format(y)}I{Format(i)}J{Format(j)}Z{Format(z)}F{Format(feed)}");

        public void PlungeSequence(float x, float y, JaraguaMach4TapSettings settings)
        {
            Write($"G1X{Format(x)}Y{Format(y)}Z{Format(settings.ClearanceZMm)}F{Format(settings.PlungeFeedMm)}");
            Write($"G1X{Format(x)}Y{Format(y + settings.RampLengthMm)}Z{Format(settings.RampZMm)}");
            Write($"G1X{Format(x)}Y{Format(y)}Z{Format(settings.CutDepthZMm)}");
        }

        private static string Format(float value) =>
            value.ToString("0.0##", Invariant);
    }
}
