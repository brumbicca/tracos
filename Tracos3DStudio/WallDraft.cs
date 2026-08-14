using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class WallDraft
{
    public WallDraftState State { get; private set; } = WallDraftState.Idle;

    public List<Vector2> Points { get; } = new();

    /// <summary>Comprimento digitado (mm) por segmento, na face de referência (Orientação).</summary>
    public List<float> InnerLengthTargets { get; } = new();

    public float Thickness { get; set; } = 150f;

    public float Height { get; set; } = 2600f;

    public WallOrientation Orientation { get; set; } = WallOrientation.Right;

    /// <summary>Orientação Promob: qual lado da parede recebe o Comprimento digitado.</summary>
    public WallMeasureSide MeasureSide { get; set; } = WallMeasureSide.Interior;

    public bool AngleLockEnabled { get; set; } = true;

    public float AngleIncrementDegrees { get; set; } = 45f;

    public float CloseTolerance { get; set; } = 80f;

    public Vector2 PreviewPoint { get; private set; }

    public float PreviewLength
    {
        get
        {
            if (Points.Count == 0)
                return 0f;

            return (PreviewPoint - Points[^1]).Length;
        }
    }

    public float PreviewAngleDegrees
    {
        get
        {
            if (Points.Count == 0)
                return 0f;

            var delta = PreviewPoint - Points[^1];

            if (delta.Length < 0.001f)
                return 0f;

            return MathHelper.RadiansToDegrees(MathF.Atan2(delta.Y, delta.X));
        }
    }

    public bool IsClosed => State == WallDraftState.Closed;

    public event Action? Changed;

    public void Start(Vector2 startPoint)
    {
        Points.Clear();
        InnerLengthTargets.Clear();
        Points.Add(startPoint);
        PreviewPoint = startPoint;
        State = WallDraftState.Drawing;
        Changed?.Invoke();
    }

    public void MovePreview(Vector2 rawPoint)
    {
        if (State != WallDraftState.Drawing || Points.Count == 0)
            return;

        PreviewPoint = AngleLockEnabled
            ? Geometry2D.SnapAngle(Points[^1], rawPoint, AngleIncrementDegrees)
            : rawPoint;

        if (ShouldCloseAt(PreviewPoint))
            PreviewPoint = Points[0];

        Changed?.Invoke();
    }

    /// <param name="innerLengthMm">Comprimento na face de referência (Orientação); null quando confirmado por clique.</param>
    public void ConfirmPoint(Vector2 rawPoint, float? innerLengthMm = null)
    {
        if (State == WallDraftState.Idle)
        {
            Start(rawPoint);
            return;
        }

        if (State != WallDraftState.Drawing)
            return;

        MovePreview(rawPoint);
        var confirmedPoint = PreviewPoint;

        if (ShouldCloseAt(confirmedPoint))
        {
            if (innerLengthMm.HasValue)
                InnerLengthTargets.Add(innerLengthMm.Value);

            CloseSmart();
            return;
        }

        if (!Geometry2D.AlmostEqual(Points[^1], confirmedPoint, 1f))
        {
            if (innerLengthMm.HasValue)
                InnerLengthTargets.Add(innerLengthMm.Value);

            Points.Add(confirmedPoint);
        }

        Changed?.Invoke();
    }

    public void SetLengthAndConfirm(float lengthMm)
    {
        if (State != WallDraftState.Drawing || Points.Count == 0)
            return;

        var direction = PreviewPoint - Points[^1];

        if (direction.Length < 0.001f)
            direction = Vector2.UnitX;

        direction = Vector2.Normalize(direction);
        ConfirmPoint(Points[^1] + direction * lengthMm, lengthMm);
    }

    public void CloseSmart()
    {
        if (Points.Count < 3)
            return;

        State = WallDraftState.Closed;
        PreviewPoint = Points[0];
        Changed?.Invoke();
    }

    public void Cancel()
    {
        Points.Clear();
        InnerLengthTargets.Clear();
        PreviewPoint = Vector2.Zero;
        State = WallDraftState.Cancelled;
        Changed?.Invoke();
    }

    public void Reset()
    {
        Points.Clear();
        InnerLengthTargets.Clear();
        PreviewPoint = Vector2.Zero;
        State = WallDraftState.Idle;
        Changed?.Invoke();
    }

    public bool UndoLastConfirmedPoint()
    {
        if (Points.Count == 0)
            return false;

        if (Points.Count == 1)
        {
            Reset();
            return false;
        }

        if (State == WallDraftState.Closed)
            State = WallDraftState.Drawing;

        Points.RemoveAt(Points.Count - 1);

        if (InnerLengthTargets.Count >= Points.Count)
            InnerLengthTargets.RemoveAt(InnerLengthTargets.Count - 1);

        PreviewPoint = Points[^1];
        Changed?.Invoke();
        return true;
    }

    public List<WallSegment> BuildWalls()
    {
        bool isClosed = State == WallDraftState.Closed && Points.Count >= 3;

        return WallInnerFaceService.BuildWallsFromReferenceCorners(
            Points,
            isClosed,
            Thickness,
            Height,
            Orientation,
            MeasureSide,
            InnerLengthTargets);
    }

    public Room BuildRoom()
    {
        var room = new Room();
        room.SetWalls(BuildWalls());
        return room;
    }

    private bool ShouldCloseAt(Vector2 point)
    {
        if (Points.Count < 3)
            return false;

        return Geometry2D.AlmostEqual(point, Points[0], CloseTolerance);
    }
}
