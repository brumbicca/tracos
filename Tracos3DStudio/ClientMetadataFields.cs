namespace Tracos3DStudio;

/// <summary>Carrega e grava campos de cliente/obra no metadata (Promob Dados do Cliente).</summary>
public static class ClientMetadataFields
{
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void LoadClient(ProjectMetadata metadata, ClientDataFieldsPanel panel)
    {
        panel.ClientCustomerType = metadata.ClientCustomerType;
        panel.ClientCode = metadata.ClientCode ?? "";
        panel.ClientName = metadata.ClientName ?? "";
        panel.ClientTaxId = metadata.ClientTaxId ?? "";
        panel.ClientAddress = metadata.ClientAddress ?? "";
        panel.ClientAddressNumber = metadata.ClientAddressNumber ?? "";
        panel.ClientAddressComplement = metadata.ClientAddressComplement ?? "";
        panel.ClientNeighborhood = metadata.ClientNeighborhood ?? "";
        panel.ClientDeliveryAddress = metadata.ClientDeliveryAddress ?? "";
        panel.ClientCity = metadata.ClientCity ?? "";
        panel.ClientState = metadata.ClientState ?? "";
        panel.ClientZip = metadata.ClientZip ?? "";
        panel.ClientPhone = metadata.ClientPhone ?? "";
        panel.ClientMobile = metadata.ClientMobile ?? "";
        panel.ClientEmail = metadata.ClientEmail ?? "";
        panel.ClientNotes = metadata.ClientNotes ?? "";
        panel.RefreshTaxLabel();
    }

    public static void SaveClient(ProjectMetadata metadata, ClientDataFieldsPanel panel)
    {
        metadata.ClientCustomerType = panel.ClientCustomerType;
        metadata.ClientCode = Normalize(panel.ClientCode);
        metadata.ClientName = Normalize(panel.ClientName);
        metadata.ClientTaxId = Normalize(panel.ClientTaxId);
        metadata.ClientAddress = Normalize(panel.ClientAddress);
        metadata.ClientAddressNumber = Normalize(panel.ClientAddressNumber);
        metadata.ClientAddressComplement = Normalize(panel.ClientAddressComplement);
        metadata.ClientNeighborhood = Normalize(panel.ClientNeighborhood);
        metadata.ClientDeliveryAddress = Normalize(panel.ClientDeliveryAddress);
        metadata.ClientCity = Normalize(panel.ClientCity);
        metadata.ClientState = Normalize(panel.ClientState);
        metadata.ClientZip = Normalize(panel.ClientZip);
        metadata.ClientPhone = Normalize(panel.ClientPhone);
        metadata.ClientMobile = Normalize(panel.ClientMobile);
        metadata.ClientEmail = Normalize(panel.ClientEmail);
        metadata.ClientNotes = Normalize(panel.ClientNotes);
    }
}
