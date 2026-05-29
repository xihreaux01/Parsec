using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the bicomplex Julia (Fracmonk's formula on fractalforums.com)
/// with per-component scaling muls and a W_add symmetry-breaker. Uses the
/// bicomplex (tessarine) algebra rather than quaternions, producing a
/// different Julia set with faceted/crystalline character (because bicomplex
/// isn't a division algebra).
///
/// DE: running-scalar-derivative (Hubbard-Douady), same family as Mandelbulb
/// and Phoenix -- see bicomplex_core.glsl. (This replaced an earlier numerical-
/// gradient DE that produced wispy artifacts.)
/// </summary>
public sealed record BicomplexParams
{
    public int Iterations { get; init; } = 12;

    public Vector4 C { get; init; } = new(-0.5f, 0.0f, 0.0f, 0.0f);

    // Per-component multipliers (Fracmonk's user augmentation).
    public float XMul { get; init; } = 1.0f;
    public float YMul { get; init; } = 1.0f;
    public float ZMul { get; init; } = 1.0f;
    public float WMul { get; init; } = 1.0f;
    public float WAdd { get; init; } = 0.0f;

    public float WSlice { get; init; } = 0.0f;
    public float Bailout { get; init; } = 4.0f;

    // Half-cut (inherited from quaternion-Julia/qjbox).
    public bool Cut { get; init; } = true;
    /// <summary>0 = X, 1 = Y, 2 = Z.</summary>
    public int CutAxis { get; init; } = 0;
    public float PlaneOffset { get; init; } = 0.0f;

    public float Fudge { get; init; } = 0.85f;
    public float BoundRadius { get; init; } = 4.0f;

    public float CutAxisFlag => Cut ? (CutAxis + 1) : 0f;
}

/// <summary>
/// GPU raymarcher for the bicomplex Julia. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuBicomplexRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuBicomplexRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("bicomplex_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "bicomplex");
    }

    public uint[] RenderToBuffer(
        BicomplexParams bp,
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
            BoxParams = new Vector4(bp.XMul, bp.YMul, bp.ZMul, bp.WMul),
            SurfParams = new Vector4(bp.WAdd, bp.WSlice, 0, 0),
            JuliaCVec = bp.C,
            Rot = new Vector4(bp.PlaneOffset, bp.CutAxisFlag, bp.Bailout, bp.Fudge),
            BoundSphere = new Vector4(0, 0, 0, bp.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        BicomplexParams bp, Camera3D camera, int width, int height,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuBicomplexRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
