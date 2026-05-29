using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the pseudo-Kleinian inversive limit set. Box fold + sphere
/// inversion iterated into a 3D Apollonian foam. The DE is computed by a
/// NUMERICAL gradient inside the shader (the analytic derivative is unstable for
/// inversive systems), validated in Python (kleinian_numgrad.py). The offset is
/// what generates the limit set; scale ~2 with radii 0.5/1.0 is the foam regime.
/// </summary>
public sealed record KleinianParams
{
    public int Iterations { get; init; } = 9;

    public float Scale { get; init; } = 2.0f;
    public float Cell { get; init; } = 1.0f;
    public float MinRadius { get; init; } = 0.5f;
    public float FixedRadius { get; init; } = 1.0f;

    /// <summary>The tiling offset that generates the limit set.</summary>
    public Vector3 Offset { get; init; } = new(0.5f, 0.5f, 1.2f);

    /// <summary>DE step fudge in (0,1]. Numerical-gradient DEs overshoot, so keep low.</summary>
    public float Fudge { get; init; } = 0.7f;

    public float BoundRadius { get; init; } = 6.0f;
}

/// <summary>
/// GPU raymarcher for the pseudo-Kleinian family. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline. DE core is
/// kleinian_core.glsl (a numerical-gradient estimate).
/// </summary>
public sealed class GpuKleinianRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuKleinianRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("kleinian_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "kleinian");
    }

    public uint[] RenderToBuffer(
        KleinianParams kl,
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

        // Pack into the shared FoldParams slots (see kleinian_core.glsl):
        //   boxParams = (scale, cell, minRadius, fixedRadius)
        //   juliaC    = (offset.xyz, _)
        //   rot       = (_, _, _, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = kl.Iterations,
            Mode = 0,
            JuliaMode = 0,
            Pad0 = 0,
            BoxParams = new Vector4(kl.Scale, kl.Cell, kl.MinRadius, kl.FixedRadius),
            SurfParams = Vector4.Zero,
            JuliaCVec = new Vector4(kl.Offset, 0f),
            Rot = new Vector4(0f, 0f, 0f, kl.Fudge),
            BoundSphere = new Vector4(0, 0, 0, kl.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        KleinianParams kl, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(kl, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuKleinianRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
