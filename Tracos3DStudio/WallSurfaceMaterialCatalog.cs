namespace Tracos3DStudio;

public sealed class WallSurfaceMaterialOption
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string ColorHex { get; init; }
}

/// <summary>Materiais de acabamento para faixas e regiões de parede (módulos + revestimentos).</summary>
public static class WallSurfaceMaterialCatalog
{
    public static IReadOnlyList<WallSurfaceMaterialOption> All
    {
        get
        {
            var list = new List<WallSurfaceMaterialOption>();

            foreach (var material in MaterialCatalog.All)
            {
                list.Add(new WallSurfaceMaterialOption
                {
                    Id = material.Id,
                    DisplayName = material.DisplayName,
                    ColorHex = material.ColorHex
                });
            }

            foreach (var material in FloorMaterialCatalog.All)
            {
                list.Add(new WallSurfaceMaterialOption
                {
                    Id = material.Id,
                    DisplayName = material.DisplayName,
                    ColorHex = material.ColorHex
                });
            }

            return list;
        }
    }

    public static WallSurfaceMaterialOption? FindOption(string? materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            return null;

        return All.FirstOrDefault(m => m.Id.Equals(materialId, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetDisplayName(string? materialId)
    {
        var option = FindOption(materialId);
        return option != null ? option.DisplayName : string.IsNullOrWhiteSpace(materialId) ? "—" : materialId;
    }

    public static OpenTK.Mathematics.Vector4 GetPreviewColor(string? materialId, float alpha = 0.38f)
    {
        var option = FindOption(materialId);
        var (r, g, b) = ColorParsing.ParseHexRgb(option?.ColorHex ?? "#CCCCCC");
        return new OpenTK.Mathematics.Vector4(r, g, b, alpha);
    }
}
