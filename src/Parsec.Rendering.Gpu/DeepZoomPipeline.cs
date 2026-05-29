using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Gpu;
using Parsec.Rendering.Raymarching;

namespace Parsec.Rendering.DeepZoom;

// std430 layout for the delta pass. MUST match DeepParams in deepzoom_delta.glsl.
// All dvec2 members start on 16-byte boundaries (32,48,64,80), so the C# doubles
// land at the same offsets GLSL uses -- no manual padding needed beyond Pad0/1.
[StructLayout(LayoutKind.Sequential)]
internal struct DeepParamsGpu
{
    public int Width, Height, RowOffset, RowCount;   // 0..32
    public int RefCount, MaxIter, Pad0, Pad1;        // ..32
    public double RefDcX, RefDcY;                    // dvec2 refDc   @32
    public double PixelDxX, PixelDxY;                // dvec2 pixelDx @48
    public double PixelDyX, PixelDyY;                // dvec2 pixelDy @64
    public double JitterX, JitterY;                  // dvec2 jitter  @80
    public double EscapeR2, Pad2;                    // @96 ..112
}

// std430 layout for the color pass. MUST match ColorParams in deepzoom_color.glsl.
[StructLayout(LayoutKind.Sequential)]
internal struct ColorParamsGpu
{
    public int Width, Height, P0, P1;     // 0..16
    public Vector4 PalBase;               // rgb=a, a=freq   @16
    public Vector4 PalAmp;                // rgb=b           @32
    public Vector4 PalPhase;              // rgb=d           @48
    public Vector4 Bg;                    // rgb=in-set bg   @64
    public float PalScale, P2, P3, P4;    // @80 ..96
}

/// <summary>
/// Parallel 2D pipeline for deep-zoom Mandelbrot. It does NOT use the
/// raymarcher: each pixel is a perturbation delta against one CPU-computed
/// high-precision reference orbit. Owns its SSBOs and four compute shaders
/// (delta, color, clear, finalize) and mirrors RaymarchPipeline's tile/AA
/// lifecycle, emitting the same packed RGBA8 uint[] so the display path is shared.
///
/// Per render: clear the accumulator, then for each AA sample run the fp64 delta
/// pass (tiled, TDR-safe) into the mu buffer and a color pass that maps mu through
/// the palette and ADDS into the accumulator; finalize divides by the sample
/// count and packs. Reference recompute policy (per design): recompute the bignum
/// orbit on zoom (radius change) or when the cached reference center drifts out of
/// view; otherwise reuse it, threading the small offset as refDc.
///
/// Deep-zoom SSBO bindings: 1=RefOrbit 2=Mu 3=AAParams 4=DeepParams 5=Accum
/// 6=ColorParams 7=ImageOut.
/// </summary>
public sealed class DeepZoomPipeline : IDisposable
{
    private readonly Gl _gl;
    private readonly StorageBuffer<double> _refBuffer;            // 1
    private readonly StorageBuffer<float> _muBuffer;             // 2
    private readonly StorageBuffer<AAParamsGpu> _aaParams;       // 3
    private readonly StorageBuffer<DeepParamsGpu> _deepParams;   // 4
    private readonly StorageBuffer<Vector4> _accumBuffer;        // 5
    private readonly StorageBuffer<ColorParamsGpu> _colorParams; // 6
    private readonly StorageBuffer<uint> _imageBuffer;           // 7
    private readonly ComputeShader _deltaShader;
    private readonly ComputeShader _colorShader;
    private readonly ComputeShader _clearShader;
    private readonly ComputeShader _finalizeShader;
    private bool _disposed;

    // reference-orbit cache
    private ReferenceOrbit? _ref;
    private string _refRe = "", _refIm = "";
    private double _refRadius = double.NaN;
    private int _refP = -1;

