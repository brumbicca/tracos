namespace Tracos3DStudio;

public readonly record struct ModulationFrontRect(
    float X1,
    float Y1,
    float X2,
    float Y2,
    ModulationFrontType Type,
    string Label);

/// <summary>
/// Layout paramétrico de frentes a partir de <see cref="ModulationStructure.FrontBays"/>.
/// </summary>
public static class ModulationFrontLayout
{
    public static IReadOnlyList<ModulationFrontRect> Layout(
        float moduleWidth,
        float moduleHeight,
        ModulationStructure structure)
    {
        if (structure.FrontBays.Count == 0)
            return Array.Empty<ModulationFrontRect>();

        float gap = structure.FrontGapMm >= 0f ? structure.FrontGapMm : 4f;
        const float inset = 2f;
        float minX = inset;
        float minY = inset;
        float maxX = moduleWidth - inset;
        float maxY = moduleHeight - inset;
        float usableW = Math.Max(0f, maxX - minX);
        float usableH = Math.Max(0f, maxY - minY);

        if (structure.FrontBays.All(bay => bay.Type == ModulationFrontType.Drawer))
            return LayoutDrawers(structure.FrontBays, minX, minY, usableW, usableH, gap);

        return LayoutDoors(structure.FrontBays, minX, minY, usableW, usableH, gap);
    }

    private static IReadOnlyList<ModulationFrontRect> LayoutDrawers(
        IReadOnlyList<ModulationFrontBay> bays,
        float minX,
        float minY,
        float usableW,
        float usableH,
        float gap)
    {
        var rects = new List<ModulationFrontRect>(bays.Count);
        float availableForDrawers = usableH - gap * (bays.Count + 1);
        float y = minY + gap;
        int index = 0;

        foreach (var bay in bays)
        {
            float height = availableForDrawers * bay.HeightFraction;
            float width = usableW * bay.WidthFraction;
            rects.Add(new ModulationFrontRect(
                minX,
                y,
                minX + width,
                y + height,
                bay.Type,
                string.IsNullOrWhiteSpace(bay.Id) ? $"Gaveta {++index}" : bay.Id));
            y += height + gap;
        }

        return rects;
    }

    private static IReadOnlyList<ModulationFrontRect> LayoutDoors(
        IReadOnlyList<ModulationFrontBay> bays,
        float minX,
        float minY,
        float usableW,
        float usableH,
        float gap)
    {
        var rects = new List<ModulationFrontRect>(bays.Count);
        float availableForDoors = usableW - gap * (bays.Count + 1);
        float x = minX + gap;
        int index = 0;

        foreach (var bay in bays)
        {
            float width = availableForDoors * bay.WidthFraction;
            float height = usableH * bay.HeightFraction;
            rects.Add(new ModulationFrontRect(
                x,
                minY,
                x + width,
                minY + height,
                bay.Type,
                string.IsNullOrWhiteSpace(bay.Id) ? $"Porta {++index}" : bay.Id));
            x += width + gap;
        }

        return rects;
    }
}
