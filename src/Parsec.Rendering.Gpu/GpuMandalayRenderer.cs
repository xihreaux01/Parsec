using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the Mandalay Fold fractal (darkbeam's Mandalay fold V2, an SDF
/// CSG fold transform, wrapped in the standard escape-time scale scaffold the
/// transform itself lacks: z = scale*fold(z) + c, dr = |scale|*dr + 1, DE = |z|/dr).
/// Same family as the Mandelbox/KIFS chapters. The fold is nearly isometric
/// (measured Lipschitz mean ~0.98) but spikes to ~1.7x at the fold seams, so a
/// DE fudge ~0.55 keeps the marcher from overshooting there. Validated in
/// Python (mandalay_proto.py): negative scale (~ -2) gives a rich bounded set.
/// </summary>
public sealed record MandalayParams
{
    public int Iterations { get; init; } = 12;

    /// <summary>0 = parallel folds (each axis from z), 1 = sequential (the "Sw" toggle).</summary>
    public int Mode { get; init; } = 0;
    /// <summary>0 = Mandelbrot (c = position), 1 = Julia (c = JuliaC).</summary>
    public int JuliaMode { get; init; } = 0;

    /// <summary>Escape-time scale. Negative (~ -2) gives the rich set; positive a sliver.</summary>
    public float Scale { get; init; } = -2.0f;
    /// <summary>Primary fold offset (darkbeam's fo / additionConstant0555).</summary>
    public float FoldOffset { get; init; } = 0.555f;
    /// <summary>Secondary offset g (offsetA000); opens up the beam structure.</summary>
    public float OffsetG { get; init; } = 0.0f;
    /// <summary>Secondary offset h (offsetF000); opens up the beam structure.</summary>
    public float OffsetH { get; init; } = 0.0f;

    /// <summary>Escape bailout radius.</summary>
    public float Bailout { get; init; } = 8.0f;

    /// <summary>Julia constant (the additive c), used when JuliaMode == 1.</summary>
    public Vector3 JuliaC { get; init; } = Vector3.Zero;

    /// <summary>DE step fudge in (0,1]. The fold expands ~1.7x at seams, so ~0.55
    /// is the safe default; lower it if the seams sparkle / dropout.</summary>
    public float Fudge { get; init; } = 0.55f;
    /// <summary>Bounding sphere radius around the origin for the marcher fast-skip.</summary>
    public float BoundRadius { get; init; } = 6.0f;
}

/// <summary>
/// GPU raymarcher for the Mandalay Fold fold-fractal. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline. Its
/// parameters pack into the shared FoldParams slots reinterpreted by
/// mandalay_core.glsl (see that file's header).
/// </summary>
public sealed class GpuMandalayRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuMandalayRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("mandalay_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "mandalay");
    }

    /// <summary>Render and return the raw RGBA8 pixel buffer (one uint per pixel).</summary>
    public uint[] RenderToBuffer(
        MandalayParams md,
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

        // Pack into the shared FoldParams slots (see mandalay_core.glsl):
        //   boxParams  = (scale, fo, g, h)
        //   surfParams = (bailout, _, _, _)
        //   juliaC     = (cx, cy, cz, _)
        //   rot        = (_, _, _, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = md.Iterations,
            Mode = md.Mode,
            JuliaMode = md.JuliaMode,
            Pad0 = 0,
            BoxParams = new Vector4(md.Scale, md.FoldOffset, md.OffsetG, md.OffsetH),
            SurfParams = new Vector4(md.Bailout, 0f, 0f, 0f),
            JuliaCVec = new Vector4(md.JuliaC, 0f),
            Rot = new Vector4(0f, 0f, 0f, md.Fudge),
            BoundSphere = new Vector4(0, 0, 0, md.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    /// <summary>Render and return an SKBitmap (for the headless/file path).</summary>
    public SKBitmap Render(
        MandalayParams md, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(md, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuMandalayRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