    public DeepZoomPipeline(Gl gl)
    {
        _gl = gl;
        _refBuffer = new StorageBuffer<double>(gl);
        _muBuffer = new StorageBuffer<float>(gl);
        _aaParams = new StorageBuffer<AAParamsGpu>(gl);
        _deepParams = new StorageBuffer<DeepParamsGpu>(gl);
        _accumBuffer = new StorageBuffer<Vector4>(gl);
        _colorParams = new StorageBuffer<ColorParamsGpu>(gl);
        _imageBuffer = new StorageBuffer<uint>(gl);

        // Standalone single-file compute shaders (each carries its own #version,
        // like smoke.comp / ifs_de.glsl). Add both under Shaders/ as EmbeddedResource
        // in Parsec.Rendering.Gpu.csproj; Load() resolves them by bare filename.
        _deltaShader = ComputeShader.FromSource(gl,
            ShaderLoader.Load("deepzoom_delta.glsl"), "deepzoom_delta");
        _colorShader = ComputeShader.FromSource(gl,
            ShaderLoader.Load("deepzoom_color.glsl"), "deepzoom_color");
        _clearShader = ComputeShader.FromSource(gl, ClearShaderSource, "dz_clear");
        _finalizeShader = ComputeShader.FromSource(gl, FinalizeShaderSource, "dz_finalize");
    }

    public uint[] Render(
        DeepZoomView view, int width, int height,
        PaletteParams palette, Color background,
        int heroSamples = 1, int tileRows = 32, Action<int, int>? progress = null)
    {
        ThrowIfDisposed();
        int samples = Math.Max(1, heroSamples);
        int pixelCount = width * height;

        EnsureReference(view);                       // (re)computes + uploads _refBuffer
        int P = view.PrecisionBits();
        var (refDcRe, refDcIm) = BinaryFixed.OffsetToDouble(
            _refRe, _refIm, view.CenterRe, view.CenterIm, P);
        double spacing = view.SpacingFor(height);

        _muBuffer.Allocate(pixelCount);
        _accumBuffer.Allocate(pixelCount);
        _imageBuffer.Allocate(pixelCount);
        _aaParams.Upload(new[] { new AAParamsGpu {
            Width = width, Height = height, SampleCount = samples, Pad0 = 0 } });
        _colorParams.Upload(new[] { BuildColorParams(width, height, palette, background) });

        // clear accumulator
        _accumBuffer.BindBase(5);
        _aaParams.BindBase(3);
        _clearShader.Use();
        _clearShader.Dispatch((width + 7) / 8, (height + 7) / 8);
        _gl.MemoryBarrier(GlConst.ShaderStorageBarrierBit);

        int tiles = (height + tileRows - 1) / tileRows;
        int totalUnits = samples * tiles;
        int done = 0;

        for (int sample = 0; sample < samples; sample++)
        {
            Vector2 jit = (samples == 1) ? Vector2.Zero : HaltonJitter(sample);

            // ---- delta pass (fp64), tiled ----
            _refBuffer.BindBase(1);
            _muBuffer.BindBase(2);
            _deltaShader.Use();
            for (int tile = 0; tile < tiles; tile++)
            {
                int rowOffset = tile * tileRows;
                int rowCount = Math.Min(tileRows, height - rowOffset);
                _deepParams.Upload(new[] { BuildDeepParams(
                    width, height, rowOffset, rowCount, _ref!.Count, view.MaxIterations,
                    refDcRe, refDcIm, spacing, 0.5 + jit.X, 0.5 + jit.Y) });
                _deepParams.BindBase(4);
                _deltaShader.Dispatch((width + 7) / 8, (rowCount + 7) / 8);
                _gl.MemoryBarrier(GlConst.ShaderStorageBarrierBit);
                _gl.Finish();
                done++;
                progress?.Invoke(done, totalUnits);
            }

            // ---- color + accumulate (full frame) ----
            _muBuffer.BindBase(2);
            _accumBuffer.BindBase(5);
            _colorParams.BindBase(6);
            _colorShader.Use();
            _colorShader.Dispatch((width + 7) / 8, (height + 7) / 8);
            _gl.MemoryBarrier(GlConst.ShaderStorageBarrierBit);
        }

        // finalize: divide by sampleCount, pack RGBA8
        _accumBuffer.BindBase(5);
        _imageBuffer.BindBase(7);
        _aaParams.BindBase(3);
        _finalizeShader.Use();
        _finalizeShader.Dispatch((width + 7) / 8, (height + 7) / 8);
        _gl.MemoryBarrier(GlConst.ShaderStorageBarrierBit);
        _gl.Finish();

        return _imageBuffer.Download();
    }

    private void EnsureReference(DeepZoomView view)
    {
        int P = view.PrecisionBits();
        bool need = _ref == null || view.Radius != _refRadius || P != _refP;
        if (!need)
        {
            var (dx, dy) = BinaryFixed.OffsetToDouble(
                _refRe, _refIm, view.CenterRe, view.CenterIm, P);
            if (Math.Sqrt(dx * dx + dy * dy) > view.Radius)   // reference center left the view
                need = true;
        }
        if (need)
        {
            _ref = ReferenceOrbit.Compute(view.CenterRe, view.CenterIm, P, view.MaxIterations);
            _refRe = view.CenterRe; _refIm = view.CenterIm;
            _refRadius = view.Radius; _refP = P;
            _refBuffer.Upload(_ref.ToInterleaved());
        }
    }

