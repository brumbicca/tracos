using System.Windows;
using System.Windows.Controls;

namespace Tracos3DStudio;

public partial class ClientDataFieldsPanel : UserControl
{
    public event EventHandler? FieldsChanged;

    public ClientDataFieldsPanel()
    {
        InitializeComponent();
        RefreshTaxLabel();
    }

    public ClientCustomerType ClientCustomerType
    {
        get => ClientCustomerTypeCombo.SelectedIndex == 1
            ? ClientCustomerType.LegalEntity
            : ClientCustomerType.Individual;
        set => ClientCustomerTypeCombo.SelectedIndex = value == ClientCustomerType.LegalEntity ? 1 : 0;
    }

    public string ClientCode
    {
        get => ClientCodeBox.Text;
        set => ClientCodeBox.Text = value;
    }

    public string ClientName
    {
        get => ClientNameBox.Text;
        set => ClientNameBox.Text = value;
    }

    public string ClientTaxId
    {
        get => ClientTaxIdBox.Text;
        set => ClientTaxIdBox.Text = value;
    }

    public string ClientAddress
    {
        get => ClientAddressBox.Text;
        set => ClientAddressBox.Text = value;
    }

    public string ClientAddressNumber
    {
        get => ClientAddressNumberBox.Text;
        set => ClientAddressNumberBox.Text = value;
    }

    public string ClientAddressComplement
    {
        get => ClientAddressComplementBox.Text;
        set => ClientAddressComplementBox.Text = value;
    }

    public string ClientNeighborhood
    {
        get => ClientNeighborhoodBox.Text;
        set => ClientNeighborhoodBox.Text = value;
    }

    public string ClientDeliveryAddress
    {
        get => ClientDeliveryAddressBox.Text;
        set => ClientDeliveryAddressBox.Text = value;
    }

    public string ClientCity
    {
        get => ClientCityBox.Text;
        set => ClientCityBox.Text = value;
    }

    public string ClientState
    {
        get => ClientStateBox.Text;
        set => ClientStateBox.Text = value;
    }

    public string ClientZip
    {
        get => ClientZipBox.Text;
        set => ClientZipBox.Text = value;
    }

    public string ClientPhone
    {
        get => ClientPhoneBox.Text;
        set => ClientPhoneBox.Text = value;
    }

    public string ClientMobile
    {
        get => ClientMobileBox.Text;
        set => ClientMobileBox.Text = value;
    }

    public string ClientEmail
    {
        get => ClientEmailBox.Text;
        set => ClientEmailBox.Text = value;
    }

    public string ClientNotes
    {
        get => ClientNotesBox.Text;
        set => ClientNotesBox.Text = value;
    }

    private void ClientField_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        RefreshTaxLabel();
        FieldsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshTaxLabel()
    {
        if (ClientTaxIdLabel is null)
            return;

        ClientTaxIdLabel.Text = ClientCustomerType == ClientCustomerType.LegalEntity ? "CNPJ" : "CPF";
    }
}
