using System.Numerics;
using Parsec.Core.Ifs;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// Twisted Sierpiński tetrahedron — the classic four maps, each with a
/// rotation about its fixed vertex, so sub-copies spiral into their parents.
/// Still a pure affine IFS; the twist emerges from composition across depth.
/// </summary>
public sealed class TwistedTetrahedronExample : IExample
{
    public string Name => "twisted-tet";
    public string Description => "Twisted Sierpiński tetrahedron (affine, per-vertex rotation)";

    public SKBitmap? Render()
    {
        var ifs = TwistedIFS3D.SierpinskiTetrahedron(
            twistRadians: 0.5f,
            axisMode: TwistAxisMode.CentroidToVertex);

        var estimator = new IFS3DDistanceEstimator(ifs, new IFS3DDistanceEstimatorConfig(
            MaxDepth: 10, DetailEpsilon: 1e-2f));

        const int width = 900;
        const int height = 900;

        var camera = new Camera3D(
            position: new Vector3(2.2f, 1.7f, 2.4f),
            lookAt: new Vector3(0.5f, 0.5f, 0.5f),
            up: Vector3.UnitY,
            verticalFovRadians: MathF.PI / 5f,
            aspectRatio: (float)width / height);

        var renderer = new RaymarchingRenderer(new RaymarchingConfig(
            Estimator: estimator,
            Camera: camera,
            ImageWidth: width,
            ImageHeight: height,
            Background: new Color(0.97f, 0.965f, 0.94f),
            Surface: Color.Rgb(95, 120, 95),
            LightDirection: new Vector3(0.5f, 0.9f, 0.3f),
            Settings: new RaymarchSettings(
                MaxSteps: 200, HitEpsilon: 1.5e-3f, MaxDistance: 20f, NormalEpsilon: 3e-3f,
                EnableSoftShadows: true, ShadowSteps: 48, ShadowSoftness: 12f,
                EnableAmbientOcclusion: true, AOSamples: 5, AOStepDistance: 0.04f, AOIntensity: 1.2f),
            Parallel: true));

        return renderer.Render();
    }
}
