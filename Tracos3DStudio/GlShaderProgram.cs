using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Tracos3DStudio;

public sealed class GlShaderProgram : IDisposable
{
    private readonly int _program;

    public int MvpLocation { get; }

    public GlShaderProgram(string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _program = GL.CreateProgram();
        GL.AttachShader(_program, vertexShader);
        GL.AttachShader(_program, fragmentShader);
        GL.LinkProgram(_program);

        GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string info = GL.GetProgramInfoLog(_program);
            throw new InvalidOperationException($"Falha ao linkar shader: {info}");
        }

        GL.DetachShader(_program, vertexShader);
        GL.DetachShader(_program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        MvpLocation = GL.GetUniformLocation(_program, "uMvp");
        if (MvpLocation < 0)
            throw new InvalidOperationException("Uniform uMvp não encontrado no shader.");
    }

    public void Use() => GL.UseProgram(_program);

    public void SetMvp(Matrix4 mvp) => GL.UniformMatrix4(MvpLocation, false, ref mvp);

    public void Dispose()
    {
        if (_program != 0)
            GL.DeleteProgram(_program);
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Falha ao compilar {type}: {info}");
        }

        return shader;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ColoredVertex
{
    public readonly float Px;
    public readonly float Py;
    public readonly float Pz;
    public readonly float Cr;
    public readonly float Cg;
    public readonly float Cb;
    public readonly float Ca;

    public ColoredVertex(Vector3 position, Vector4 color)
    {
        Px = position.X;
        Py = position.Y;
        Pz = position.Z;
        Cr = color.X;
        Cg = color.Y;
        Cb = color.Z;
        Ca = color.W;
    }
}

public static class GlShaderSources
{
    public const string ColoredVertex330 = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec4 aColor;

        uniform mat4 uMvp;

        out vec4 vColor;

        void main()
        {
            vColor = aColor;
            gl_Position = uMvp * vec4(aPosition, 1.0);
        }
        """;

    public const string ColoredFragment330 = """
        #version 330 core

        in vec4 vColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vColor;
        }
        """;
}
