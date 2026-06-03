using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Parameters for a quaternion Julia set (z -> z^2 + c in the quaternions). The
/// set is 4D; <see cref="WSlice"/> chooses the 3D slice we see, and the cut
/// plane (<see cref="PlaneNormal"/> + <see cref="PlaneOffset"/>, enabled by
/// <see cref="Cut"/>) reveals the intricate nested interior -- the killer view.
///
/// <see cref="Stereo"/> swaps the flat 3-plane slice for an inverse-stereographic
/// wrap of R^3 onto a 3-sphere of radius <see cref="StereoR"/> (input pre-scaled
/// by <see cref="StereoK"/>) -- a curved cut that surfaces structure a flat slice
/// flattens. WSlice is ignored while stereographic is on; the cut still applies.
/// </summary>
public sealed record QuaternionJuliaParams
{
    public int Iterations { get; init; } = 10;
    public Vector4 C { get; init; } = new(-0.2f, 0.8f, 0.0f, 0.0f);  // canonical
    public float WSlice { get; init; } = 0.0f;
    public float Bailout { get; init; } = 4.0f;

    public bool Cut { get; init; } = true;
    /// <summary>Cut plane axis: 0 = X, 1 = Y, 2 = Z. The plane normal is this axis.</summary>
    public int CutAxis { get; init; } = 0;
    public float PlaneOffset { get; init; } = 0.0f;

    public Vector3 PlaneNormal => CutAxis switch
    {
        1 => new Vector3(0, 1, 0),
        2 => new Vector3(0, 0, 1),
        _ => new Vector3(1, 0, 0),
    };

    public bool Stereo { get; init; } = false;
    public float StereoK { get; init; } = 1.0f;
    public float StereoR { get; init; } = 0.8f;

    public float Fudge { get; init; } = 0.9f;
    public float BoundRadius { get; init; } = 2.0f;
}

/// <summary>
/// GPU raymarcher for the quaternion Julia set. Owns only its compute shader;
/// shared buffers and the tile/AA loop live in RaymarchPipeline.
/// </summary>
public sealed class GpuQuaternionJuliaRenderer : IDisposable
{
    private readonly RaymarchPipeline _pipeline;
    private readonly ComputeShader _shader;
    private bool _disposed;

    public GpuQuaternionJuliaRenderer(Gl gl, RaymarchPipeline pipeline)
    {
        _pipeline = pipeline;
        var src = ShaderLoader.LoadComposite("quaternion_julia_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(gl, src, "quaternion_julia");
    }

    public uint[] RenderToBuffer(
        QuaternionJuliaParams qj,
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
            Iterations = qj.Iterations, Mode = 0, JuliaMode = 0, Pad0 = 0,
            BoxParams = new Vector4(qj.WSlice, qj.PlaneOffset, qj.Bailout, qj.Cut ? 1f : 0f),
            SurfParams = new Vector4(Vector3.Normalize(qj.PlaneNormal), 0),
            JuliaCVec = qj.C,
            Rot = new Vector4(qj.Stereo ? 1f : 0f, qj.StereoK, qj.StereoR, qj.Fudge),
            BoundSphere = new Vector4(0, 0, 0, qj.BoundRadius),
        };
        return _pipeline.Render(_shader, foldParams, camera, width, height, settings,
                                background, surface, lightDirection, palette,
                                heroSamples: settings.HeroSamples,
                                tileRows: tileRows, progress: progress);
    }

    public SKBitmap Render(
        QuaternionJuliaParams qj, Camera3D camera, int width, int height,
        RaymarchSettings settings, Color background, Color surface,
        Vector3 lightDirection, PaletteParams palette,
        int tileRows = 32, Action<int, int>? progress = null)
    {
        uint[] pixels = RenderToBuffer(qj, camera, width, height, settings,
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
        if (_disposed) throw new ObjectDisposedException(nameof(GpuQuaternionJuliaRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
    }
}
