using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Wpf;

namespace Tracos3DStudio;

public static class ViewportCaptureService
{
    public static byte[]? CapturePngBytes(GLWpfControl viewport, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return null;

        var pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

        return EncodePng(pixels, width, height);
    }

    public static byte[]? CaptureOffscreen(Action<int, int> renderScene, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return null;

        int framebuffer = GL.GenFramebuffer();
        int colorTexture = GL.GenTexture();
        int depthRenderbuffer = GL.GenRenderbuffer();

        try
        {
            GL.BindTexture(TextureTarget.Texture2D, colorTexture);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba8,
                width,
                height,
                0,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                colorTexture,
                0);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                depthRenderbuffer);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                return null;

            GL.Viewport(0, 0, width, height);
            RenderEngine.EnsureInitialized();
            renderScene(width, height);

            var pixels = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            return EncodePng(pixels, width, height);
        }
        finally
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DeleteFramebuffer(framebuffer);
            GL.DeleteTexture(colorTexture);
            GL.DeleteRenderbuffer(depthRenderbuffer);
        }
    }

    public static bool SaveViewportPng(GLWpfControl viewport, string filePath)
    {
        int width = (int)viewport.ActualWidth;
        int height = (int)viewport.ActualHeight;

        var bytes = CapturePngBytes(viewport, width, height);

        if (bytes == null || bytes.Length == 0)
            return false;

        File.WriteAllBytes(filePath, bytes);
        return true;
    }

    private static byte[]? EncodePng(byte[] pixels, int width, int height)
    {
        FlipRowsVertically(pixels, width, height);

        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);

        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void FlipRowsVertically(byte[] pixels, int width, int height)
    {
        int stride = width * 4;
        var row = new byte[stride];

        for (int y = 0; y < height / 2; y++)
        {
            int top = y * stride;
            int bottom = (height - 1 - y) * stride;
            System.Buffer.BlockCopy(pixels, top, row, 0, stride);
            System.Buffer.BlockCopy(pixels, bottom, pixels, top, stride);
            System.Buffer.BlockCopy(row, 0, pixels, bottom, stride);
        }
    }
}