    private static DeepParamsGpu BuildDeepParams(
        int width, int height, int rowOffset, int rowCount, int refCount, int maxIter,
        double refDcRe, double refDcIm, double spacing, double jitterX, double jitterY)
        => new DeepParamsGpu
        {
            Width = width, Height = height, RowOffset = rowOffset, RowCount = rowCount,
            RefCount = refCount, MaxIter = maxIter, Pad0 = 0, Pad1 = 0,
            RefDcX = refDcRe, RefDcY = refDcIm,
            PixelDxX = spacing, PixelDxY = 0.0,
            PixelDyX = 0.0, PixelDyY = -spacing,
            JitterX = jitterX, JitterY = jitterY,
            EscapeR2 = 4.0, Pad2 = 0.0,
        };

    private static ColorParamsGpu BuildColorParams(
        int width, int height, PaletteParams palette, Color background)
        => new ColorParamsGpu
        {
            Width = width, Height = height, P0 = 0, P1 = 0,
            PalBase = new Vector4(palette.Base, palette.Frequency),
            PalAmp = new Vector4(palette.Amp, palette.TrapScale),
            PalPhase = new Vector4(palette.Phase, palette.ShellMix),
            Bg = new Vector4(background.R, background.G, background.B, 1f),
            PalScale = 0.0125f,    // mu -> palette cycles (~80 iters/cycle); expose later
            P2 = 0, P3 = 0, P4 = 0,
        };

    // Halton(2,3) sub-pixel jitter in [-0.5,0.5]^2 (same as RaymarchPipeline).
    private static Vector2 HaltonJitter(int sampleIndex)
    {
        int n = sampleIndex + 1;
        return new Vector2(Halton(n, 2) - 0.5f, Halton(n, 3) - 0.5f);
    }

    private static float Halton(int index, int b)
    {
        float f = 1f, result = 0f;
        while (index > 0) { f /= b; result += f * (index % b); index /= b; }
        return result;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DeepZoomPipeline));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _deltaShader.Dispose();
        _colorShader.Dispose();
        _clearShader.Dispose();
        _finalizeShader.Dispose();
        _refBuffer.Dispose();
        _muBuffer.Dispose();
        _aaParams.Dispose();
        _deepParams.Dispose();
        _accumBuffer.Dispose();
        _colorParams.Dispose();
        _imageBuffer.Dispose();
    }

    // Clear + finalize mirror RaymarchPipeline's (image output here is binding 7,
    // since binding 2 holds the mu buffer). Candidates to factor into a shared
    // AA-helper if you'd rather not keep two copies.
    private const string ClearShaderSource = @"
#version 430 core
layout(local_size_x = 8, local_size_y = 8) in;
layout(std430, binding = 5) buffer Accum { vec4 colors[]; } accum;
layout(std430, binding = 3) readonly buffer AAParams { int width; int height; int sampleCount; int pad0; } aa;
void main() {
    int px = int(gl_GlobalInvocationID.x);
    int py = int(gl_GlobalInvocationID.y);
    if (px >= aa.width || py >= aa.height) return;
    accum.colors[py * aa.width + px] = vec4(0.0);
}
";

    private const string FinalizeShaderSource = @"
#version 430 core
layout(local_size_x = 8, local_size_y = 8) in;
layout(std430, binding = 5) readonly buffer Accum { vec4 colors[]; } accum;
layout(std430, binding = 7) writeonly buffer Output { uint pixels[]; } img;
layout(std430, binding = 3) readonly buffer AAParams { int width; int height; int sampleCount; int pad0; } aa;
void main() {
    int px = int(gl_GlobalInvocationID.x);
    int py = int(gl_GlobalInvocationID.y);
    if (px >= aa.width || py >= aa.height) return;
    int idx = py * aa.width + px;
    vec4 c = accum.colors[idx] / float(aa.sampleCount);
    uvec3 q = uvec3(clamp(c.rgb, 0.0, 1.0) * 255.0 + 0.5);
    img.pixels[idx] = (255u << 24) | (q.b << 16) | (q.g << 8) | q.r;
}
";
}
