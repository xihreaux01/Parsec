using System.Numerics;
using Parsec.Core.Geometry;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;
using Parsec.Rendering;
using Parsec.Rendering.Visualizations;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// The IFS from the reference image: a unit-square rotated 45° about its center
/// (the "diamond") unioned with two half-scale squares in the upper-left and
/// upper-right quadrants.
/// </summary>
/// <remarks>
/// The diamond map is a pure rotation, not a contraction — the corner maps
/// provide the contraction for the system as a whole. This is a non-standard
/// IFS in the sense that not all maps are contractive, but it produces a real
/// (asymmetric) attractor.
/// </remarks>
public sealed class DiamondExample : IExample
{
    public string Name => "diamond";
    public string Description => "Reference: diamond + upper-corner squares (depth 7, outlines)";

    public static IFS2D BuildIfs()
    {
        var diamond = IFS2D.FromNodes(
            new IFSNode2D(
                Transform: AffineMap2D.RotateScaleAt(MathF.PI / 4f, scale: 1f, center: new Vector2(0.5f, 0.5f)),
                Label: "diamond"));

        var corners = IFS2D.FromNodes(
            new IFSNode2D(
                Transform: AffineMap2D.ScaleToCell(0.5f, new Vector2(0f, 0.5f)),
                Label: "upper-left"),
            new IFSNode2D(
                Transform: AffineMap2D.ScaleToCell(0.5f, new Vector2(0.5f, 0.5f)),
                Label: "upper-right"));

        return diamond | corners;
    }

    public SKBitmap? Render()
    {
        var renderer = new DeterministicSubdivisionRenderer(new DeterministicSubdivisionConfig(
            System: BuildIfs(),
            BaseShape: Polygon2D.UnitSquare,
            Depth: 7,
            ImageWidth: 1600,
            ImageHeight: 1600,
            View: ViewBounds.UnitSquare.Pad(0.6f),
            Mode: FillMode.Outline,
            Outline: Color.Rgb(40, 50, 70, 180),
            OutlineWidth: 1f));
        return renderer.Render();
    }
}

/// <summary>
/// The same IFS at depth 2 with translucent fill + outlines: matches the
/// bottom-right panel of the reference image (one iteration of the Hutchinson
/// operator past A ∪ B).
/// </summary>
public sealed class DiamondConstructionExample : IExample
{
    public string Name => "diamond-construction";
    public string Description => "Reference: diamond IFS at depth 2 with fill + outlines";

    public SKBitmap? Render()
    {
        var renderer = new DeterministicSubdivisionRenderer(new DeterministicSubdivisionConfig(
            System: DiamondExample.BuildIfs(),
            BaseShape: Polygon2D.UnitSquare,
            Depth: 2,
            ImageWidth: 1200,
            ImageHeight: 1200,
            View: ViewBounds.UnitSquare.Pad(0.6f),
            Mode: FillMode.FillAndOutline,
            Fill: new Color(0.2f, 0.2f, 0.24f, 0.5f),
            Outline: Color.Rgb(20, 30, 50),
            OutlineWidth: 1.5f));
        return renderer.Render();
    }
}
