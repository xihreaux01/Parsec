using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the strict 3D Apollonian gasket -- four mutually-tangent
/// unit spheres at the vertices of a regular tetrahedron, plus a Soddy
/// bounding sphere. Rendered as the LIMIT SET of the Kleinian group generated
/// by these inversions (a real Coxeter gasket -- not a space-fold approximation).
///
/// The headline knob is <see cref="Tangency"/> (multiplier on the canonical
/// inner-sphere centre distance). 1.0 = the connected Coxeter gasket; less
/// than 1 overlaps the spheres for a denser foam; greater than 1 separates
/// them and fragments the limit set into Kleinian fractal dust. One slider,
/// three qualitatively distinct aesthetic regimes -- a rare phase-transition
/// morph star.
/// </summary>
public sealed record ApollonianParams
{
    public int Iterations { get; init; } = 25;

    /// <summary>Multiplier on canonical sphere centre distance sqrt(3/2).
    /// 1.0 = mutually tangent Coxeter packing; less = overlap; greater = dust.</summary>
    public float Tangency { get; init; } = 1.0f;

    /// <summary>Multiplier on canonical bounding-sphere radius (sqrt(6)+2)/2.
    /// 1.0 = canonical Soddy bound.</summary>
    public float OuterRadiusMult { get; init; } = 1.0f;

    /// <summary>DE envelope -- effectively the rendered surface "thickness".
    /// Smaller = thinner gasket trace, more accurate to true limit set.</summary>
    public float DeEnvelope { get; init; } = 0.30f;

    public bool Cut { get; init; } = true;
    public Vector3 PlaneNormal { get; init; } = new(0.3f, 0.5f, 0.8f);
    public float PlaneOffset { get; init; } = 0.0f;

    public float Fudge { get; init; } = 0.9f;

    /// <summary>Canonical bounding radius from 3D Descartes/Soddy.</summary>
    public const float CanonicalOuterRadius = 2.2247448713915890f;

    /// <summary>Fast-skip bound, sized generously to enclose the visible fractal
    /// across the full tangency/outer-mult sweep.</summary>
    public float BoundRadius =>
        MathF.Max(2.6f,
                  MathF.Max(CanonicalOuterRadius * OuterRadiusMult,
                            MathF.Sqrt(1.5f) * Tangency + 1.0f) + 0.4f);
}

/// <summary>
/// GPU raymarcher for the 3D Apollonian gasket. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuApollonianRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuApollonianRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("apollonian_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "apollonian");
    }

    public uint[] RenderToBuffer(
        ApollonianParams ap,
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
            Iterations = ap.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams  = new Vector4(ap.Tangency, ap.PlaneOffset, ap.OuterRadiusMult,
                                     ap.Cut ? 1f : 0f),
            SurfParams = new Vector4(Vector3.Normalize(ap.PlaneNormal), ap.DeEnvelope),
            JuliaCVec  = Vector4.Zero,
            Rot        = new Vector4(0, 0, 0, ap.Fudge),
            BoundSphere = new Vector4(0, 0, 0, ap.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        ApollonianParams ap, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(ap, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuApollonianRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
