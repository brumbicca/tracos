using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public static class RenderEngine
{
    private static bool _initialized;
    private static GlShaderProgram? _shader;
    private static int _vao;
    private static int _vbo;
    private static Matrix4 _viewProjection = Matrix4.Identity;

    private static readonly List<ColoredVertex> TriangleBatch = new();
    private static readonly List<ColoredVertex> LineBatch = new();

    private static Vector4 _currentColor = Vector4.One;
    private static bool _triangleBatchOpen;
    private static bool _lineBatchOpen;

    private static readonly int VertexStride = Marshal.SizeOf<ColoredVertex>();
    private static readonly int ColorOffset = Marshal.OffsetOf<ColoredVertex>(nameof(ColoredVertex.Cr)).ToInt32();

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _shader = new GlShaderProgram(GlShaderSources.ColoredVertex330, GlShaderSources.ColoredFragment330);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexStride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, VertexStride, ColorOffset);

        GL.BindVertexArray(0);

        _initialized = true;
    }

    public static void PrepareFrame(int width, int height)
    {
        EnsureInitialized();

        GL.Viewport(0, 0, width, height);
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.23f, 0.23f, 0.23f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public static void SetViewProjection(Matrix4 view, Matrix4 projection)
    {
        // OpenTK stores matrices row-major. GL.UniformMatrix4(transpose=false) passes bytes
        // directly → OpenGL interprets as column-major (effectively transposing the OpenTK matrix).
        // The fixed-function GL.LoadMatrix did the same transposition, so the effective chain was
        // T(P) * T(V) * pos = T(V*P) * pos. Therefore _viewProjection = view * projection so that
        // GLSL uMvp = T(view * projection) = T(P) * T(V), matching the legacy pipeline.
        _viewProjection = view * projection;
    }

    public static void Color3(float r, float g, float b) => _currentColor = new Vector4(r, g, b, 1f);

    public static void Color4(float r, float g, float b, float a) => _currentColor = new Vector4(r, g, b, a);

    public static void BeginTriangleBatch() => _triangleBatchOpen = true;

    public static void BeginLineBatch() => _lineBatchOpen = true;

    public static void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector4? color = null)
    {
        var c4 = color ?? _currentColor;
        TriangleBatch.Add(new ColoredVertex(a, c4));
        TriangleBatch.Add(new ColoredVertex(b, c4));
        TriangleBatch.Add(new ColoredVertex(c, c4));
    }

    public static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4? color = null)
    {
        Triangle(a, b, c, color);
        Triangle(a, c, d, color);
    }

    public static void QuadDouble(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4? color = null)
    {
        Quad(a, b, c, d, color);
        Quad(d, c, b, a, color);
    }

    public static void Line(Vector3 a, Vector3 b, Vector4? color = null)
    {
        var c4 = color ?? _currentColor;
        LineBatch.Add(new ColoredVertex(a, c4));
        LineBatch.Add(new ColoredVertex(b, c4));
    }

    public static void Line(Vector2 a, Vector2 b, float y, Vector4? color = null)
    {
        Line(new Vector3(a.X, y, a.Y), new Vector3(b.X, y, b.Y), color);
    }

    public static void LineLoop(IReadOnlyList<Vector3> vertices, Vector4? color = null)
    {
        if (vertices.Count < 2)
            return;

        for (int i = 0; i < vertices.Count; i++)
        {
            int next = (i + 1) % vertices.Count;
            Line(vertices[i], vertices[next], color);
        }
    }

    public static void PointMarker(Vector3 center, float size, Vector4? color = null)
    {
        var c4 = color ?? _currentColor;
        float half = size * 0.5f;
        Quad(
            center + new Vector3(-half, 0, -half),
            center + new Vector3(half, 0, -half),
            center + new Vector3(half, 0, half),
            center + new Vector3(-half, 0, half),
            c4);
    }

    public static void EndTriangleBatch(
        bool depthTest = true,
        bool depthMask = true,
        bool blend = false,
        bool cullFace = false,
        bool polygonOffsetFill = false,
        float polygonOffsetFactor = 1f,
        float polygonOffsetUnits = 1f)
    {
        _triangleBatchOpen = false;

        if (TriangleBatch.Count == 0)
            return;

        if (depthTest)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        GL.DepthMask(depthMask);

        if (blend)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else
        {
            GL.Disable(EnableCap.Blend);
        }

        if (cullFace)
            GL.Enable(EnableCap.CullFace);
        else
            GL.Disable(EnableCap.CullFace);

        if (polygonOffsetFill)
        {
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(polygonOffsetFactor, polygonOffsetUnits);
        }

        DrawBatch(PrimitiveType.Triangles, TriangleBatch);
        TriangleBatch.Clear();

        GL.Disable(EnableCap.PolygonOffsetFill);
    }

    public static void EnableDepthTest() => GL.Enable(EnableCap.DepthTest);

    public static void EndLineBatch(
        float lineWidth,
        bool depthTest = true,
        bool polygonOffsetLine = false,
        float polygonOffsetFactor = -1f,
        float polygonOffsetUnits = -1f)
    {
        _lineBatchOpen = false;

        if (LineBatch.Count == 0)
            return;

        if (depthTest)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        if (polygonOffsetLine)
        {
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(polygonOffsetFactor, polygonOffsetUnits);
        }

        GL.LineWidth(lineWidth);
        DrawBatch(PrimitiveType.Lines, LineBatch);
        LineBatch.Clear();

        GL.Disable(EnableCap.PolygonOffsetLine);
    }

    public static void FlushOpenBatches()
    {
        if (_triangleBatchOpen)
            EndTriangleBatch();

        if (_lineBatchOpen)
            EndLineBatch(1f);
    }

    private static void DrawBatch(PrimitiveType primitiveType, List<ColoredVertex> vertices)
    {
        if (_shader == null || vertices.Count == 0)
            return;

        _shader.Use();
        _shader.SetMvp(_viewProjection);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        var array = vertices.ToArray();
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            array.Length * VertexStride,
            array,
            BufferUsageHint.DynamicDraw);

        GL.DrawArrays(primitiveType, 0, array.Length);
        GL.BindVertexArray(0);
    }

}
