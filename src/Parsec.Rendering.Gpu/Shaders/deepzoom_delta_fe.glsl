#version 430 core

// ===========================================================================
// Deep-zoom Mandelbrot -- perturbation + rebasing delta pass (floatexp deltas).
//
// IDENTICAL in contract and output to deepzoom_delta.glsl, but the per-pixel
// delta dz is carried as "floatexp": a double mantissa with a separate int
// exponent, giving the full 53-bit mantissa over an essentially unbounded
// exponent range. This breaks the ~1.5e-154 wall where plain fp64 dz^2
// underflows to zero (sqrt of the smallest normal double), which collapses the
// perturbation to its linear term and dissolves the image.
//
// What stays fp64: the reference orbit Zref (its values are O(1); depth lives
// in the deltas' exponent range, not the reference mantissa) and dc itself
// (spacing stays a healthy double down to ~1e-300). Only dz / dz^2 move to
// floatexp. So this shader reads the SAME DeepParams / RefOrbit / MuOut SSBOs
// as the fp64 shader -- the pipeline simply picks which one to dispatch by
// view radius, and the common shallow case keeps the faster fp64 path.
//
// Validated against an mpmath oracle (floatexp_validate.py): the floatexp loop
// matches the oracle's escape iterations exactly at 1e-20 (36/36) and past the
// fp64 wall at 1e-180 (72/72 escaping pixels), with the only differences being
// orbits that agree to ~3e-17 straddling |z| = 2 (the usual SSAA-resolved
// escape-edge flip, inherent to a double-precision reference, not to floatexp).
//
// Output: one float per pixel = smooth iteration count, or -1.0 for in-set.
// ===========================================================================

layout(local_size_x = 8, local_size_y = 8) in;

layout(std430, binding = 1) readonly buffer RefOrbit {
    dvec2 Zref[];
};

layout(std430, binding = 2) writeonly buffer MuOut {
    float mu[];
};

layout(std430, binding = 4) readonly buffer DeepParams {
    int   width;
    int   height;
    int   rowOffset;
    int   rowCount;
    int   refCount;
    int   maxIter;
    int   _pad0;
    int   _pad1;
    dvec2 refDc;
    dvec2 pixelDx;
    dvec2 pixelDy;
    dvec2 jitter;
    double escapeR2;
    double _pad2;
};

// --------------------------------------------------------------------------
// floatexp: value = m * 2^e, with m normalized to [0.5,1) via frexp (or 0).
// frexp(double,out int) / ldexp(double,int) are core in GLSL 4.00+.
// These mirror the Python reference ops one-to-one.
// --------------------------------------------------------------------------
struct FloatExp { double m; int e; };

FloatExp feNorm(double m, int e)
{
    if (m == 0.0LF) return FloatExp(0.0LF, 0);
    int de;
    double fm = frexp(m, de);
    return FloatExp(fm, e + de);
}
FloatExp feFrom(double x)
{
    if (x == 0.0LF) return FloatExp(0.0LF, 0);
    int e;
    double m = frexp(x, e);
    return FloatExp(m, e);
}
double feTo(FloatExp a)
{
    return (a.m == 0.0LF) ? 0.0LF : ldexp(a.m, a.e);
}
FloatExp feMul(FloatExp a, FloatExp b)
{
    if (a.m == 0.0LF || b.m == 0.0LF) return FloatExp(0.0LF, 0);
    return feNorm(a.m * b.m, a.e + b.e);
}
FloatExp feMulD(FloatExp a, double x)        // floatexp * plain double
{
    if (a.m == 0.0LF || x == 0.0LF) return FloatExp(0.0LF, 0);
    return feNorm(a.m * x, a.e);
}
FloatExp feSqr(FloatExp a)
{
    if (a.m == 0.0LF) return FloatExp(0.0LF, 0);
    return feNorm(a.m * a.m, a.e + a.e);      // never underflows: exponent just doubles
}
FloatExp feNeg(FloatExp a) { return FloatExp(-a.m, a.e); }
FloatExp feAdd(FloatExp a, FloatExp b)
{
    if (a.m == 0.0LF) return b;
    if (b.m == 0.0LF) return a;
    int de = a.e - b.e;
    if (de >= 0) {
        if (de > 54) return a;                // b below a's last bit
        return feNorm(a.m + ldexp(b.m, -de), a.e);
    } else {
        if (-de > 54) return b;
        return feNorm(b.m + ldexp(a.m, de), b.e);
    }
}
FloatExp feSub(FloatExp a, FloatExp b) { return feAdd(a, feNeg(b)); }
bool feLess(FloatExp a, FloatExp b)           // |a| < |b| for nonneg (squared mags)
{
    if (a.m == 0.0LF) return b.m != 0.0LF;
    if (b.m == 0.0LF) return false;
    if (a.e != b.e) return a.e < b.e;
    return abs(a.m) < abs(b.m);
}

