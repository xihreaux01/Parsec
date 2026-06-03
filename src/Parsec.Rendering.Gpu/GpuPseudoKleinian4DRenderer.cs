using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the Pseudo-Kleinian 4D fractal (Mandelbulber pseudo-kleinian
/// lineage). Approximates a Kleinian-group LIMIT SET -- foamy nested-cell
/// lattices / cathedral tilings -- via a box offset, a box fold, and a
/// one-sided spherical fold, finalized with a slab-intersect-tube distance
/// estimate (NOT the |z|/dr of the Mandelbox family). The 4th coordinate is a
/// fixed slice (<see cref="W0"/>). Validated against the real raymarch entry
/// point (pseudokleinian_core.glsl) before this port.
/// </summary>
public sealed record PseudoKleinian4DParams
{
    public int Iterations { get; init; } = 12;

    /// <summary>DE form: 0 = tube sqrt(x^2+y^2+w^2); 1 = quaternionic min-of-four.</summary>
    public int Mode { get; init; } = 0;
    /// <summary>Bounding: 0 = raw (render within bound sphere), 1 = sphere-inversion ON.</summary>
    public int InversionMode { get; init; } = 1;

    /// <summary>Box-fold half-size per axis (the w component is fixed at 1 in the core).</summary>
    public Vector3 BoxSize { get; init; } = new(1.0f, 1.0f, 1.0f);
    /// <summary>One-sided spherical fold scale: invert only inside the min-radius sphere.</summary>
    public float SphereFoldScale { get; init; } = 1.0f;

    /// <summary>Box offset: a sign-dependent translation -- the Kleinian symmetry break.</summary>
    public Vector3 BoxOffset { get; init; } = Vector3.Zero;
    /// <summary>The fixed 4D slice coordinate (W0).</summary>
    public float W0 { get; init; } = 0.0f;

    /// <summary>DE tube radius about the z-axis (offsetR0).</summary>
    public float TubeRadius { get; init; } = 0.0f;
    /// <summary>DE offset (offset0).</summary>
    public float DeOffset { get; init; } = 0.0f;
    /// <summary>DE derivative tweak (tweak005); small positive keeps the DE conservative.</summary>
    public float DeTweak { get; init; } = 0.05f;
    /// <summary>Sphere-inversion scale (scaleG1); used only when <see cref="InversionMode"/> == 1.</summary>
    public float InversionScale { get; init; } = 1.0f;

    /// <summary>DE step fudge in (0,1]. Lower = safer against overshoot, slower.</summary>
    public float Fudge { get; init; } = 0.6f;
    /// <summary>Bounding sphere radius around the origin for the marcher fast-skip.</summary>
    public float BoundRadius { get; init; } = 8.0f;
}

/// <summary>
/// GPU raymarcher for the Pseudo-Kleinian 4D limit-set fractal. Owns only its
/// compute shader; shared buffers and the tile/AA loop live in RaymarchPipeline.
/// Its parameters are packed into the same FoldParams slots the Mandelbox uses,
/// reinterpreted by pseudokleinian_core.glsl (see that file's header for the map).
/// </summary>
public sealed class GpuPseudoKleinian4DRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuPseudoKleinian4DRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("pseudokleinian_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "pseudokleinian4d");
    }

    /// <summary>Render and return the raw RGBA8 pixel buffer (one uint per pixel).</summary>
    public uint[] RenderToBuffer(
        PseudoKleinian4DParams pk,
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

        // Pack into the shared FoldParams slots (see pseudokleinian_core.glsl):
        //   boxParams  = (cSize.xyz, sphereFoldScale)
        //   surfParams = (tubeRadius, deOffset, deTweak, inversionScale)
        //   juliaC     = (boxOffset.xyz, w0)
        //   rot        = (_, _, _, fudge)
        var foldParams = new FoldParamsGpu
        {
            Iterations = pk.Iterations,
            Mode = pk.Mode,
            JuliaMode = pk.InversionMode,
            Pad0 = 0,
            BoxParams = new Vector4(pk.BoxSize, pk.SphereFoldScale),
            SurfParams = new Vector4(pk.TubeRadius, pk.DeOffset, pk.DeTweak, pk.InversionScale),
            JuliaCVec = new Vector4(pk.BoxOffset, pk.W0),
            Rot = new Vector4(0f, 0f, 0f, pk.Fudge),
            BoundSphere = new Vector4(0, 0, 0, pk.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    /// <summary>Render and return an SKBitmap (for the headless/file path).</summary>
    public SKBitmap Render(
        PseudoKleinian4DParams pk, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(pk, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuPseudoKleinian4DRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
