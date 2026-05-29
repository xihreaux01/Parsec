using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Core.Attractors;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Rendering parameters for the attractor tube (the look, not the shape -- the
/// shape lives in the prebuilt <see cref="AttractorHash"/>). These can change
/// live without re-generating the trajectory.
/// </summary>
public sealed record AttractorRenderParams
{
    public float TubeRadius { get; init; } = 0.06f;
    public float Fudge { get; init; } = 0.45f;
}

/// <summary>
/// GPU raymarcher for a strange-attractor trajectory rendered as a glowing tube.
/// Unlike the fold/inversion renderers, this consumes a prebuilt
/// <see cref="AttractorHash"/> (trajectory points + spatial hash), uploaded to
/// SSBOs at bindings 6/7/8, and pairs it with the hash-walking DE core
/// (attractor_core.glsl).
///
/// This renderer is special: it owns three extra data buffers (trajectory, hash,
/// sorted indices) at bindings 6/7/8. The shared RaymarchPipeline only ever
/// binds 1/2/3/4/5, so binding these once before delegating is enough -- they
/// persist through the pipeline's clear/dispatch/finalize passes (glBindBufferBase
/// state is global and not disturbed by switching shader programs). That keeps
/// the attractor on the same shared AA + tiling path as every other fractal.
///
/// The hash is uploaded once via <see cref="SetAttractor"/> (the expensive
/// "generate" step), then any number of frames can be rendered at different
/// cameras/colours without re-upload.
/// </summary>
public sealed class GpuAttractorRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private readonly StorageBuffer<Vector4> _trajBuffer;
    private readonly StorageBuffer<int> _hashBuffer;
    private readonly StorageBuffer<int> _sortedBuffer;

    private AttractorHash? _hash;
    private bool _disposed;

    public GpuAttractorRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("attractor_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "attractor");
        _trajBuffer = new StorageBuffer<Vector4>(gl);
        _hashBuffer = new StorageBuffer<int>(gl);
        _sortedBuffer = new StorageBuffer<int>(gl);
    }

    /// <summary>
    /// Upload a freshly built attractor hash to the GPU. This is the expensive
    /// "generate" step; call it on demand (e.g. a Generate button), not per frame.
    /// </summary>
    public void SetAttractor(AttractorHash hash)
    {
        ThrowIfDisposed();
        _hash = hash;

        _trajBuffer.Upload(hash.Points);

        // Pack the per-cell (offset, count) pairs into one int array.
        int totalCells = hash.CellOffsets.Length;
        var cellData = new int[totalCells * 2];
        for (int i = 0; i < totalCells; i++)
        {
            cellData[i * 2] = hash.CellOffsets[i];
            cellData[i * 2 + 1] = hash.CellCounts[i];
        }
        _hashBuffer.Upload(cellData);
        _sortedBuffer.Upload(hash.SortedIndices);
    }

    public uint[] RenderToBuffer(
        AttractorRenderParams rp,
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
        if (_hash is null)
            throw new InvalidOperationException("SetAttractor must be called before rendering.");

        var lo = _hash.BoundsMin;
        var hi = _hash.BoundsMax;
        var center = (lo + hi) * 0.5f;
        float boundR = (hi - lo).Length() * 0.5f + rp.TubeRadius;

        // Pack into the shared FoldParams slots (see attractor_core.glsl):
        //   boxParams = (tubeRadius, gridSize, _, _)
        //   juliaC    = (boundsMin.xyz, _)   surfParams = (boundsMax.xyz, _)
        //   rot.w     = fudge                 boundSphere = (center, radius)
        var foldParams = new FoldParamsGpu
        {
            Iterations = 0, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(rp.TubeRadius, _hash.GridSize, 0, 0),
            SurfParams = new Vector4(hi, 0),
            JuliaCVec = new Vector4(lo, 0),
            Rot = new Vector4(0, 0, 0, rp.Fudge),
            BoundSphere = new Vector4(center, boundR),
        };

        // Bind the attractor's own data buffers at 6/7/8. The shared pipeline
        // only touches 1/2/3/4/5, so these persist for the duration of the
        // render (across the clear, per-tile/per-sample dispatch, and finalize).
        _trajBuffer.BindBase(6);
        _hashBuffer.BindBase(7);
        _sortedBuffer.BindBase(8);

        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        AttractorRenderParams rp, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(rp, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuAttractorRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
        _trajBuffer.Dispose();
        _hashBuffer.Dispose();
        _sortedBuffer.Dispose();
    }
}
