using System.Numerics;
using System.Runtime.InteropServices;
using Parsec.Core.Ifs;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// GPU raymarcher: renders an IFS attractor by sphere-tracing the GPU
/// distance estimator. Mirrors the CPU
/// <see cref="Parsec.Rendering.Raymarching.RaymarchingRenderer"/> in output,
/// but runs the whole frame on the GPU.
/// </summary>
/// <remarks>
/// <para>
/// Dispatched in horizontal tiles to stay under the Windows TDR (Timeout
/// Detection and Recovery) watchdog, which resets the GPU driver if a single
/// dispatch blocks for more than ~2 seconds. Tiling also gives us natural
/// progress reporting.
/// </para>
/// </remarks>
public sealed class GpuRaymarchingRenderer : IDisposable
{
    private readonly Gl _gl;
    private readonly ComputeShader _shader;
    private readonly StorageBuffer<IFSMapGpu> _ifsBuffer;
    private readonly StorageBuffer<QueryParams> _queryBuffer;
    private readonly StorageBuffer<RenderParamsGpu> _renderBuffer;
    private readonly StorageBuffer<uint> _imageBuffer;

    private readonly IFS3D _ifs;
    private readonly int _numMaps;
    private readonly int _maxDepth;
    private readonly float _detailEpsilon;
    private readonly Core.Geometry.BoundingSphere _attractorSphere;

    private bool _disposed;

    public GpuRaymarchingRenderer(Gl gl, IFS3D ifs, int maxDepth = 10, float detailEpsilon = 1e-2f)
    {
        _gl = gl;
        if (ifs.Nodes.IsDefaultOrEmpty)
            throw new ArgumentException("IFS must have at least one node", nameof(ifs));
        if (ifs.Nodes.Length > 64)
            throw new ArgumentException("GPU renderer supports up to 64 maps.", nameof(ifs));
        if (maxDepth < 1 || maxDepth > 13)
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "MaxDepth must be in [1, 13].");

        _ifs = ifs;
        _numMaps = ifs.Nodes.Length;
        _maxDepth = maxDepth;
        _detailEpsilon = detailEpsilon;
        _attractorSphere = ifs.ComputeBoundingSphere();

        var src = ShaderLoader.LoadComposite("de_core.glsl", "raymarch_main.glsl");
        _shader = ComputeShader.FromSource(_gl, src, "raymarch");

        // Pack and upload IFS data.
        var gpuMaps = new IFSMapGpu[_numMaps];
        for (int i = 0; i < _numMaps; i++)
        {
            var t = ifs.Nodes[i].Transform;
            gpuMaps[i] = new IFSMapGpu
            {
                Row0 = new Vector4(t.M00, t.M01, t.M02, t.Tx),
                Row1 = new Vector4(t.M10, t.M11, t.M12, t.Ty),
                Row2 = new Vector4(t.M20, t.M21, t.M22, t.Tz),
                SigmaPad = new Vector4(t.SpectralNorm, 0, 0, 0),
            };
        }
        _ifsBuffer = new StorageBuffer<IFSMapGpu>(_gl);
        _ifsBuffer.Upload(gpuMaps);

        _queryBuffer = new StorageBuffer<QueryParams>(_gl);
        _queryBuffer.Upload(new[] { new QueryParams
        {
            PointCount = 0,
            NumMaps = _numMaps,
            MaxDepth = _maxDepth,
            Pad0 = 0,
            AttractorSphere = new Vector4(_attractorSphere.Center, _attractorSphere.Radius),
            DetailEps = new Vector4(_detailEpsilon, 0, 0, 0),
        }});

