namespace Tracos3DStudio;

public static class LibraryState
{
    public static string? LibraryName { get; set; }

    public static string? CompanyDisplayName { get; set; }

    public static string? BudgetLogoPath { get; set; }

    public static Dictionary<string, decimal>? ModulePrices { get; set; }

    public static bool TryGetModulePrice(string definitionId, out decimal price)
    {
        if (ModulePrices != null && ModulePrices.TryGetValue(definitionId, out price))
            return true;

        price = 0m;
        return false;
    }

    public static void Apply(LibraryDocument document)
    {
        LibraryName = document.Name;
        CompanyDisplayName = document.CompanyDisplayName;
        BudgetLogoPath = document.BudgetLogoPath;
        ModulePrices = document.ModulePrices != null
            ? new Dictionary<string, decimal>(document.ModulePrices)
            : null;
    }
}
