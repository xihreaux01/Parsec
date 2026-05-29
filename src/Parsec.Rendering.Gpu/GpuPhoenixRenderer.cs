using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the 3D Phoenix Julia. See phoenix_core.glsl for the math
/// (Ushiki's z^2 + c + p*z_prev lifted to 3D via Mandelbulb-style trig; DE
/// via scalar-derivative tracking, same family as Mandelbulb).
/// </summary>
public sealed record PhoenixParams
{
    public int Iterations { get; init; } = 14;
    public Vector3 C { get; init; } = new(0.4f, 0.0f, 0.0f);
    public float PMem { get; init; } = -0.5f;
    public float Bailout { get; init; } = 4.0f;
    public bool Cut { get; init; } = true;
    public Vector3 PlaneNormal { get; init; } = new(0.3f, 0.5f, 0.8f);
    public float PlaneOffset { get; init; } = 0.0f;
    public float Fudge { get; init; } = 0.85f;
    public float BoundRadius { get; init; } = 4.0f;
}

/// <summary>
/// GPU raymarcher for Phoenix. Owns only the per-fractal compute shader;
/// the SSBOs and the tile/AA loop live in the shared RaymarchPipeline.
///
/// (Migrated from the old self-contained pattern -- previously this class
/// had ~200 lines of buffer management duplicated across 14 renderers; the
/// pipeline cleanup lifted all of it out, leaving each renderer responsible
/// only for packing its fractal-specific params into FoldParamsGpu.)
/// </summary>
public sealed class GpuPhoenixRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuPhoenixRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("phoenix_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "phoenix");
    }

    public uint[] RenderToBuffer(
        PhoenixParams ph,
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
        var foldParams = new FoldParamsGpu
        {
            Iterations = ph.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams  = new Vector4(ph.PMem, ph.PlaneOffset, ph.Bailout, ph.Cut ? 1f : 0f),
            SurfParams = new Vector4(Vector3.Normalize(ph.PlaneNormal), 0),
            JuliaCVec  = new Vector4(ph.C, 0),
            Rot        = new Vector4(0, 0, 0, ph.Fudge),
            BoundSphere = new Vector4(0, 0, 0, ph.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        PhoenixParams ph, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(ph, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuPhoenixRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
