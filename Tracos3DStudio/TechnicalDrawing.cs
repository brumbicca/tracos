namespace Tracos3DStudio;

public sealed class TechnicalDrawingLine
{
    public float X1 { get; init; }

    public float Y1 { get; init; }

    public float X2 { get; init; }

    public float Y2 { get; init; }
}

public sealed class TechnicalDrawingRect
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public string? Label { get; init; }
}

public sealed class TechnicalDrawingDimension
{
    public float X1 { get; init; }

    public float Y1 { get; init; }

    public float X2 { get; init; }

    public float Y2 { get; init; }

    public required string Text { get; init; }
}

public sealed class TechnicalElevation
{
    public required string Title { get; init; }

    public required IReadOnlyList<TechnicalDrawingRect> Modules { get; init; }

    public required IReadOnlyList<TechnicalDrawingDimension> Dimensions { get; init; }
}

public sealed class TechnicalDrawingSet
{
    public required IReadOnlyList<TechnicalDrawingLine> FloorPlanWalls { get; init; }

    public required IReadOnlyList<TechnicalDrawingRect> FloorPlanModules { get; init; }

    public required IReadOnlyList<TechnicalDrawingDimension> FloorPlanDimensions { get; init; }

    public required IReadOnlyList<TechnicalElevation> Elevations { get; init; }

    public float MinX { get; init; }

    public float MinY { get; init; }

    public float MaxX { get; init; }

    public float MaxY { get; init; }
}
