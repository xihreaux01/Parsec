using System.Numerics;
using Parsec.Core.Geometry;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;
using Parsec.Rendering;
using Parsec.Rendering.Visualizations;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// Sierpiński triangle: three half-scale maps placing copies into the three
/// corners of the unit square. Rendered with the unit-triangle seed.
/// </summary>
/// <remarks>
/// A sanity check that the architecture is genuinely shape-agnostic — same
/// renderer, different seed, different IFS, different fractal.
/// </remarks>
public sealed class TriangleExample : IExample
{
    public string Name => "triangle";
    public string Description => "Sierpiński triangle, depth 7";

    public static IFS2D BuildIfs() => IFS2D.FromNodes(
        new IFSNode2D(
            Transform: AffineMap2D.ScaleToCell(0.5f, new Vector2(0.25f, 0.5f)),
            Label: "top"),
        new IFSNode2D(
            Transform: AffineMap2D.ScaleToCell(0.5f, new Vector2(0f, 0f)),
            Label: "bottom-left"),
        new IFSNode2D(
            Transform: AffineMap2D.ScaleToCell(0.5f, new Vector2(0.5f, 0f)),
            Label: "bottom-right"));

    public SKBitmap? Render()
    {
        var renderer = new DeterministicSubdivisionRenderer(new DeterministicSubdivisionConfig(
            System: BuildIfs(),
            BaseShape: Polygon2D.UnitTriangle,
            Depth: 7,
            ImageWidth: 1280,
            ImageHeight: 1280,
            View: ViewBounds.UnitSquare,
            Fill: Color.Rgb(60, 70, 95),
            Mode: FillMode.Fill));
        return renderer.Render();
    }
}
