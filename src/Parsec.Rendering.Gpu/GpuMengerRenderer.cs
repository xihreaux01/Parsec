using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for the rotated folded Menger-IFS. Classic Menger sort (largest
/// component to z) with abs-fold preserving rectilinear character, scale-by-3
/// IFS step, optional translation offset, and a compounding pre-rotation each
/// iteration. Produces architectural/rectilinear fractal geometry. Validated in
/// Python (menger_proto.py).
/// </summary>
public sealed record MengerParams
{
    public int Iterations { get; init; } = 6;
    public float Scale { get; init; } = 3.0f;
    public Vector3 Offset { get; init; } = new(1.0f, 1.0f, 0.0f);

    /// <summary>Euler rotation in RADIANS, applied each iteration before the fold.</summary>
    public Vector3 Rotation { get; init; } = new(0.10f, 0.07f, 0.04f);

    public float Fudge { get; init; } = 0.8f;
    public float BoundRadius { get; init; } = 3.5f;
}

/// <summary>
/// GPU raymarcher for the rotated folded Menger-IFS. Owns only its compute
/// shader; shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuMengerRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuMengerRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("menger_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "menger");
    }

    public uint[] RenderToBuffer(
        MengerParams mg,
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
            Iterations = mg.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(mg.Scale, mg.Offset.X, mg.Offset.Y, mg.Offset.Z),
            SurfParams = new Vector4(mg.Rotation, 0),
            JuliaCVec = Vector4.Zero,
            Rot = new Vector4(0, 0, 0, mg.Fudge),
            BoundSphere = new Vector4(0, 0, 0, mg.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        MengerParams mg, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(mg, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuMengerRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
