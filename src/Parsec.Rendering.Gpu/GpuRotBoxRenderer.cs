using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the rotation-augmented Mandelbox ("rotated fold"). Standard
/// Mandelbox fold set plus three Euler rotation angles (radians) applied each
/// iteration before the folds. The angles are the new, highly generative morph
/// knobs (they compound across iterations). Validated in Python (rotbox_proto.py).
/// </summary>
public sealed record RotBoxParams
{
    public int Iterations { get; init; } = 12;
    public float Scale { get; init; } = -2.0f;
    public float MinRadius { get; init; } = 0.5f;
    public float FixedRadius { get; init; } = 1.0f;
    public float FoldLimit { get; init; } = 1.0f;

    /// <summary>Euler rotation angles in RADIANS, applied each iteration.</summary>
    public Vector3 Rotation { get; init; } = new(0.15f, 0.10f, 0.05f);

    public float Fudge { get; init; } = 0.85f;
    public float BoundRadius { get; init; } = 8.0f;
}

/// <summary>
/// GPU raymarcher for the rotation-augmented Mandelbox. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuRotBoxRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuRotBoxRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("rotbox_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "rotbox");
    }

    public uint[] RenderToBuffer(
        RotBoxParams rb,
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
            Iterations = rb.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(rb.Scale, rb.MinRadius, rb.FixedRadius, rb.FoldLimit),
            SurfParams = new Vector4(rb.Rotation, 0),
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, rb.Fudge),
            BoundSphere = new Vector4(0, 0, 0, rb.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        RotBoxParams rb, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(rb, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuRotBoxRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
