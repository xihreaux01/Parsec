using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the Mosely-snowflake KIFS. Unlike the Kaleidoscopic "Amazing
/// IFS" (escape-time: sphere fold, dr*scale+1, DE = |z|/|dr|), this is a LINEAR
/// / EXACT cube-IFS: the corner-cube recursion that builds the Mosely snowflake
/// (keep the 8 corner subcubes of a 3x3x3 split; dimension log8/log3 ~ 1.893).
/// Every per-iteration op is an isometry except one uniform scale, so the DE is
/// exact: DE = sdBox(z, body) / scale^iters. Validated in Python
/// (mosely_groundtruth.py) before the GPU port.
///
/// Two deformation knobs, both isometries (exactness preserved):
///   Twist : rotation about the body diagonal [1,1,1]; near-120 deg breaks the
///           snowflake's mirror symmetry into a chiral pinwheel (keeps 3-fold).
///   Wedge : kaleidoscope fold of the [111]-frame x'y' plane; 2*pi == OFF ==
///           pure snowflake, smaller -> radial mandala.
/// Body fattens the final box SDF: 1.0 = sparse dust, ~1.4 = fuller lace (the
/// corner rule carves space at every scale, so it never fuses fully solid).
/// </summary>
public sealed record MoselyParams
{
    public int Iterations { get; init; } = 16;

    /// <summary>3x3x3 split factor. 3.0 is the true snowflake; DE-valid to detune.</summary>
    public float Scale { get; init; } = 3.0f;
    /// <summary>Final-box half-extent. 1.0 = sparse dust, ~1.4 = fuller lace.</summary>
    public float Body { get; init; } = 1.4f;

    /// <summary>Twist about the body diagonal [1,1,1] (radians, per iteration).</summary>
    public float TwistRadians { get; init; } = 0f;
    /// <summary>Wedge fold angle in the [111] frame (radians). &gt;= 2*pi disables it.</summary>
    public float WedgeRadians { get; init; } = MathF.Tau;

    /// <summary>DE step fudge in (0,1]. A touch under 1 guards the wedge seams.</summary>
    public float Fudge { get; init; } = 0.9f;

    /// <summary>Bounding sphere radius around the origin for the marcher fast-skip.</summary>
    public float BoundRadius { get; init; } = 2.0f;
}

/// <summary>
/// GPU raymarcher for the Mosely-snowflake KIFS. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline. Parameters are
/// packed into the same FoldParams slots the Mandelbox uses, reinterpreted by
/// mosely_core.glsl ("swap the DE core, keep the boat").
/// </summary>
public sealed class GpuMoselyRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuMoselyRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("mosely_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "mosely");
    }

    /// <summary>Render and return the raw RGBA8 pixel buffer (one uint per pixel).</summary>
    public uint[] RenderToBuffer(
        MoselyParams ms,
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

        // Pack Mosely params into the shared FoldParams slots (see mosely_core.glsl):
        //   boxParams  = (scale, body, _, _)
        //   surfParams = (twist, wedge, _, _)
        //   juliaC     = unused
        //   rot        = (_, _, _, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = ms.Iterations,
            Mode = 0,
            JuliaMode = 0,
            Pad0 = 0,
            BoxParams = new Vector4(ms.Scale, ms.Body, 0f, 0f),
            SurfParams = new Vector4(ms.TwistRadians, ms.WedgeRadians, 0f, 0f),
            JuliaCVec = new Vector4(0f, 0f, 0f, 0f),
            Rot = new Vector4(0f, 0f, 0f, ms.Fudge),
            BoundSphere = new Vector4(0, 0, 0, ms.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    /// <summary>Render and return an SKBitmap (for the headless/file path).</summary>
    public SKBitmap Render(
        MoselyParams ms, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(ms, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuMoselyRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
