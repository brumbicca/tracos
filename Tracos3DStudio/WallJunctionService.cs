using OpenTK.Mathematics;

namespace Tracos3DStudio;

public enum WallJunctionKind
{
    Corner,
    T
}

public static class WallJunctionService
{
  public const float MinWallLengthMm = 200f;

  /// <summary>Tolerância colinear ao eixo (mm) — apenas para verificar que o ponto pertence ao segmento.</summary>
  private const float AxisColinearToleranceMm = 1f;

  public static bool TryApplyCornerJoin(WallSegment moving, WallSegment other, out bool adjustedStart)
  {
    adjustedStart = false;

    if (!TryComputeCornerJoin(moving, other, out Vector2 intersection, out adjustedStart))
      return false;

    if (adjustedStart)
      moving.Start = intersection;
    else
      moving.End = intersection;

    return moving.Length >= MinWallLengthMm;
  }

  public static bool TryApplyTJoin(WallSegment moving, WallSegment through)
  {
    if (!TryComputeTJoin(moving, through, out Vector2 intersection, out bool adjustStart))
      return false;

    if (adjustStart)
      moving.Start = intersection;
    else
      moving.End = intersection;

    return moving.Length >= MinWallLengthMm;
  }

  public static bool TryPickSecondWall(
      WallSegment firstWall,
      WallSegment candidate,
      WallJunctionKind kind)
  {
    if (firstWall.Id == candidate.Id)
      return false;

    return kind == WallJunctionKind.Corner
        ? TryComputeCornerJoin(firstWall, candidate, out _, out _)
        : TryComputeTJoin(firstWall, candidate, out _, out _);
  }

  /// <summary>
  /// Canto: eixos se cruzam no segmento da segunda parede; a primeira estende/encurta até o cruzamento.
  /// </summary>
  private static bool TryComputeCornerJoin(
      WallSegment moving,
      WallSegment other,
      out Vector2 intersection,
      out bool adjustStart)
  {
    intersection = Vector2.Zero;
    adjustStart = false;

    if (!Geometry2D.TryLineIntersection(
            moving.Start, moving.End,
            other.Start, other.End,
            out intersection))
      return false;

    if (!IsPointOnAxisSegment(intersection, other.Start, other.End))
      return false;

    float distStart = (moving.Start - intersection).LengthSquared;
    float distEnd = (moving.End - intersection).LengthSquared;
    adjustStart = distStart <= distEnd;

    float newLength = adjustStart
        ? (moving.End - intersection).Length
        : (intersection - moving.Start).Length;

    return newLength >= MinWallLengthMm;
  }

  /// <summary>
  /// Encontro T: eixos se cruzam no segmento da parede de passagem; a parede móvel estende até o cruzamento.
  /// </summary>
  private static bool TryComputeTJoin(
      WallSegment moving,
      WallSegment through,
      out Vector2 intersection,
      out bool adjustStart)
  {
    intersection = Vector2.Zero;
    adjustStart = false;

    if (!Geometry2D.TryLineIntersection(
            moving.Start, moving.End,
            through.Start, through.End,
            out intersection))
      return false;

    if (!IsPointOnAxisSegment(intersection, through.Start, through.End))
      return false;

    float distStart = (moving.Start - intersection).LengthSquared;
    float distEnd = (moving.End - intersection).LengthSquared;
    adjustStart = distStart <= distEnd;

    float newLength = adjustStart
        ? (moving.End - intersection).Length
        : (intersection - moving.Start).Length;

    return newLength >= MinWallLengthMm;
  }

  private static bool IsPointOnAxisSegment(Vector2 point, Vector2 segStart, Vector2 segEnd)
  {
    Vector2 seg = segEnd - segStart;
    float lenSq = seg.LengthSquared;

    if (lenSq < 0.001f)
      return Geometry2D.AlmostEqual(point, segStart, AxisColinearToleranceMm);

    float t = Vector2.Dot(point - segStart, seg) / lenSq;

    if (t < 0f || t > 1f)
      return false;

    Vector2 projection = segStart + seg * t;
    return Geometry2D.AlmostEqual(point, projection, AxisColinearToleranceMm);
  }
}
