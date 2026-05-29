using System.Numerics;

namespace Parsec.Rendering.DeepZoom;

/// <summary>
/// The 2D deep-zoom view model -- the replacement for the 3D camera in
/// Mandelbrot mode. Plain serializable data (so the deferred zoom-video keyframe
/// support stays cheap): a high-precision center as decimal strings, a double
/// half-height radius, and an iteration cap. Pan is applied at full precision via
/// <see cref="BinaryFixed"/>; zoom just scales the radius. Required fixed-point
/// bit count scales with depth.
/// </summary>
public sealed class DeepZoomView
{
    /// <summary>View center real part, arbitrary-precision decimal string.</summary>
    public string CenterRe { get; set; } = "-0.5";
    /// <summary>View center imaginary part, arbitrary-precision decimal string.</summary>
    public string CenterIm { get; set; } = "0.0";
    /// <summary>Half-height of the view in complex units (the zoom level).</summary>
    public double Radius { get; set; } = 1.5;
    /// <summary>Iteration cap (also the reference-orbit length).</summary>
    public int MaxIterations { get; set; } = 2000;

    /// <summary>Fixed-point fractional bits needed at the current depth, with margin.</summary>
    public int PrecisionBits()
    {
        int depthDigits = Math.Max(15, (int)Math.Ceiling(-Math.Log10(Radius)) + 4);
        return ReferenceOrbit.RecommendedPrecisionBits(depthDigits);
    }

    /// <summary>Multiply the zoom radius (factor &lt; 1 zooms in).</summary>
    public void ZoomBy(double factor)
        => Radius = Math.Clamp(Radius * factor, 1e-300, 4.0);

    /// <summary>Shift the center by a complex offset (in complex units), at full
    /// precision -- the offset is a double but is placed at the correct binary
    /// position in the P-bit center, so deep pans don't lose precision.</summary>
    public void PanComplex(double dRe, double dIm)
    {
        int P = PrecisionBits();
        int digits = (int)Math.Ceiling(P * 0.3010299957) + 2;
        BigInteger cre = BinaryFixed.FromDecimal(CenterRe, P) + RoundToFixed(dRe, P);
        BigInteger cim = BinaryFixed.FromDecimal(CenterIm, P) + RoundToFixed(dIm, P);
        CenterRe = BinaryFixed.ToDecimal(cre, P, digits);
        CenterIm = BinaryFixed.ToDecimal(cim, P, digits);
    }

    /// <summary>Drag-pan by a pixel delta given the current resolution. Dragging
    /// the image right moves the view left (content follows the cursor).</summary>
    public void PanPixels(double dxPixels, double dyPixels, int height)
    {
        double spacing = (2.0 * Radius) / height;
        PanComplex(-dxPixels * spacing, dyPixels * spacing);
    }

    /// <summary>Complex-units-per-pixel at a given output height.</summary>
    public double SpacingFor(int height) => (2.0 * Radius) / height;

    // round(v * 2^P) as BigInteger, exact for the value's 53 mantissa bits.
    // (v*2^P stays ~ pixels * 2^margin well under 2^53 because P tracks depth,
    // so ScaleB then round-to-integer is exact.)
    private static BigInteger RoundToFixed(double v, int P)
        => v == 0.0 ? BigInteger.Zero : new BigInteger(Math.Round(Math.ScaleB(v, P)));
}
