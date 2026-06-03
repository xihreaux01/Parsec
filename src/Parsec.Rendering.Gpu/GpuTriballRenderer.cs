using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the "triball" mined fold fractal: a Mandelbox-family rule
/// found by the fold prospector -- one gentle box fold feeding three nested
/// ball folds (radii 1.29 -> 1.16 -> 1.04), scale 1.82. The fold constants are
/// baked into triball_core.glsl; the live knobs are the affine scale (headline
/// morphology, exactly like the Mandelbox's scale dial), bailout, and DE fudge.
/// </summary>
public sealed record TriballParams
{
    public int Iterations { get; init; } = 14;
    public float Scale { get; init; } = 1.82f;
    public float Bailout { get; init; } = 6.0f;
    public float Fudge { get; init; } = 0.9f;
    public float BoundRadius { get; init; } = 5.0f;
}

/// <summary>
/// GPU raymarcher for the triball fold fractal. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline. Mirrors
/// GpuMandelboxRenderer (same FoldParams packing, linear Mandelbox DE).
/// </summary>
public sealed class GpuTriballRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuTriballRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("triball_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "triball");
    }

    public uint[] RenderToBuffer(
        TriballParams tb,
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
            Iterations = tb.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(tb.Scale, tb.Bailout, 0, 0),
            SurfParams = Vector4.Zero,
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, tb.Fudge),
            BoundSphere = new Vector4(0, 0, 0, tb.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        TriballParams tb, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(tb, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuTriballRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
