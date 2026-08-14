using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class TechnicalDrawingService
{
  private const float DimensionOffsetMm = 280f;

  public static TechnicalDrawingSet Build(Project project)
  {
    var walls = new List<TechnicalDrawingLine>();
    var floorModules = new List<TechnicalDrawingRect>();
    var floorDimensions = new List<TechnicalDrawingDimension>();

    float minX = float.MaxValue;
    float minY = float.MaxValue;
    float maxX = float.MinValue;
    float maxY = float.MinValue;

    foreach (var wall in project.Room.Walls)
    {
      walls.Add(new TechnicalDrawingLine
      {
        X1 = wall.Start.X,
        Y1 = wall.Start.Y,
        X2 = wall.End.X,
        Y2 = wall.End.Y
      });

      ExpandBounds(wall.Start.X, wall.Start.Y, ref minX, ref minY, ref maxX, ref maxY);
      ExpandBounds(wall.End.X, wall.End.Y, ref minX, ref minY, ref maxX, ref maxY);

      var refFace = WallInnerFaceService.GetReferenceFace(wall, project.Room.Walls);
      Vector2 dimStart = refFace.InnerStart + refFace.InteriorNormal * DimensionOffsetMm;
      Vector2 dimEnd = refFace.InnerEnd + refFace.InteriorNormal * DimensionOffsetMm;

      floorDimensions.Add(new TechnicalDrawingDimension
      {
        X1 = dimStart.X,
        Y1 = dimStart.Y,
        X2 = dimEnd.X,
        Y2 = dimEnd.Y,
        Text = $"{WallInnerFaceService.GetDisplayReferenceLength(wall, project.Room.Walls):0}"
      });

      ExpandBounds(dimStart.X, dimStart.Y, ref minX, ref minY, ref maxX, ref maxY);
      ExpandBounds(dimEnd.X, dimEnd.Y, ref minX, ref minY, ref maxX, ref maxY);
    }

    foreach (var module in project.Modules)
    {
      var (boundsMin, boundsMax) = module.GetBounds();
      float x = boundsMin.X;
      float y = boundsMin.Z;
      float w = boundsMax.X - boundsMin.X;
      float h = boundsMax.Z - boundsMin.Z;

      var definition = ModuleCatalog.GetRequired(module.DefinitionId);
      floorModules.Add(new TechnicalDrawingRect
      {
        X = x,
        Y = y,
        Width = w,
        Height = h,
        Label = definition.DisplayName
      });

      ExpandBounds(x, y, ref minX, ref minY, ref maxX, ref maxY);
      ExpandBounds(x + w, y + h, ref minX, ref minY, ref maxX, ref maxY);
    }

    if (minX == float.MaxValue)
    {
      minX = 0;
      minY = 0;
      maxX = 1000;
      maxY = 1000;
    }

    var elevations = BuildElevations(project);

    return new TechnicalDrawingSet
    {
      FloorPlanWalls = walls,
      FloorPlanModules = floorModules,
      FloorPlanDimensions = floorDimensions,
      Elevations = elevations,
      MinX = minX,
      MinY = minY,
      MaxX = maxX,
      MaxY = maxY
    };
  }

  private static List<TechnicalElevation> BuildElevations(Project project)
  {
    var groups = new Dictionary<int, List<ModuleInstance>>();

    foreach (var module in project.Modules)
    {
      int bucket = NormalizeRotation(module.RotationYDegrees);
      if (!groups.TryGetValue(bucket, out var list))
      {
        list = new List<ModuleInstance>();
        groups[bucket] = list;
      }

      list.Add(module);
    }

    var elevations = new List<TechnicalElevation>();

    foreach (var (rotation, modules) in groups.OrderBy(kv => kv.Key))
    {
      var rects = new List<TechnicalDrawingRect>();
      var dimensions = new List<TechnicalDrawingDimension>();

      foreach (var module in modules)
      {
        var (min, max) = module.GetBounds();
        var (x, w, y, h) = ProjectElevationRect(min, max, rotation);
        var definition = ModuleCatalog.GetRequired(module.DefinitionId);

        rects.Add(new TechnicalDrawingRect
        {
          X = x,
          Y = y,
          Width = w,
          Height = h,
          Label = definition.DisplayName
        });

        dimensions.Add(new TechnicalDrawingDimension
        {
          X1 = x,
          Y1 = y + h + 120f,
          X2 = x + w,
          Y2 = y + h + 120f,
          Text = $"{module.Width:0}"
        });

        dimensions.Add(new TechnicalDrawingDimension
        {
          X1 = x - 120f,
          Y1 = y,
          X2 = x - 120f,
          Y2 = y + h,
          Text = $"{module.Height:0}"
        });
      }

      elevations.Add(new TechnicalElevation
      {
        Title = $"Elevação ({rotation}°)",
        Modules = rects,
        Dimensions = dimensions
      });
    }

    return elevations;
  }

  private static (float X, float Width, float Y, float Height) ProjectElevationRect(
    Vector3 min,
    Vector3 max,
    int rotation)
  {
    return rotation switch
    {
      90 => (min.Z, max.Z - min.Z, min.Y, max.Y - min.Y),
      180 => (min.X, max.X - min.X, min.Y, max.Y - min.Y),
      270 => (min.Z, max.Z - min.Z, min.Y, max.Y - min.Y),
      _ => (min.X, max.X - min.X, min.Y, max.Y - min.Y)
    };
  }

  private static int NormalizeRotation(float degrees)
  {
    float normalized = (degrees % 360f + 360f) % 360f;
    int bucket = (int)MathF.Round(normalized / 90f) * 90;
    return bucket % 360;
  }

  private static void ExpandBounds(
    float x,
    float y,
    ref float minX,
    ref float minY,
    ref float maxX,
    ref float maxY)
  {
    minX = Math.Min(minX, x);
    minY = Math.Min(minY, y);
    maxX = Math.Max(maxX, x);
    maxY = Math.Max(maxY, y);
  }
}
