using OpenTK.Mathematics;

namespace Tracos3DStudio;

/// <summary>
/// Mapeia eixos locais (X/Y/Z) ↔ rótulos da seta no painel.
/// Portas do Canto L na face X (Porta esq.): o vão horizontal é Z, a espessura é X.
/// </summary>
public static class ModulePartAxisDisplay
{
    /// <summary>
    /// True quando a largura do vão da peça está no eixo Z (profundidade local),
    /// típico da Porta esq. do Canto L.
    /// </summary>
    public static bool FaceWidthIsDepth(string? partLabel, Vector3 localDims)
    {
        if (string.IsNullOrEmpty(partLabel))
            return false;

        if (partLabel.StartsWith("Porta esq", StringComparison.OrdinalIgnoreCase))
            return true;

        // Fechamento CR tipo Lateral (Promob Rotação 90): vão/dimensão em Z, espessura em X.
        if (partLabel.Equals("Fechamento frontal", StringComparison.OrdinalIgnoreCase) &&
            localDims.Z > localDims.X * 1.2f)
            return true;

        // Heurística: porta com espessura em X e vão em Z.
        return partLabel.StartsWith("Porta", StringComparison.OrdinalIgnoreCase)
               && localDims.X > 0.5f
               && localDims.Z > localDims.X * 2.5f;
    }

    /// <summary>Eixo geométrico correspondente à linha "Largura" do painel.</summary>
    public static PartHandleAxis PanelWidthAxis(bool faceWidthIsDepth) =>
        faceWidthIsDepth ? PartHandleAxis.Depth : PartHandleAxis.Width;

    /// <summary>Eixo geométrico correspondente à linha "Profundidade/Espessura" do painel.</summary>
    public static PartHandleAxis PanelDepthAxis(bool faceWidthIsDepth) =>
        faceWidthIsDepth ? PartHandleAxis.Width : PartHandleAxis.Depth;

    public static string WidthLabel(bool faceWidthIsDepth) =>
        faceWidthIsDepth ? "Largura (vão)" : "Largura";

    public static string DepthLabel(bool faceWidthIsDepth) =>
        faceWidthIsDepth ? "Espessura" : "Profundidade";

    public static float WidthValue(Vector3 dims, bool faceWidthIsDepth) =>
        faceWidthIsDepth ? dims.Z : dims.X;

    public static float DepthValue(Vector3 dims, bool faceWidthIsDepth) =>
        faceWidthIsDepth ? dims.X : dims.Z;
}
