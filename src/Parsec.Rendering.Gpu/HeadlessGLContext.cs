using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// A headless OpenGL 4.3 context wrapped around an invisible OpenTK
/// <c>NativeWindow</c>. Suitable for compute-only workloads where we never
/// want to show pixels on screen — we just want to dispatch compute shaders
/// and read back buffers.
/// </summary>
/// <remarks>
/// <para>
/// OpenGL contexts are inherently bound to a "window" on every platform; even
/// "headless" GL is conceptually a hidden window. OpenTK's <c>NativeWindow</c>
/// gives us the simplest cross-platform way to obtain a current GL context
/// without setting up EGL/GLX directly. We create the window 1x1, hidden, and
/// keep it alive for the duration of the GPU session.
/// </para>
/// <para>
/// OpenTK is used here ONLY to create the context and provide a proc-address
/// loader. All actual GL calls go through <see cref="Gl"/>, the unified
/// GetProcAddress layer shared with the Avalonia app — there are no OpenTK
/// <c>GL.*</c> calls anywhere in Parsec.
/// </para>
/// <para>
/// This class is <em>not</em> thread-safe: OpenGL contexts are bound to a
/// specific thread, and all GL calls must happen on the thread that called
/// <see cref="MakeCurrent"/>.
/// </para>
/// </remarks>
public sealed class HeadlessGLContext : IDisposable
{
    private readonly NativeWindow _window;
    private bool _disposed;

    /// <summary>The unified GL wrapper bound to this context.</summary>
    public Gl Gl { get; }

    public HeadlessGLContext()
    {
        var settings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1, 1),
            Title = "Parsec headless",
            StartVisible = false,
            StartFocused = false,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 3),
            Flags = ContextFlags.ForwardCompatible,
        };
        _window = new NativeWindow(settings);
        _window.MakeCurrent();

        // Build the unified GL layer from GLFW's proc-address loader. This is
        // the same Gl class the Avalonia app builds from GlInterface.GetProcAddress;
        // only the loader source differs.
        Gl = new Gl(GLFW.GetProcAddress);
    }

    /// <summary>
    /// Bind this context to the current thread.
    /// </summary>
    public void MakeCurrent()
    {
        ThrowIfDisposed();
        _window.MakeCurrent();
    }

    /// <summary>
    /// OpenGL vendor / renderer / version strings — useful for confirming we
    /// got the context we expected.
    /// </summary>
    public string Info()
    {
        ThrowIfDisposed();
        var vendor = Gl.GetString(GlConst.Vendor);
        var renderer = Gl.GetString(GlConst.Renderer);
        var version = Gl.GetString(GlConst.Version);
        var glsl = Gl.GetString(GlConst.ShadingLanguageVersion);
        return $"Vendor: {vendor}\nRenderer: {renderer}\nVersion: {version}\nGLSL: {glsl}";
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HeadlessGLContext));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _window.Dispose();
    }
}
