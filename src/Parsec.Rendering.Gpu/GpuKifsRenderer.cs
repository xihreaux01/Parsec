using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the Kaleidoscopic IFS ("Amazing IFS") fold fractal. This is
/// an escape-time system like the Mandelbox (despite the "IFS" name): each
/// iteration rotates, plane-folds (abs), rotates again, sphere-folds, and
/// scales toward a pivot. The post-fold rotation is the curl/spiral generator.
/// Validated in Python (kifs_proto2.py / kifs_spherefold.py) before the GPU port.
/// </summary>
public sealed record KifsParams
{
    public int Iterations { get; init; } = 16;

    public float Scale { get; init; } = 2.0f;
    public float MinRadius { get; init; } = 0.5f;
    public float FixedRadius { get; init; } = 1.0f;

    /// <summary>Rotation applied BEFORE the abs fold (radians).</summary>
    public Vector3 PreRotationRadians { get; init; } = Vector3.Zero;
    /// <summary>Rotation applied AFTER the abs fold (radians). The curl generator.</summary>
    public Vector3 PostRotationRadians { get; init; } =
        new(20f * MathF.PI / 180f, 15f * MathF.PI / 180f, 10f * MathF.PI / 180f);

    /// <summary>The point the scale step contracts toward.</summary>
    public Vector3 Pivot { get; init; } = new(1f, 1f, 1f);

    /// <summary>DE step fudge in (0,1]. Lower = safer against overshoot, slower.</summary>
    public float Fudge { get; init; } = 0.7f;

    /// <summary>Bounding sphere radius around the origin for the marcher fast-skip.</summary>
    public float BoundRadius { get; init; } = 6.0f;
}

/// <summary>
/// GPU raymarcher for the Kaleidoscopic IFS fold family. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline. The
/// KIFS parameters are packed into the same FoldParams slots the Mandelbox uses,
/// reinterpreted by the KIFS core.
/// </summary>
public sealed class GpuKifsRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuKifsRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("kifs_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "kifs");
    }

    /// <summary>Render and return the raw RGBA8 pixel buffer (one uint per pixel).</summary>
    public uint[] RenderToBuffer(
        KifsParams kf,
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

        // Pack KIFS params into the shared FoldParams slots (see kifs_core.glsl):
        //   boxParams  = (scale, _, minRadius, fixedRadius)
        //   surfParams = (postRot.xyz, _)
        //   juliaC     = (pivot.xyz, _)
        //   rot        = (preRot.xyz, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = kf.Iterations,
            Mode = 0,
            JuliaMode = 0,
            Pad0 = 0,
            BoxParams = new Vector4(kf.Scale, 0f, kf.MinRadius, kf.FixedRadius),
            SurfParams = new Vector4(kf.PostRotationRadians, 0f),
            JuliaCVec = new Vector4(kf.Pivot, 0f),
            Rot = new Vector4(kf.PreRotationRadians, kf.Fudge),
            BoundSphere = new Vector4(0, 0, 0, kf.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    /// <summary>Render and return an SKBitmap (for the headless/file path).</summary>
    public SKBitmap Render(
        KifsParams kf, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(kf, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuKifsRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
