using System.Collections.Immutable;
using System.Numerics;
using Parsec.Core.Geometry;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;
using Parsec.Rendering;
using Parsec.Rendering.Visualizations;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// The classic Sierpiński carpet: eight maps, each scaling by 1/3 and placing
/// the result into one of the eight outer cells of a 3x3 grid (skipping the
/// center cell).
/// </summary>
public sealed class CarpetExample : IExample
{
    public string Name => "carpet";
    public string Description => "Sierpiński carpet, depth 5";

    public static IFS2D BuildIfs()
    {
        var nodes = new List<IFSNode2D>(8);
        const float s = 1f / 3f;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (row == 1 && col == 1) continue; // skip center
                nodes.Add(new IFSNode2D(
                    Transform: AffineMap2D.ScaleToCell(s, new Vector2(col * s, row * s)),
                    Label: $"cell-{row}-{col}"));
            }
        }
        return new IFS2D(nodes.ToImmutableArray());
    }

    public SKBitmap? Render()
    {
        // 3^5 = 243; pick an image size that's a multiple for pixel-crisp output.
        const int size = 1215;
        var renderer = new DeterministicSubdivisionRenderer(new DeterministicSubdivisionConfig(
            System: BuildIfs(),
            BaseShape: Polygon2D.UnitSquare,
            Depth: 5,
            ImageWidth: size,
            ImageHeight: size,
            View: ViewBounds.UnitSquare,
            Fill: Color.Rgb(40, 50, 70),
            Mode: FillMode.Fill));
        return renderer.Render();
    }
}
