using System.Numerics;
using Parsec.Core.Ifs;
using Parsec.Rendering;
using Parsec.Rendering.Raymarching;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// Fractal trefoil knot — a non-classical IFS where 24 similarity maps
/// contract toward sample points on the trefoil. The attractor is a string
/// of fractal "beads" tracing the knot's path through 3-space.
/// </summary>
public sealed class TrefoilExample : IExample
{
    public string Name => "trefoil";
    public string Description => "Fractal trefoil knot — bead-string attractor";

    public SKBitmap? Render()
    {
        var ifs = KnotIFS.TrefoilKnot(sampleCount: 24, contraction: 0.20f);
        var estimator = new IFS3DDistanceEstimator(ifs, new IFS3DDistanceEstimatorConfig(
            MaxDepth: 10,
            DetailEpsilon: 1e-2f));

        const int width = 900;
        const int height = 900;

        // The trefoil has 3-fold symmetry around the z-axis. Looking
        // mostly down z from a moderately elevated oblique angle shows
        // the three-lobed top profile while still revealing depth via
        // the bead structure.
        var camera = new Camera3D(
            position: new Vector3(3.0f, -5.5f, 6.5f),
            lookAt:   new Vector3(0f, -0.3f, 0f),
            up:       Vector3.UnitZ,
            verticalFovRadians: MathF.PI / 4.5f,
            aspectRatio: (float)width / height);

        var renderer = new RaymarchingRenderer(new RaymarchingConfig(
            Estimator: estimator,
            Camera: camera,
            ImageWidth: width,
            ImageHeight: height,
            Background: new Color(0.97f, 0.965f, 0.94f),
            Surface: Color.Rgb(80, 100, 130),
            LightDirection: new Vector3(0.5f, -0.4f, 0.9f),
            Settings: new RaymarchSettings(
                MaxSteps: 250,
                HitEpsilon: 2e-3f,
                MaxDistance: 30f,
                NormalEpsilon: 4e-3f,
                EnableSoftShadows: true,
                ShadowSteps: 48,
                ShadowSoftness: 10f,
                EnableAmbientOcclusion: true,
                AOSamples: 5,
                AOStepDistance: 0.04f,
                AOIntensity: 1.2f),
            Parallel: true));

        return renderer.Render();
    }
}
