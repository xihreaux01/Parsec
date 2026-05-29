using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the 3D Pickover biomorph: standard Julia iteration
/// (z_{n+1} = z^2 + c) but with COMPONENTWISE escape (max(|z.x|, |z.y|, |z.z|) > B)
/// instead of the usual rotation-invariant |z| > B. Lifted to 3D via Mandelbulb-
/// style trig (same n=2 power, same lift as Phoenix).
///
/// Pickover discovered this in 1986 by using the "wrong" escape criterion -- the
/// componentwise check treats the three axes asymmetrically and produces Julia
/// sets with limb-like protrusions, antennae, and bulbous bodies that look
/// recognizably creature-like. Validated in Python: c = (-0.5, 0.5, 0), B = 10
/// gives the iconic radial multi-armed creature.
///
/// DE: scalar-derivative tracking (Hubbard-Douady), same as Phoenix.
/// </summary>
public sealed record BiomorphParams
{
    public int Iterations { get; init; } = 16;

    /// <summary>Julia constant c (only .xyz used; .w kept for layout).</summary>
    public Vector3 C { get; init; } = new(-0.5f, 0.5f, 0.0f);

    /// <summary>Componentwise bailout. Larger B = orbits run longer before escape;
    /// smaller B = tighter basin boundary. Pickover's canonical is 10.</summary>
    public float Bailout { get; init; } = 10.0f;

    public bool Cut { get; init; } = true;
    public Vector3 PlaneNormal { get; init; } = new(0.3f, 0.5f, 0.8f);
    public float PlaneOffset { get; init; } = 0.0f;

    public float Fudge { get; init; } = 0.85f;
    public float BoundRadius { get; init; } = 3.0f;
}

/// <summary>
/// GPU raymarcher for the 3D Pickover biomorph. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuBiomorphRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuBiomorphRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("biomorph_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "biomorph");
    }

    public uint[] RenderToBuffer(
        BiomorphParams bp,
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
            Iterations = bp.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams  = new Vector4(bp.Bailout, bp.PlaneOffset, 0, bp.Cut ? 1f : 0f),
            SurfParams = new Vector4(Vector3.Normalize(bp.PlaneNormal), 0),
            JuliaCVec  = new Vector4(bp.C, 0),
            Rot        = new Vector4(0, 0, 0, bp.Fudge),
            BoundSphere = new Vector4(0, 0, 0, bp.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        BiomorphParams bp, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(bp, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuBiomorphRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
