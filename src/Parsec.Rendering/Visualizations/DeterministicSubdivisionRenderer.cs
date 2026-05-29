using System.Numerics;
using Parsec.Core.Geometry;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;
using SkiaSharp;

namespace Parsec.Rendering.Visualizations;

/// <summary>
/// How leaf polygons are drawn: filled, outlined, or both.
/// </summary>
public enum FillMode
{
    Fill,
    Outline,
    FillAndOutline,
}

/// <summary>
/// Configuration for the deterministic subdivision renderer.
/// </summary>
/// <remarks>
/// <para>
/// This renderer applies the Hutchinson operator <paramref name="Depth"/> times
/// to the seed <see cref="BaseShape"/>, producing N^Depth leaf polygons (where
/// N is the number of nodes in the IFS), and rasterizes each leaf to the output
/// bitmap.
/// </para>
/// <para>
/// Best for: low-to-mid depth construction visualizations, IFSes with little
/// overlap (e.g. Sierpiński carpet), and debugging. Heavy overlap with opaque
/// fill degenerates visually to the convex footprint of the attractor — for
/// systems like the diamond example, prefer outlines or a density-accumulation
/// renderer once we add one.
/// </para>
/// </remarks>
public sealed record DeterministicSubdivisionConfig(
    IFS2D System,
    Polygon2D BaseShape,
    int Depth,
    int ImageWidth = 1024,
    int ImageHeight = 1024,
    ViewBounds? View = null,
    Color? Background = null,
    Color? Fill = null,
    Color? Outline = null,
    FillMode Mode = FillMode.Fill,
    float OutlineWidth = 1f)
{
    public ViewBounds EffectiveView => View ?? ViewBounds.UnitSquare;
    public Color EffectiveBackground => Background ?? new Color(0.97f, 0.965f, 0.94f);
    public Color EffectiveFill => Fill ?? Color.Rgb(50, 50, 60);
    public Color EffectiveOutline => Outline ?? Color.Rgb(20, 30, 50);
}

/// <summary>
/// Renders an IFS by recursive Hutchinson application on a seed polygon,
/// drawing each leaf polygon to an SKBitmap.
/// </summary>
public sealed class DeterministicSubdivisionRenderer : IIFSRenderer2D
{
    private readonly DeterministicSubdivisionConfig _config;

    public DeterministicSubdivisionRenderer(DeterministicSubdivisionConfig config)
    {
        if (config.Depth < 0)
            throw new ArgumentOutOfRangeException(nameof(config), "Depth must be non-negative");
        _config = config;
    }

    public SKBitmap Render()
    {
        var info = new SKImageInfo(_config.ImageWidth, _config.ImageHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SkColor(_config.EffectiveBackground));

        // Collect leaf polygons by recursive composition.
        var seedVerts = _config.BaseShape.Vertices.AsSpan();
        var leaves = new List<Vector2[]>();
        Recurse(AffineMap2D.Identity, _config.Depth, seedVerts, leaves);

        using var fillPaint = MakeFillPaint();
        using var outlinePaint = MakeOutlinePaint();

        var pixelBuf = new SKPoint[seedVerts.Length];
        foreach (var poly in leaves)
        {
            for (int i = 0; i < poly.Length; i++)
            {
                var px = _config.EffectiveView.ToPixel(poly[i], _config.ImageWidth, _config.ImageHeight);
                pixelBuf[i] = new SKPoint(px.X, px.Y);
            }

            using var path = new SKPath();
            path.MoveTo(pixelBuf[0]);
            for (int i = 1; i < pixelBuf.Length; i++) path.LineTo(pixelBuf[i]);
            path.Close();

            if (fillPaint is not null)
                canvas.DrawPath(path, fillPaint);
            if (outlinePaint is not null)
                canvas.DrawPath(path, outlinePaint);
        }

        canvas.Flush();
        return bitmap;
    }

    private void Recurse(
        AffineMap2D accumulated,
        int depth,
        ReadOnlySpan<Vector2> seedVerts,
        List<Vector2[]> output)
    {
        if (depth == 0)
        {
            output.Add(accumulated.ApplyPolygon(seedVerts));
            return;
        }
        foreach (var node in _config.System.Nodes)
        {
            var next = accumulated.Then(node.Transform);
            if (node.PostTransform is { } post)
                next = next.Then(post);
            Recurse(next, depth - 1, seedVerts, output);
        }
    }

    private SKPaint? MakeFillPaint()
    {
        if (_config.Mode == FillMode.Outline) return null;
        return new SKPaint
        {
            Color = SkColor(_config.EffectiveFill),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
    }

    private SKPaint? MakeOutlinePaint()
    {
        if (_config.Mode == FillMode.Fill) return null;
        return new SKPaint
        {
            Color = SkColor(_config.EffectiveOutline),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = _config.OutlineWidth,
            IsAntialias = true,
        };
    }

    private static SKColor SkColor(Color c) => new(c.R8, c.G8, c.B8, c.A8);
}
