using System.Globalization;
using System.Windows;

namespace Tracos3DStudio;

public partial class ProjectClientDataWindow : Window
{
    private readonly Project _project;
    private readonly Action _onProjectChanged;

    public ProjectClientDataWindow(Project project, Action onProjectChanged)
    {
        InitializeComponent();
        _project = project;
        _onProjectChanged = onProjectChanged;
        LoadFields();
    }

    private void LoadFields()
    {
        ProjectNameBox.Text = _project.Metadata.Name;
        WorkNameBox.Text = _project.Metadata.WorkName ?? "";
        EnvironmentNameBox.Text = _project.Metadata.EnvironmentName ?? "";
        BudgetValidityDaysBox.Text = (_project.Metadata.BudgetValidityDays > 0
            ? _project.Metadata.BudgetValidityDays
            : 30).ToString();
        BudgetDiscountPercentBox.Text = _project.Metadata.BudgetDiscountPercent.ToString("0.##", CultureInfo.InvariantCulture);
        BudgetPaymentTermsBox.Text = _project.Metadata.BudgetPaymentTerms ?? "";
        BudgetSalesPersonBox.Text = _project.Metadata.BudgetSalesPerson ?? "";
        BudgetCommercialNotesBox.Text = _project.Metadata.BudgetCommercialNotes ?? "";
        ClientMetadataFields.LoadClient(_project.Metadata, ClientFields);
    }

    private void SaveFields()
    {
        _project.Metadata.Name = ClientMetadataFields.Normalize(ProjectNameBox.Text) ?? "Projeto sem título";
        _project.Metadata.WorkName = ClientMetadataFields.Normalize(WorkNameBox.Text);
        _project.Metadata.EnvironmentName = ClientMetadataFields.Normalize(EnvironmentNameBox.Text);
        RoomCompartmentService.SyncPrimaryCompartmentFromEnvironmentName(_project.Room, _project.Metadata);
        _project.Metadata.BudgetValidityDays = ParseBudgetValidityDays(BudgetValidityDaysBox.Text);
        _project.Metadata.BudgetDiscountPercent = ParseBudgetDiscountPercent(BudgetDiscountPercentBox.Text);
        _project.Metadata.BudgetPaymentTerms = ClientMetadataFields.Normalize(BudgetPaymentTermsBox.Text);
        _project.Metadata.BudgetSalesPerson = ClientMetadataFields.Normalize(BudgetSalesPersonBox.Text);
        _project.Metadata.BudgetCommercialNotes = ClientMetadataFields.Normalize(BudgetCommercialNotesBox.Text);
        ClientMetadataFields.SaveClient(_project.Metadata, ClientFields);
        _onProjectChanged();
    }

    private static int ParseBudgetValidityDays(string? text)
    {
        if (int.TryParse(text?.Trim(), out int days) && days > 0)
            return days;

        return 30;
    }

    private static decimal ParseBudgetDiscountPercent(string? text)
    {
        if (!decimal.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value))
            return 0m;

        return value switch
        {
            < 0m => 0m,
            > 100m => 100m,
            _ => value
        };
    }

    private void WorkField_Changed(object sender, RoutedEventArgs e) => SaveFields();

    private void ClientFields_FieldsChanged(object? sender, EventArgs e) => SaveFields();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SaveFields();
        DialogResult = true;
        Close();
    }
}
