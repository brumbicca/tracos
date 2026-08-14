using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class SelectableFace
{
    public Guid OwnerId { get; init; }

    public FaceKind Kind { get; init; }

    public string Label { get; init; } = string.Empty;

    public int TriangleStartIndex { get; init; }

    public int TriangleCount { get; init; }

    public Vector3[] Vertices { get; init; } = Array.Empty<Vector3>();

    public Vector3 Normal { get; init; }

    public Vector3 Center
    {
        get
        {
            if (Vertices.Length == 0)
                return Vector3.Zero;

            var sum = Vector3.Zero;

            foreach (var v in Vertices)
                sum += v;

            return sum / Vertices.Length;
        }
    }
}