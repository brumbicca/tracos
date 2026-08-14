using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class MeshData
{
    public List<Vector3> Vertices { get; } = new();

    public List<int> Indices { get; } = new();

    public List<Vector3> Normals { get; } = new();

    public List<Vector2> Uv { get; } = new();

    public List<SelectableFace> Faces { get; } = new();

    public void Clear()
    {
        Vertices.Clear();
        Indices.Clear();
        Normals.Clear();
        Uv.Clear();
        Faces.Clear();
    }

    public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Vertices.Add(position);
        Normals.Add(normal);
        Uv.Add(uv);

        return Vertices.Count - 1;
    }

    public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, FaceKind kind, Guid ownerId, string label = "")
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        var start = Vertices.Count;

        AddVertex(a, normal, new Vector2(0, 0));
        AddVertex(b, normal, new Vector2(1, 0));
        AddVertex(c, normal, new Vector2(1, 1));
        AddVertex(d, normal, new Vector2(0, 1));

        Indices.Add(start + 0);
        Indices.Add(start + 1);
        Indices.Add(start + 2);
        Indices.Add(start + 0);
        Indices.Add(start + 2);
        Indices.Add(start + 3);

        Faces.Add(new SelectableFace
        {
            OwnerId = ownerId,
            Kind = kind,
            Label = label,
            TriangleStartIndex = Indices.Count - 6,
            TriangleCount = 2,
            Vertices = new[] { a, b, c, d },
            Normal = normal
        });
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, FaceKind kind, Guid ownerId, string label = "")
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        var start = Vertices.Count;

        AddVertex(a, normal, new Vector2(0, 0));
        AddVertex(b, normal, new Vector2(1, 0));
        AddVertex(c, normal, new Vector2(1, 1));

        Indices.Add(start + 0);
        Indices.Add(start + 1);
        Indices.Add(start + 2);

        Faces.Add(new SelectableFace
        {
            OwnerId = ownerId,
            Kind = kind,
            Label = label,
            TriangleStartIndex = Indices.Count - 3,
            TriangleCount = 1,
            Vertices = new[] { a, b, c },
            Normal = normal
        });
    }

    /// <summary>
    /// Face com contorno arbitrário (arestas = edgeLoop) e triangulação própria.
    /// Usado para peças L inteiras: o LineLoop desenha só o perímetro, sem emenda interna.
    /// </summary>
    public void AddPolygonalFace(
        Vector3[] edgeLoop,
        IReadOnlyList<(Vector3 A, Vector3 B, Vector3 C)> triangles,
        FaceKind kind,
        Guid ownerId,
        string label = "")
    {
        if (edgeLoop.Length < 3 || triangles.Count == 0)
            return;

        var (a0, b0, c0) = triangles[0];
        var normal = Vector3.Normalize(Vector3.Cross(b0 - a0, c0 - a0));
        int triStart = Indices.Count;

        foreach (var (a, b, c) in triangles)
        {
            int start = Vertices.Count;
            AddVertex(a, normal, new Vector2(0, 0));
            AddVertex(b, normal, new Vector2(1, 0));
            AddVertex(c, normal, new Vector2(1, 1));
            Indices.Add(start + 0);
            Indices.Add(start + 1);
            Indices.Add(start + 2);
        }

        Faces.Add(new SelectableFace
        {
            OwnerId = ownerId,
            Kind = kind,
            Label = label,
            TriangleStartIndex = triStart,
            TriangleCount = triangles.Count,
            Vertices = edgeLoop,
            Normal = normal
        });
    }
}