        _renderBuffer = new StorageBuffer<RenderParamsGpu>(_gl);
        _imageBuffer = new StorageBuffer<uint>(_gl);
    }

    /// <summary>
    /// Render the attractor to an SKBitmap. <paramref name="tileRows"/> sets
    /// the height of each dispatched tile; smaller tiles are safer against
    /// TDR but have marginally more overhead.
    /// </summary>
    public SKBitmap Render(
        Camera3D camera,
        int width,
        int height,
        RaymarchSettings settings,
        Color background,
        Color surface,
        Vector3 lightDirection,
        int tileRows = 64,
        Action<int, int>? progress = null)
    {
        ThrowIfDisposed();

        // Allocate the output image buffer (one uint per pixel).
        _imageBuffer.Allocate(width * height);

        // Build the camera frame the same way Camera3D does internally.
        var frame = CameraFrame.Build(camera, width, height);
        var lightDir = Vector3.Normalize(lightDirection);

        int flags = (settings.EnableSoftShadows ? 1 : 0) | (settings.EnableAmbientOcclusion ? 2 : 0);

        // Bind the persistent buffers.
        _ifsBuffer.BindBase(0);
        _queryBuffer.BindBase(1);
        // bindings 2,3 unused by the raymarch shader
        _renderBuffer.BindBase(4);
        _imageBuffer.BindBase(5);
        _shader.Use();

        int tiles = (height + tileRows - 1) / tileRows;
        for (int tile = 0; tile < tiles; tile++)
        {
            int rowOffset = tile * tileRows;
            int rowCount = Math.Min(tileRows, height - rowOffset);

            var rparams = new RenderParamsGpu
            {
                ImageWidth = width,
                ImageHeight = height,
                RowOffset = rowOffset,
                RowCount = rowCount,
                CamPos = new Vector4(camera.Position, 0),
                CamForward = new Vector4(frame.Forward, 0),
                CamRight = new Vector4(frame.Right, 0),
                CamUp = new Vector4(frame.Up, 0),
                TanFov = new Vector4(frame.TanFovX, frame.TanFovY, 0, 0),
                LightDir = new Vector4(lightDir, 0),
                Background = new Vector4(background.R, background.G, background.B, 1),
                Surface = new Vector4(surface.R, surface.G, surface.B, 1),
                MarchA = new Vector4(settings.HitEpsilon, settings.MaxDistance,
                                     settings.NormalEpsilon, settings.ShadowSoftness),
                MarchB = new Vector4(settings.AOStepDistance, settings.AOIntensity, 0, 0),
                MarchI0 = settings.MaxSteps,
                MarchI1 = settings.ShadowSteps,
                MarchI2 = settings.AOSamples,
                MarchI3 = flags,
            };
            _renderBuffer.Upload(new[] { rparams });
            _renderBuffer.BindBase(4);

            // Dispatch over this tile: 8x8 local size.
            int groupsX = (width + 7) / 8;
            int groupsY = (rowCount + 7) / 8;
            _shader.Dispatch(groupsX, groupsY);
            // Finish this tile before moving on, so a single dispatch stays
            // short (TDR safety) and the GPU isn't queuing the whole frame.
            _gl.MemoryBarrier(GlConst.ShaderStorageBarrierBit);
            _gl.Finish();

            progress?.Invoke(tile + 1, tiles);
        }

        // Read back and assemble the bitmap.
        uint[] pixels = _imageBuffer.Download();
        return BuildBitmap(pixels, width, height);
    }

    private static SKBitmap BuildBitmap(uint[] pixels, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        var bytes = new byte[pixels.Length * 4];
        System.Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length);
        Marshal.Copy(bytes, 0, bitmap.GetPixels(), bytes.Length);
        return bitmap;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GpuRaymarchingRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shader.Dispose();
        _ifsBuffer.Dispose();
        _queryBuffer.Dispose();
        _renderBuffer.Dispose();
        _imageBuffer.Dispose();
    }

    // -- Camera frame replication (matches Camera3D internals) --
    private readonly struct CameraFrame
    {
        public readonly Vector3 Forward, Right, Up;
        public readonly float TanFovX, TanFovY;

        private CameraFrame(Vector3 f, Vector3 r, Vector3 u, float tx, float ty)
        { Forward = f; Right = r; Up = u; TanFovX = tx; TanFovY = ty; }

        public static CameraFrame Build(Camera3D cam, int width, int height)
        {
            var forward = Vector3.Normalize(cam.LookAt - cam.Position);
            var right = Vector3.Normalize(Vector3.Cross(forward, cam.Up));
            var up = Vector3.Cross(right, forward);
            float tanFovY = MathF.Tan(cam.VerticalFovRadians * 0.5f);
            float tanFovX = tanFovY * ((float)width / height);
            return new CameraFrame(forward, right, up, tanFovX, tanFovY);
        }
    }

    // -- GPU struct layouts (must match std430 in the shaders) --

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IFSMapGpu
    {
        public Vector4 Row0, Row1, Row2, SigmaPad;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct QueryParams
    {
        public int PointCount, NumMaps, MaxDepth, Pad0;
        public Vector4 AttractorSphere;
        public Vector4 DetailEps;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RenderParamsGpu
    {
        public int ImageWidth, ImageHeight, RowOffset, RowCount;
        public Vector4 CamPos, CamForward, CamRight, CamUp, TanFov;
        public Vector4 LightDir, Background, Surface;
        public Vector4 MarchA, MarchB;
        public int MarchI0, MarchI1, MarchI2, MarchI3;
    }
}
