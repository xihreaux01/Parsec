using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the Riemann Sphere (Msltoe) fractal. An escape-time map that
/// projects onto a sphere, stereographically maps to the plane, sine-folds the
/// plane coordinates (the organic, coral/cellular generator), runs a variable-
/// exponent radial power map, and inverse-projects. The source has no analytic
/// DE; because the radial part collapses to |z| -> |z|^(2p) - 0.25 it is
/// Mandelbulb-class, so riemann_sphere_core.glsl uses a scalar-derivative DE
/// (approximate). Validated in Python (riemann_proto.py) before this port.
/// </summary>
public sealed record RiemannSphereParams
{
    public int Iterations { get; init; } = 20;

    /// <summary>0 = Mandelbrot (c = position), 1 = Julia (c = JuliaC).</summary>
    public int JuliaMode { get; init; } = 0;

    /// <summary>Sphere projection radius (z is normalized to this each step).</summary>
    public float Scale { get; init; } = 1.0f;
    /// <summary>Sine-fold phase offset on the projected s coordinate.</summary>
    public float OffsetA { get; init; } = 0.4f;
    /// <summary>Sine-fold phase offset on the projected t coordinate.</summary>
    public float OffsetB { get; init; } = 0.7f;

    /// <summary>
    /// Escape bailout radius. MUST stay low (~2): the exponent 2p reaches ~72,
    /// so r^(2p) overflows a float by r=4. The set lives near r&lt;=1 anyway.
    /// </summary>
    public float Bailout { get; init; } = 2.0f;
    /// <summary>Upper clamp on the variable exponent p (pow() guard; source uses 36).</summary>
    public float PowerClamp { get; init; } = 36.0f;

    /// <summary>Julia constant (the additive c), used when JuliaMode == 1.</summary>
    public Vector3 JuliaC { get; init; } = Vector3.Zero;
    /// <summary>Optional pre-rotation applied each iteration (radians).</summary>
    public Vector3 RotationRadians { get; init; } = Vector3.Zero;

    /// <summary>DE step fudge in (0,1]. The DE is approximate here, so a lower
    /// value (more conservative stepping) is the first knob if it looks faceted.</summary>
    public float Fudge { get; init; } = 0.6f;
    /// <summary>Bounding sphere radius around the origin for the marcher fast-skip.</summary>
    public float BoundRadius { get; init; } = 3.0f;
}

/// <summary>
/// GPU raymarcher for the Riemann Sphere (Msltoe) escape-time fractal. Owns only
/// its compute shader; shared buffers and the tile/AA loop live in
/// RaymarchPipeline. Its parameters pack into the shared FoldParams slots
/// reinterpreted by riemann_sphere_core.glsl (see that file's header).
/// </summary>
public sealed class GpuRiemannSphereRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuRiemannSphereRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("riemann_sphere_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "riemann_sphere");
    }

    /// <summary>Render and return the raw RGBA8 pixel buffer (one uint per pixel).</summary>
    public uint[] RenderToBuffer(
        RiemannSphereParams rs,
        Camera3D camera,
        int width, int height,
        RaymarchSettings settings,
        Color background, Color surface,
        Vector3 lightDirection,
        PaletteParams palette,
        int tileRows = 32,
        Action<int, int>? progress = null)
    {
        ThrowIfDisposed();

        // Pack into the shared FoldParams slots (see riemann_sphere_core.glsl):
        //   boxParams  = (scale, offsetA0, offsetB0, bailout)
        //   surfParams = (powerClamp, _, _, _)
        //   juliaC     = (cx, cy, cz, _)
        //   rot        = (rot.xyz, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = rs.Iterations,
            Mode = 0,
            JuliaMode = rs.JuliaMode,
            Pad0 = 0,
            BoxParams = new Vector4(rs.Scale, rs.OffsetA, rs.OffsetB, rs.Bailout),
            SurfParams = new Vector4(rs.PowerClamp, 0f, 0f, 0f),
            JuliaCVec = new Vector4(rs.JuliaC, 0f),
            Rot = new Vector4(rs.RotationRadians, rs.Fudge),
            BoundSphere = new Vector4(0, 0, 0, rs.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    /// <summary>Render and return an SKBitmap (for the headless/file path).</summary>
    public SKBitmap Render(
        RiemannSphereParams rs, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(rs, camera, width, height, settings,
            background, surface, lightDirection, palette, tileRows, progress);
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        var bytes = new byte[pixels.Length * 4];
        System.Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        Marshal.Copy(bytes, 0, bitmap.GetPixels(), bytes.Length);
        return bitmap;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GpuRiemannSphereRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
