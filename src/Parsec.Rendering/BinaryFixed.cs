using System.Numerics;

namespace Parsec.Rendering.DeepZoom;

/// <summary>
/// Binary fixed-point helpers shared by the deep-zoom subsystem. A real value
/// <c>a</c> is represented as a <see cref="BigInteger"/> mantissa
/// <c>A = round(a * 2^P)</c> for some fractional-bit count P. This is the
/// arithmetic the reference orbit is computed in and the high-precision view
/// center is stored/panned in. All operations are validated against an mpmath
/// oracle (orbit) and a Decimal round-trip (string conversions).
/// </summary>
public static class BinaryFixed
{
    /// <summary>round((a*b) / 2^P), symmetric round-to-nearest.</summary>
    public static BigInteger MulShift(BigInteger a, BigInteger b, int P)
    {
        BigInteger t = a * b;
        BigInteger half = BigInteger.One << (P - 1);
        return t.Sign >= 0 ? (t + half) >> P : -(((-t) + half) >> P);
    }

    /// <summary>Parse a decimal string to fixed-point round(value * 2^P).</summary>
    public static BigInteger FromDecimal(string s, int P)
    {
        s = s.Trim();
        bool neg = s.Length > 0 && s[0] == '-';
        if (s.Length > 0 && (s[0] == '-' || s[0] == '+')) s = s.Substring(1);

        int dot = s.IndexOf('.');
        string digits = dot < 0 ? s : s.Substring(0, dot) + s.Substring(dot + 1);
        int fracLen = dot < 0 ? 0 : s.Length - dot - 1;

        BigInteger num = BigInteger.Parse(digits.Length == 0 ? "0" : digits);
        BigInteger scaled = num << P;
        BigInteger den = BigInteger.Pow(10, fracLen);
        BigInteger q = (scaled + den / 2) / den;       // round-to-nearest
        return neg ? -q : q;
    }

    /// <summary>Render a fixed-point value as a decimal string with the given
    /// number of fractional digits (use ~ ceil(P*0.302)+2 for full precision).</summary>
    public static string ToDecimal(BigInteger a, int P, int fracDigits)
    {
        bool neg = a.Sign < 0;
        BigInteger m = neg ? -a : a;
        BigInteger ip = m >> P;
        BigInteger frac = m - (ip << P);
        var sb = new System.Text.StringBuilder();
        if (neg) sb.Append('-');
        sb.Append(ip.ToString());
        if (fracDigits > 0)
        {
            sb.Append('.');
            for (int i = 0; i < fracDigits; i++)
            {
                frac *= 10;
                BigInteger d = frac >> P;
                sb.Append((char)('0' + (int)d));
                frac -= d << P;
            }
        }
        return sb.ToString();
    }

    /// <summary>value = a / 2^P as a double, exponent-aware (no overflow for large P).</summary>
    public static double ToDouble(BigInteger a, int P)
    {
        if (a.IsZero) return 0.0;
        bool neg = a.Sign < 0;
        BigInteger m = neg ? -a : a;
        int bits = (int)m.GetBitLength();
        int shift = Math.Max(0, bits - 54);
        double mant = (double)(m >> shift);
        double r = Math.ScaleB(mant, shift - P);
        return neg ? -r : r;
    }

    /// <summary>Re-express a fixed-point value at a different precision P
    /// (e.g. when a deeper zoom needs more fractional bits).</summary>
    public static BigInteger Rescale(BigInteger a, int fromP, int toP)
    {
        if (toP >= fromP) return a << (toP - fromP);
        int sh = fromP - toP;
        BigInteger half = BigInteger.One << (sh - 1);
        return a.Sign >= 0 ? (a + half) >> sh : -(((-a) + half) >> sh);
    }

    /// <summary>(toRe,toIm) - (fromRe,fromIm), evaluated in fixed-point then cast to
    /// double. Used for the reference offset dc = viewCenter - referenceCenter: a
    /// small difference of two close high-precision numbers, which double cannot do
    /// directly (catastrophic cancellation) but holds fine once differenced.</summary>
    public static (double re, double im) OffsetToDouble(
        string fromRe, string fromIm, string toRe, string toIm, int P)
    {
        BigInteger dRe = FromDecimal(toRe, P) - FromDecimal(fromRe, P);
        BigInteger dIm = FromDecimal(toIm, P) - FromDecimal(fromIm, P);
        return (ToDouble(dRe, P), ToDouble(dIm, P));
    }
}
