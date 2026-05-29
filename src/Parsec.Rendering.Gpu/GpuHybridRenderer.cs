using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the rotated Mandelbox + Mandelbulb hybrid. Both operations
/// run every iteration in sequence, with a compounding rotation woven in. The
/// DE is heuristic (no proven lower-bound for hybrids) with a safety factor
/// baked into the shader; validated as renderable in Python (hybrid_proto.py).
/// </summary>
public sealed record HybridParams
{
    public int Iterations { get; init; } = 8;
    public float Scale { get; init; } = -1.8f;
    public float MinRadius { get; init; } = 0.5f;
    public float FixedRadius { get; init; } = 1.0f;
    public float FoldLimit { get; init; } = 1.0f;
    public float Power { get; init; } = 2.0f;

    /// <summary>Euler rotation angles in RADIANS, applied each iteration.</summary>
    public Vector3 Rotation { get; init; } = new(0.12f, 0.08f, 0.04f);

    public float Fudge { get; init; } = 0.6f;
    public float BoundRadius { get; init; } = 4.0f;
}

/// <summary>
/// GPU raymarcher for the hybrid Mandelbox+Mandelbulb. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuHybridRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuHybridRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("hybrid_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "hybrid");
    }

    public uint[] RenderToBuffer(
        HybridParams hp,
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
            Iterations = hp.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(hp.Scale, hp.MinRadius, hp.FixedRadius, hp.FoldLimit),
            SurfParams = new Vector4(hp.Rotation, hp.Power),
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, hp.Fudge),
            BoundSphere = new Vector4(0, 0, 0, hp.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        HybridParams hp, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(hp, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuHybridRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
