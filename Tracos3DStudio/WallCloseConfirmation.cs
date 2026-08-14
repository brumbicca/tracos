namespace Tracos3DStudio;

/// <summary>
/// Confirmação ao fechar o contorno de paredes (paridade Promob P8 / V3.1a).
/// </summary>
public static class WallCloseConfirmation
{
    public const string DialogTitle = "Fechar parede";

    public const string DialogMessage =
        "Deseja fechar a parede e finalizar o ambiente?";

    /// <summary>
    /// Exibir diálogo quando o usuário clica no vértice inicial com contorno fechável (≥ 3 vértices).
    /// </summary>
    public static bool ShouldConfirm(int confirmedPointCount, bool closingAtFirstVertex) =>
        closingAtFirstVertex && confirmedPointCount >= 3;
}