void main()
{
    uint gx = gl_GlobalInvocationID.x;
    uint gy = gl_GlobalInvocationID.y;
    if (gx >= uint(width) || gy >= uint(rowCount)) return;
    int  py = rowOffset + int(gy);
    if (py >= height) return;
    int  outIdx = py * width + int(gx);

    // Per-pixel dc = c - C in fp64 (spacing stays a healthy double here), then
    // lift to floatexp so the perturbation recurrence is underflow-proof.
    double fx = double(gx) - 0.5LF * double(width)  + jitter.x;
    double fy = double(py) - 0.5LF * double(height) + jitter.y;
    dvec2  dcD = refDc + fx * pixelDx + fy * pixelDy;
    FloatExp dcRe = feFrom(dcD.x);
    FloatExp dcIm = feFrom(dcD.y);

    FloatExp dzRe = FloatExp(0.0LF, 0);
    FloatExp dzIm = FloatExp(0.0LF, 0);
    int    m   = 0;
    int    esc = maxIter;
    double z2  = 0.0LF;

    for (int n = 0; n < maxIter; n++)
    {
        dvec2 Zr = Zref[m];

        // Full value z = Zr + dz (floatexp). When the orbit is escaping it is
        // O(1), so collapsing to double for the bailout test is exact enough.
        FloatExp zRe = feAdd(feFrom(Zr.x), dzRe);
        FloatExp zIm = feAdd(feFrom(Zr.y), dzIm);
        double zrd = feTo(zRe);
        double zid = feTo(zIm);
        double az2 = zrd * zrd + zid * zid;
        if (az2 > escapeR2) { esc = n; z2 = az2; break; }

        // Rebase: full value smaller than the delta (needs a true floatexp
        // magnitude compare -- both can be far below double's range here), or
        // the reference is exhausted.
        FloatExp azf  = feAdd(feSqr(zRe),  feSqr(zIm));
        FloatExp adzf = feAdd(feSqr(dzRe), feSqr(dzIm));
        if (feLess(azf, adzf) || m >= refCount - 1)
        {
            dzRe = zRe; dzIm = zIm;
            m  = 0;
            Zr = Zref[0];
        }

        // dz_{n+1} = 2*Zr*dz + dz^2 + dc   (complex, in floatexp)
        FloatExp twoZrDzRe = feSub(feMulD(dzRe, 2.0LF * Zr.x), feMulD(dzIm, 2.0LF * Zr.y));
        FloatExp twoZrDzIm = feAdd(feMulD(dzRe, 2.0LF * Zr.y), feMulD(dzIm, 2.0LF * Zr.x));
        FloatExp dz2Re = feSub(feSqr(dzRe), feSqr(dzIm));
        FloatExp dz2Im = feMulD(feMul(dzRe, dzIm), 2.0LF);
        dzRe = feAdd(feAdd(twoZrDzRe, dz2Re), dcRe);
        dzIm = feAdd(feAdd(twoZrDzIm, dz2Im), dcIm);
        m++;
    }

    if (esc >= maxIter)
    {
        mu[outIdx] = -1.0;
    }
    else
    {
        float lz = log(float(sqrt(z2)));
        mu[outIdx] = float(esc) + 1.0 - log2(max(lz, 1e-20));
    }
}
