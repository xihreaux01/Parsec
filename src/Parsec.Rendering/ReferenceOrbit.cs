using System.Numerics;

namespace Parsec.Rendering.DeepZoom;

/// <summary>
/// High-precision reference orbit Z_{n+1} = Z_n^2 + C for perturbation-based
/// deep-zoom Mandelbrot. The ONLY arbitrary-precision component: one orbit per
/// view, computed in binary fixed-point (see <see cref="BinaryFixed"/>), each
/// Z_n then cast to double for the GPU delta pass. The Z_n are O(1), so double
/// storage is fine; depth is carried by the deltas' exponent range, never by the
/// reference mantissa. Validated against an mpmath oracle to 1 ULP.
/// </summary>
public sealed class ReferenceOrbit
{
    public readonly double[] Re;
    public readonly double[] Im;
    public readonly int Count;
    public readonly bool Escaped;

    private ReferenceOrbit(double[] re, double[] im, int count, bool escaped)
    {
        Re = re; Im = im; Count = count; Escaped = escaped;
    }

    public static ReferenceOrbit Compute(string centerRe, string centerIm,
                                         int precisionBits, int maxIter)
    {
        int P = precisionBits;
        BigInteger cre = BinaryFixed.FromDecimal(centerRe, P);
        BigInteger cim = BinaryFixed.FromDecimal(centerIm, P);
        BigInteger zre = BigInteger.Zero, zim = BigInteger.Zero;
        BigInteger four = BigInteger.One << (P + 2);   // 4.0 in fixed-point

        var re = new double[maxIter + 1];
        var im = new double[maxIter + 1];
        bool escaped = false;
        int n = 0;
        while (n <= maxIter)
        {
            re[n] = BinaryFixed.ToDouble(zre, P);
            im[n] = BinaryFixed.ToDouble(zim, P);

            BigInteger reSq = BinaryFixed.MulShift(zre, zre, P);
            BigInteger imSq = BinaryFixed.MulShift(zim, zim, P);
            if (reSq + imSq > four) { escaped = true; n++; break; }

            BigInteger reim = BinaryFixed.MulShift(zre, zim, P);
            zre = reSq - imSq + cre;       // Re(Z^2 + C)
            zim = (reim << 1) + cim;       // Im(Z^2 + C) = 2*re*im + C.im
            n++;
        }

        if (n < re.Length) { Array.Resize(ref re, n); Array.Resize(ref im, n); }
        return new ReferenceOrbit(re, im, n, escaped);
    }

    /// <summary>Interleaved [re0, im0, re1, im1, ...] for upload to a dvec2 SSBO.</summary>
    public double[] ToInterleaved()
    {
        var buf = new double[Count * 2];
        for (int i = 0; i < Count; i++) { buf[2 * i] = Re[i]; buf[2 * i + 1] = Im[i]; }
        return buf;
    }

    /// <summary>Suggested fixed-point bits for a zoom whose view radius is ~1e-digits:
    /// digits * log2(10) plus a safety margin (longer orbits / deeper zooms want more).</summary>
    public static int RecommendedPrecisionBits(int zoomDepthDecimalDigits, int marginBits = 32)
        => (int)Math.Ceiling(zoomDepthDecimalDigits * 3.321928094887362) + marginBits;
}

// ============================================================================
// VALIDATION FIXTURE (from the Python mirror, matched to mpmath within 1 ULP):
//   var orb = ReferenceOrbit.Compute(
//       "-0.743643887037158704752191506114774",
//       "0.131825904205311970493132056385139", precisionBits: 200, maxIter: 2000);
//   orb.Count == 2001;  orb.Escaped == false;
//   orb.Re[1] == -7.43643887037158668e-01;  orb.Im[1] == 1.31825904205311983e-01;
//   orb.Re[4] == -2.72462266139682607e-01;  orb.Im[4] == -9.15717851505672975e-02;
//   (sum(Re)+sum(Im)) checksum == -9.16195433262730035e+02;
//   orb.Re[Count-1] == -2.72462266139672393e-01;  // == Z[4]: pre-periodic / Misiurewicz
// ============================================================================
