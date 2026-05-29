using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the quaternion-Julia × Mandelbox hybrid with rotation and the
/// inherited half-cut from the pure quaternion Julia. Both halves run every
/// iteration in sequence: rotate -> Mandelbox fold -> quaternion-square. The DE
/// is heuristic with a safety factor baked in; validated as cross-sectioning
/// meaningfully in Python (qjbox_proto.py).
/// </summary>
public sealed record QJBoxParams
{
    public int Iterations { get; init; } = 8;

    // Mandelbox half.
    public float Scale { get; init; } = -1.8f;
    public float MinRadius { get; init; } = 0.5f;
    public float FixedRadius { get; init; } = 1.0f;
    public float FoldLimit { get; init; } = 1.0f;

    // Quaternion Julia half.
    public Vector4 C { get; init; } = new(-0.2f, 0.6f, 0.1f, 0.0f);
    public float WSlice { get; init; } = 0.0f;

    // Rotation (radians, applied to the 3D part each iteration).
    public Vector3 Rotation { get; init; } = new(0.10f, 0.07f, 0.04f);

    // Half-cut (inherited killer feature).
    public bool Cut { get; init; } = true;
    /// <summary>0 = X, 1 = Y, 2 = Z (matches the pure quaternion Julia).</summary>
    public int CutAxis { get; init; } = 0;
    public float PlaneOffset { get; init; } = 0.0f;

    public float Fudge { get; init; } = 0.6f;
    public float BoundRadius { get; init; } = 4.0f;

    /// <summary>Encode the cut state for the shader: 0 = off, 1 = X, 2 = Y, 3 = Z.</summary>
    public float CutAxisFlag => Cut ? (CutAxis + 1) : 0f;
}

/// <summary>
/// GPU raymarcher for the quaternion-Julia × Mandelbox hybrid. Owns only its
/// compute shader; shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuQJBoxRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuQJBoxRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("qjbox_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "qjbox");
    }

    public uint[] RenderToBuffer(
        QJBoxParams qb,
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
            Iterations = qb.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(qb.Scale, qb.MinRadius, qb.FixedRadius, qb.FoldLimit),
            SurfParams = new Vector4(qb.Rotation, 0),
            JuliaCVec = qb.C,
            // rot = (wslice, planeOffset, cutAxisFlag, fudge)
            Rot = new Vector4(qb.WSlice, qb.PlaneOffset, qb.CutAxisFlag, qb.Fudge),
            BoundSphere = new Vector4(0, 0, 0, qb.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        QJBoxParams qb, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(qb, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuQJBoxRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
