using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the canonical White/Nylander Mandelbulb (z -> z^n + c in
/// spherical coordinates). Power is the headline morphology knob; 8 is classic.
/// </summary>
public sealed record MandelbulbParams
{
    public int Iterations { get; init; } = 8;
    public float Power { get; init; } = 8.0f;
    public float Bailout { get; init; } = 2.0f;
    public float Fudge { get; init; } = 1.0f;
    public float BoundRadius { get; init; } = 1.3f;
}

/// <summary>
/// GPU raymarcher for the Mandelbulb. Owns only its compute shader; shared
/// buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuMandelbulbRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuMandelbulbRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("mandelbulb_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "mandelbulb");
    }

    public uint[] RenderToBuffer(
        MandelbulbParams mb,
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
            Iterations = mb.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(mb.Power, mb.Bailout, 0, 0),
            SurfParams = Vector4.Zero,
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, mb.Fudge),
            BoundSphere = new Vector4(0, 0, 0, mb.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        MandelbulbParams mb, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(mb, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuMandelbulbRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
