using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the octonion associator-Julia: z -> z^2 + eps*[z,p,q] + c,
/// where [z,p,q] = (z p) q - z (p q) is the octonion associator. At eps = 0 the
/// associator vanishes and the set collapses to the complex Julia revolved into
/// a solid of revolution (Artin's theorem); eps > 0 is genuinely non-associative
/// and 8-dimensional. The 3D view is the seed slice spanning octonion axes
/// e0,e1,e2. p, q (and hence AssocNorm = the spectral norm of the associator
/// operator, used only as a DE Lipschitz constant) are fixed defaults in this
/// version; if you expose them, recompute AssocNorm or use 2.0 as a safe bound.
/// </summary>
public sealed record OctonionParams
{
    public int Iterations { get; init; } = 64;
    public float Bailout { get; init; } = 4.0f;
    public float Eps { get; init; } = 0.25f;        // associator strength
    public float AssocNorm { get; init; } = 1.7328f; // ||A|| for the default p,q
    public float Fudge { get; init; } = 0.9f;
    public float BoundRadius { get; init; } = 1.3f;

    // Slice mode. Flat = a 3-plane through the 8D set (smooth cross-section).
    // Stereographic = inverse-stereographic wrap of R^3 onto a 3-sphere of radius
    // StereoR (input pre-scaled by StereoK), which cuts the set transversally and
    // surfaces angular structure. StereoR ~ the boundary radius is the sweet spot.
    public bool Stereo { get; init; } = false;
    public float StereoK { get; init; } = 1.0f;
    public float StereoR { get; init; } = 0.8f;

    // Julia constant c (octonion); default sits near the cardioid cusp where the
    // perturbed map stays connected.
    public Vector4 CLo { get; init; } = new(0.28f, 0.012f, 0f, 0f);
    public Vector4 CHi { get; init; } = Vector4.Zero;

    // Associator plane (unit octonions p, q). The (p,q) moduli are fixed here.
    public Vector4 PLo { get; init; } = new(0.0006f, 0.1504f, -0.138f, -0.4483f);
    public Vector4 PHi { get; init; } = new(-0.2288f, -0.4991f, 0.0303f, 0.6746f);
    public Vector4 QLo { get; init; } = new(-0.3207f, -0.4043f, 0.3191f, 0.2325f);
    public Vector4 QHi { get; init; } = new(0.0687f, -0.6062f, -0.0191f, 0.453f);
}

/// <summary>
/// GPU raymarcher for the octonion associator-Julia. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline. Mirrors
/// GpuMandelbulbRenderer (same DE family: running-scalar-derivative).
/// </summary>
public sealed class GpuOctonionRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuOctonionRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("octonion_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "octonion");
    }

    public uint[] RenderToBuffer(
        OctonionParams o,
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
            Iterations = o.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(o.Bailout, o.Eps, o.AssocNorm, 0),
            SurfParams = new Vector4(o.Stereo ? 1f : 0f, o.StereoK, o.StereoR, 0),
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, o.Fudge),
            BoundSphere = new Vector4(0, 0, 0, o.BoundRadius),
            OctCLo = o.CLo, OctCHi = o.CHi,
            OctPLo = o.PLo, OctPHi = o.PHi,
            OctQLo = o.QLo, OctQHi = o.QHi,
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        OctonionParams o, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(o, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuOctonionRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
